// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class Renamer
    {
        private readonly Workspace _workspace;
        private readonly IProjectThreadingService _threadingService;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly IRoslynServices _roslynServices;
        private readonly Project _project;
        private readonly Document _oldDocument;
        private readonly string _newFilePath;
        private readonly string _oldFilePath;
        private bool _docAdded = false;
        private bool _docRemoved = false;
        private bool _docChanged = false;

        internal Renamer(Workspace workspace,
                         IProjectThreadingService threadingService,
                         IUserNotificationServices userNotificationServices,
                         IRoslynServices roslynServices,
                         Project project,
                         string oldFilePath,
                         string newFilePath)
        {
            _workspace = workspace;
            _threadingService = threadingService;
            _userNotificationServices = userNotificationServices;
            _roslynServices = roslynServices;
            _project = project;
            _newFilePath = newFilePath;
            _oldFilePath = oldFilePath;
            _oldDocument = (from d in project.Documents where StringComparers.Paths.Equals(d.FilePath, oldFilePath) select d).FirstOrDefault();
        }

        public void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
        {
            if (args.Kind == WorkspaceChangeKind.DocumentAdded && args.ProjectId == _project.Id)
            {
                Project project = (from p in args.NewSolution.Projects where p.Id.Equals(_project.Id) select p).FirstOrDefault();
                Document addedDocument = (from d in project.Documents where d.Id.Equals(args.DocumentId) select d).FirstOrDefault();
                if (StringComparers.Paths.Equals(addedDocument.FilePath, _newFilePath))
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
                _workspace.WorkspaceChanged -= OnWorkspaceChanged;
                var myNewProject = _workspace.CurrentSolution.Projects.Where(p => StringComparers.Paths.Equals(p.FilePath, _project.FilePath)).FirstOrDefault();
                Rename(myNewProject);
            }
        }

        public void Rename(Project myNewProject)
        {
            _threadingService.JoinableTaskFactory.RunAsync(async () =>
            {
                Document newDocument = (from d in myNewProject.Documents where StringComparers.Paths.Equals(d.FilePath, _newFilePath) select d).FirstOrDefault();
                if (newDocument == null)
                    return;

                var root = await newDocument.GetSyntaxRootAsync().ConfigureAwait(false);
                if (root == null)
                    return;

                string oldName = Path.GetFileNameWithoutExtension(_oldFilePath);
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

                var userPrompted = await _userNotificationServices.CheckPromptForRenameAsync(oldName).ConfigureAwait(false);
                if (userPrompted)
                {
                    var renamedSolution = await _roslynServices.RenameSymbolAsync(newDocument.Project.Solution, symbol, newName).ConfigureAwait(false);

                    var renamedSolutionApplied = await _roslynServices.ApplyChangesToSolutionAsync(newDocument.Project.Solution.Workspace, renamedSolution).ConfigureAwait(false);
                    if (!renamedSolutionApplied)
                    {
                        _userNotificationServices.NotifyRenameFailureAsync(oldName);
                    }
                }
            });
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
