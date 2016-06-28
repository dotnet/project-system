// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using System.Threading.Tasks;
using System.Globalization;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class Renamer
    {
        private readonly Workspace _workspace;
        private readonly IProjectThreadingService _threadingService;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly IOptionsSettings _optionsSettings;
        private readonly IRoslynServices _roslynServices;
        private readonly Project _project;
        private readonly Document _oldDocument;
        private readonly string _newFilePath;
        private readonly string _oldFilePath;
        private bool _docAdded = false;
        private bool _docRemoved = false;
        private bool _docChanged = false;
        private bool _userPromptedOnce = false;
        private bool _userConfirmedRename = true;

        internal Renamer(Workspace workspace,
                         IProjectThreadingService threadingService,
                         IUserNotificationServices userNotificationServices,
                         IOptionsSettings optionsSettings,
                         IRoslynServices roslynServices,
                         Project project,
                         string oldFilePath,
                         string newFilePath)
        {
            _workspace = workspace;
            _threadingService = threadingService;
            _userNotificationServices = userNotificationServices;
            _optionsSettings = optionsSettings;
            _roslynServices = roslynServices;
            _project = project;
            _newFilePath = newFilePath;
            _oldFilePath = oldFilePath;
            _oldDocument = (from d in project.Documents where StringComparers.Paths.Equals(d.FilePath, oldFilePath) select d).FirstOrDefault();
        }

        public async void OnWorkspaceChangedAsync(object sender, WorkspaceChangeEventArgs args)
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
                _workspace.WorkspaceChanged -= OnWorkspaceChangedAsync;
                var myNewProject = _workspace.CurrentSolution.Projects.Where(p => StringComparers.Paths.Equals(p.FilePath, _project.FilePath)).FirstOrDefault();
                await RenameAsync(myNewProject).ConfigureAwait(false);
            }
        }

        public async Task RenameAsync(Project myNewProject)
        {
            Solution renamedSolution = await GetRenamedSolutionAsync(myNewProject).ConfigureAwait(false);
            if (renamedSolution == null)
                return;

            await _threadingService.SwitchToUIThread();
            var renamedSolutionApplied = _roslynServices.ApplyChangesToSolution(myNewProject.Solution.Workspace, renamedSolution);

            if (!renamedSolutionApplied)
            {
                string failureMessage = string.Format(CultureInfo.CurrentCulture, Resources.RenameSymbolFailed, Path.GetFileNameWithoutExtension(_oldFilePath));
                await _threadingService.SwitchToUIThread();
                _userNotificationServices.NotifyFailure(failureMessage);
            }
        }


        private async Task<Solution> GetRenamedSolutionAsync(Project myNewProject)
        {
            var project = myNewProject;
            Solution renamedSolution = null;
            string oldName = Path.GetFileNameWithoutExtension(_oldFilePath);

            while (project != null)
            {
                Document newDocument = (from d in project.Documents where StringComparers.Paths.Equals(d.FilePath, _newFilePath) select d).FirstOrDefault();
                if (newDocument == null)
                    return renamedSolution;
                
                var root = await newDocument.GetSyntaxRootAsync().ConfigureAwait(false);
                if (root == null)
                    return renamedSolution;

                var declarations = root.DescendantNodes().Where(n => HasMatchingSyntaxNode(newDocument, n, oldName));
                var declaration = declarations.FirstOrDefault();
                if (declaration == null)
                    return renamedSolution;

                var semanticModel = await newDocument.GetSemanticModelAsync().ConfigureAwait(false);
                if (semanticModel == null)
                    return renamedSolution;

                var symbol = semanticModel.GetDeclaredSymbol(declaration);
                if (symbol == null)
                    return renamedSolution;

                bool userConfirmed = await CheckUserConfirmation().ConfigureAwait(false);
                if (!userConfirmed)
                    return renamedSolution;

                string newName = Path.GetFileNameWithoutExtension(newDocument.FilePath);
                
                // Note that RenameSymbolAsync will return a new snapshot of solution.
                renamedSolution = await _roslynServices.RenameSymbolAsync(newDocument.Project.Solution, symbol, newName).ConfigureAwait(false);
                project = renamedSolution.Projects.Where(p => StringComparers.Paths.Equals(p.FilePath, myNewProject.FilePath)).FirstOrDefault();
            }
            return null;
        }

        private async Task<bool> CheckUserConfirmation()
        {
            if (_userPromptedOnce)
            {
                return _userConfirmedRename;
            }

            await _threadingService.SwitchToUIThread();
            var userNeedPrompt = _optionsSettings.GetPropertiesValue("Environment", "ProjectsAndSolution", "PromptForRenameSymbol", false);
            if (userNeedPrompt)
            {
                string renamePromptMessage = string.Format(CultureInfo.CurrentCulture, Resources.RenameSymbolPrompt, Path.GetFileNameWithoutExtension(_oldFilePath));

                await _threadingService.SwitchToUIThread();
                _userConfirmedRename = _userNotificationServices.Confirm(renamePromptMessage);
            }

            _userPromptedOnce = true;
            return _userConfirmedRename;
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
