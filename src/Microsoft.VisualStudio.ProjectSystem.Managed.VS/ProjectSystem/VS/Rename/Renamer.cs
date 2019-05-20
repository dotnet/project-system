// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    internal sealed class Renamer
    {
        private readonly Workspace _workspace;
        private readonly IProjectThreadingService _threadingService;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly IEnvironmentOptions _environmentOptions;
        private readonly IRoslynServices _roslynServices;
        private readonly Project _project;
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
                         IEnvironmentOptions environmentOptions,
                         IRoslynServices roslynServices,
                         Project project,
                         string oldFilePath,
                         string newFilePath)
        {
            _workspace = workspace;
            _threadingService = threadingService;
            _userNotificationServices = userNotificationServices;
            _environmentOptions = environmentOptions;
            _roslynServices = roslynServices;
            _project = project;
            _newFilePath = newFilePath;
            _oldFilePath = oldFilePath;
        }

        public async void OnWorkspaceChangedAsync(object sender, WorkspaceChangeEventArgs args)
        {
            Document oldDocument = (from d in _project.Documents where StringComparers.Paths.Equals(d.FilePath, _oldFilePath) select d).FirstOrDefault();

            if (oldDocument == null)
                return;

            if (args.Kind == WorkspaceChangeKind.DocumentAdded && args.ProjectId == _project.Id)
            {
                Project project = (from p in args.NewSolution.Projects where p.Id.Equals(_project.Id) select p).FirstOrDefault();
                Document addedDocument = (from d in project?.Documents where d.Id.Equals(args.DocumentId) select d).FirstOrDefault();

                if (addedDocument != null && StringComparers.Paths.Equals(addedDocument.FilePath, _newFilePath))
                {
                    _docAdded = true;
                }
            }

            if (args.Kind == WorkspaceChangeKind.DocumentRemoved && args.ProjectId == _project.Id && args.DocumentId == oldDocument.Id)
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
                Project myNewProject = _workspace.CurrentSolution.Projects.Where(p => StringComparers.Paths.Equals(p.FilePath, _project.FilePath)).FirstOrDefault();
                await RenameAsync(myNewProject);
            }
        }

        public async Task RenameAsync(Project project)
        {
            bool isCaseSensitive = await IsCompilationCaseSensitiveAsync(project);
            await RenameAsync(project, _oldFilePath, _newFilePath, isCaseSensitive);
        }

        private async Task RenameAsync(Project project, string oldFilePath, string newFilePath, bool isCaseSensitive)
        {
            string oldNameBase = Path.GetFileNameWithoutExtension(oldFilePath);

            if (!await ShouldRenameAsync(project, oldNameBase, newFilePath, isCaseSensitive))
                return;

            bool userConfirmed = await CheckUserConfirmation(oldNameBase);
            if (!userConfirmed)
                return;

            Solution renamedSolution = await GetRenamedSolutionAsync(project, oldNameBase, newFilePath, isCaseSensitive);
            if (renamedSolution == null)
                return;

            await _threadingService.SwitchToUIThread();
            bool renamedSolutionApplied = _roslynServices.ApplyChangesToSolution(project.Solution.Workspace, renamedSolution);

            if (!renamedSolutionApplied)
            {
                string failureMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolFailed, oldNameBase);
                await _threadingService.SwitchToUIThread();
                _userNotificationServices.ShowWarning(failureMessage);
            }
        }

        private static async Task<bool> ShouldRenameAsync(Project project, string oldNameBase, string newFilePath, bool isCaseSensitive)
        {
            // Do not rename is the name does not change
            string newName = Path.GetFileNameWithoutExtension(newFilePath);
            if (string.Compare(oldNameBase, newName, !isCaseSensitive) == 0)
                return false;

            // Do not rename if we cannot find the symbol
            ISymbol symbol = await TryGetSymbolToRenameAsync(project, oldNameBase, newFilePath, isCaseSensitive);
            return symbol != null;
        }

        private static async Task<ISymbol> TryGetSymbolToRenameAsync(Project myNewProject, string oldNameBase, string newFilePath, bool isCaseSensitive)
        {
            Project project = myNewProject;

            Document newDocument = GetDocument(project, newFilePath);
            if (newDocument == null)
                return null;

            SyntaxNode root = await GetRootNode(newDocument);
            if (root == null)
                return null;

            SemanticModel semanticModel = await newDocument.GetSemanticModelAsync();
            if (semanticModel == null)
                return null;

            IEnumerable<SyntaxNode> declarations = root.DescendantNodes().Where(n => HasMatchingSyntaxNode(semanticModel, n, oldNameBase, isCaseSensitive));
            SyntaxNode declaration = declarations.FirstOrDefault();
            if (declaration == null)
                return null;

            return semanticModel.GetDeclaredSymbol(declaration);
        }

        private async Task<Solution> GetRenamedSolutionAsync(Project myNewProject, string oldNameBase, string newFilePath, bool isCaseSensitive)
        {
            Project project = myNewProject;
            ISymbol symbol = await TryGetSymbolToRenameAsync(myNewProject, oldNameBase, newFilePath, isCaseSensitive);
            if (symbol == null)
                return null;

            string newName = Path.GetFileNameWithoutExtension(newFilePath);
            Solution solutionToRename = null;
            while (project != null)
            {
                // Note that RenameSymbolAsync will return a new snapshot of solution.
                solutionToRename = await _roslynServices.RenameSymbolAsync(project.Solution, symbol, newName);
                project = solutionToRename.Projects.Where(p => StringComparers.Paths.Equals(p.FilePath, myNewProject.FilePath)).FirstOrDefault();
            }

            return solutionToRename;
        }

        private static Document GetDocument(Project project, string filePath) =>
            (from d in project.Documents where StringComparers.Paths.Equals(d.FilePath, filePath) select d).FirstOrDefault();

        private static Task<SyntaxNode> GetRootNode(Document newDocument) =>
            newDocument.GetSyntaxRootAsync();

        private static bool HasMatchingSyntaxNode(SemanticModel model, SyntaxNode syntaxNode, string name, bool isCaseSensitive)
        {
            if (model.GetDeclaredSymbol(syntaxNode) is INamedTypeSymbol symbol &&
                (symbol.TypeKind == TypeKind.Class ||
                 symbol.TypeKind == TypeKind.Interface ||
                 symbol.TypeKind == TypeKind.Delegate ||
                 symbol.TypeKind == TypeKind.Enum ||
                 symbol.TypeKind == TypeKind.Struct ||
                 symbol.TypeKind == TypeKind.Module))
            {
                return string.Compare(symbol.Name, name, !isCaseSensitive) == 0;
            }

            return false;
        }

        private async Task<bool> CheckUserConfirmation(string oldFileName)
        {
            if (_userPromptedOnce)
            {
                return _userConfirmedRename;
            }

            await _threadingService.SwitchToUIThread();
            bool userNeedPrompt = _environmentOptions.GetOption("Environment", "ProjectsAndSolution", "PromptForRenameSymbol", false);
            if (userNeedPrompt)
            {
                string renamePromptMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolPrompt, oldFileName);

                await _threadingService.SwitchToUIThread();
                _userConfirmedRename = _userNotificationServices.Confirm(renamePromptMessage);
            }

            _userPromptedOnce = true;
            return _userConfirmedRename;
        }

        private static async Task<bool> IsCompilationCaseSensitiveAsync(Project project)
        {
            bool isCaseSensitive = false;
            Compilation compilation = await project.GetCompilationAsync();
            if (compilation != null)
            {
                isCaseSensitive = compilation.IsCaseSensitive;
            }

            return isCaseSensitive;
        }
    }
}
