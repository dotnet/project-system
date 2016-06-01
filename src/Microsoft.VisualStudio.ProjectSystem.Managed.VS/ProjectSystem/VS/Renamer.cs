// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RoslyRenamer = Microsoft.CodeAnalysis.Rename;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class Renamer
    {
        private readonly VisualStudioWorkspace _visualStudioWorkspace;
        private readonly IProjectThreadingService _threadingService;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly Project _project;
        private readonly Document _oldDocument;
        private readonly string _newFilePath;
        private bool _docAdded = false;
        private bool _docRemoved = false;
        private bool _docChanged = false;

        internal Renamer(VisualStudioWorkspace visualStudioWorkspace,
                         SVsServiceProvider serviceProvider,
                         IProjectThreadingService threadingService,
                         Project project,
                         string newFilePath,
                         string oldFilePath)
        {
            _visualStudioWorkspace = visualStudioWorkspace;
            _serviceProvider = serviceProvider;
            _threadingService = threadingService;
            _project = project;
            _newFilePath = newFilePath;
            _oldDocument = (from d in project.Documents where d.FilePath.Equals(oldFilePath, StringComparison.OrdinalIgnoreCase) select d).FirstOrDefault();
        }

        public void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
        {
            if (args.Kind == WorkspaceChangeKind.DocumentAdded && args.ProjectId == _project.Id)
            {
                Project project = (from p in args.NewSolution.Projects where p.Id.Equals(_project.Id) select p).FirstOrDefault();
                Document addedDocument = (from d in project.Documents where d.Id.Equals(args.DocumentId) select d).FirstOrDefault();
                if (addedDocument.FilePath.Equals(_newFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    _docAdded = true;
                }
            }

            if (args.Kind == WorkspaceChangeKind.DocumentRemoved && args.ProjectId == _project.Id && args.DocumentId == _oldDocument.Id)
            {
                _docRemoved = true;
            }

            if (args.Kind == WorkspaceChangeKind.DocumentChanged && args.ProjectId == _project.Id)
            {
                _docChanged = true;
            }

            if (_docAdded && _docRemoved && _docChanged)
            {
                _visualStudioWorkspace.WorkspaceChanged -= OnWorkspaceChanged;

                _threadingService.JoinableTaskFactory.RunAsync(async () =>
                {
                    var myNewProject = _visualStudioWorkspace.CurrentSolution.Projects.Where(p => string.Equals(p.FilePath, _project.FilePath, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    Document newDocument = (from d in myNewProject.Documents where d.FilePath.Equals(_newFilePath, StringComparison.OrdinalIgnoreCase) select d).FirstOrDefault();
                    if (newDocument == null)
                        return;

                    var root = await newDocument.GetSyntaxRootAsync().ConfigureAwait(false);
                    if (root == null)
                        return;

                    string oldName = Path.GetFileNameWithoutExtension(_oldDocument.FilePath);
                    string newName = Path.GetFileNameWithoutExtension(newDocument.FilePath);

                    var declaration = root.DescendantNodes().Where(n => HasMatchingSyntaxNode(newDocument, n, oldName)).FirstOrDefault();
                    if (declaration == null)
                        return;

                    var semanticModel = await newDocument.GetSemanticModelAsync().ConfigureAwait(false);
                    if (semanticModel == null)
                        return;

                    var symbol = semanticModel.GetDeclaredSymbol(declaration);
                    if (symbol == null)
                        return;

                    // We're about to do a symbolic rename.If the user has asked us to prompt, We need to open a dialog and ask them  
                    //  Otherwise go ahead and do the rename.
                    EnvDTE.DTE dte = _serviceProvider.GetService<EnvDTE.DTE, EnvDTE.DTE>();
                    var props = dte.Properties["Environment", "ProjectsAndSolution"];
                    if (props != null)
                    {
                        if ((bool)props.Item("PromptForRenameSymbol").Value)
                        {
                            string promptMessage = string.Format(Resources.RenameSymbolPrompt, oldName);
                            var result = VsShellUtilities.ShowMessageBox(_serviceProvider, promptMessage, null, OLEMSGICON.OLEMSGICON_QUERY,
                                                            OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                            if (result == (int)VSConstants.MessageBoxResult.IDNO)
                            {
                                return;
                            }
                        }
                    }

                    // Now do the rename
                    var optionSet = newDocument.Project.Solution.Workspace.Options;
                    var renamedSolution = await RoslyRenamer.Renamer.RenameSymbolAsync(newDocument.Project.Solution, symbol, newName, optionSet).ConfigureAwait(false);

                    await _threadingService.SwitchToUIThread();

                    if (!newDocument.Project.Solution.Workspace.TryApplyChanges(renamedSolution))
                    {
                        string promptMessage = string.Format(Resources.RenameSymbolFailed, oldName);
                        var result = VsShellUtilities.ShowMessageBox(_serviceProvider, promptMessage, null, OLEMSGICON.OLEMSGICON_WARNING,
                                                OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                });

            }
        }

        private bool HasMatchingSyntaxNode(Document document, SyntaxNode syntaxNode, string name)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var kind = generator.GetDeclarationKind(syntaxNode);

            if (kind == DeclarationKind.Class ||
                kind == DeclarationKind.Interface ||
                kind == DeclarationKind.Delegate ||
                kind == DeclarationKind.Enum ||
                kind == DeclarationKind.Struct)
            {
                return generator.GetName(syntaxNode) == name;
            }
            return false;
        }
    }
}
