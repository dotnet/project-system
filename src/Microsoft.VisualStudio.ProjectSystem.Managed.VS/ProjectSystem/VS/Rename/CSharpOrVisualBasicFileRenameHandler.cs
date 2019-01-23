// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
//--------------------------------------------------------------------------------------------
// <summary>
// CSharpOrVisualBasicFileRenameHandler
//
// Exports an IFileRenameHandler to be notified of file renames. If the file being renamed
// is a code file, it will prompt the user to rename the class to match. The rename is done
// using Roslyn Renamer API
// </summary>
//--------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers.Rename;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    [Export(typeof(IFileRenameHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class CSharpOrVisualBasicFileRenameHandler : IFileRenameHandler
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly Workspace _workspace;
        private readonly IEnvironmentOptions _environmentOptions;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly IRoslynServices _roslynServices;

        [ImportingConstructor]
        public CSharpOrVisualBasicFileRenameHandler(IUnconfiguredProjectVsServices projectVsServices,
                                                    VisualStudioWorkspace workspace,
                                                    IEnvironmentOptions environmentOptions,
                                                    IUserNotificationServices userNotificationServices,
                                                    IRoslynServices roslynServices)
            : this(projectVsServices, workspace as Workspace, environmentOptions, userNotificationServices, roslynServices)
        {
        }

        internal CSharpOrVisualBasicFileRenameHandler(IUnconfiguredProjectVsServices projectVsServices,
                                            Workspace workspace,
                                            IEnvironmentOptions environmentOptions,
                                            IUserNotificationServices userNotificationServices,
                                            IRoslynServices roslynServices)
        {
            _projectVsServices = projectVsServices;
            _workspace = workspace;
            _environmentOptions = environmentOptions;
            _userNotificationServices = userNotificationServices;
            _roslynServices = roslynServices;
        }

        public void HandleRename(string oldFilePath, string newFilePath)
        {
            // We do not need to block the completion of HandleRename so we queue it using the threading service
            // and ignore the result.  
            // NOTE: we queue work on JTF queue so VS shutdown can happen cleanly
            _projectVsServices.ThreadingService.Fork(() => HandleRenameAsync(oldFilePath, newFilePath),
                factory: _projectVsServices.ThreadingService.JoinableTaskFactory,
                unconfiguredProject: _projectVsServices.Project);
        }

        internal async Task HandleRenameAsync(string oldFilePath, string newFilePath)
        {
            // Do not offer to rename types if the user changes the file extensions
            if (!oldFilePath.EndsWith(Path.GetExtension(newFilePath), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // see if the current project contains a compilation
            Project project = GetCurrentProject();
            (bool success, bool isCaseSensitive) = await TryDetermineIfCompilationIsCaseSensitiveAsync(project);
            if (!success)
                return;

            // Check that the new name is a valid identifier in the the current programming language
            string oldName = Path.GetFileNameWithoutExtension(oldFilePath);
            string newName = Path.GetFileNameWithoutExtension(newFilePath);
            if (!CanHandleRename(oldName, newName, isCaseSensitive))
                return;

            // Check if there are any symbols that need to be renamed
            (success, _) = await TryGetSymbolToRename(project, oldName, newFilePath, isCaseSensitive);
            if (!success)
                return;

            // Ask if the user wants to rename the symbol
            bool userConfirmed = await CheckUserConfirmation(oldName);
            if (!userConfirmed)
                return;

            // Perform the rename operation
            Solution renamedSolution = await GetRenamedSolutionAsync(oldName, newFilePath, isCaseSensitive);
            if (renamedSolution == null)
                return;

            // Try and apply the changes to the current solution
            await _projectVsServices.ThreadingService.SwitchToUIThread();
            bool renamedSolutionApplied = _roslynServices.ApplyChangesToSolution(renamedSolution.Workspace, renamedSolution);

            // Notify the user if the rename could not be performed
            if (!renamedSolutionApplied)
            {
                string failureMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolFailed, oldName);
                _userNotificationServices.ShowWarning(failureMessage);
            }
        }

        private Project GetCurrentProject()
        {
            return _workspace
                 .CurrentSolution
                 .Projects.Where(p => StringComparers.Paths.Equals(p.FilePath, _projectVsServices.Project.FullPath))
                 .FirstOrDefault();
        }

        private static async Task<(bool success, bool isCaseSensitive)> TryDetermineIfCompilationIsCaseSensitiveAsync(Project project)
        {
            Compilation compilation = await project.GetCompilationAsync();
            if (compilation == null)
            {
                // this project does not support compilations
                return (false, false);
            }

            return (true, compilation.IsCaseSensitive);
        }

        private bool CanHandleRename(string oldName, string newName, bool isCaseSensitive)
            => _roslynServices.IsValidIdentifier(oldName) &&
               _roslynServices.IsValidIdentifier(newName) &&
              (!string.Equals(
                  oldName,
                  newName,
                  isCaseSensitive
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase));

        private static async Task<(bool success, ISymbol symbolToRename)> TryGetSymbolToRename(Project project, string oldName, string newFileName, bool isCaseSensitive)
        {
            if (project == null)
                return (false, null);

            Document newDocument = GetDocument(project, newFileName);
            if (newDocument == null)
                return (false, null);

            SyntaxNode root = await GetRootNode(newDocument);
            if (root == null)
                return (false, null);

            SemanticModel semanticModel = await newDocument.GetSemanticModelAsync();
            if (semanticModel == null)
                return (false, null);

            IEnumerable<SyntaxNode> declarations = root.DescendantNodes().Where(n => HasMatchingSyntaxNode(semanticModel, n, oldName, isCaseSensitive));
            SyntaxNode declaration = declarations.FirstOrDefault();
            if (declaration == null)
                return (false, null);

            return (true, semanticModel.GetDeclaredSymbol(declaration));
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
            await _projectVsServices.ThreadingService.SwitchToUIThread();
            bool userNeedPrompt = _environmentOptions.GetOption("Environment", "ProjectsAndSolution", "PromptForRenameSymbol", false);
            if (userNeedPrompt)
            {
                string renamePromptMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolPrompt, oldFileName);

                await _projectVsServices.ThreadingService.SwitchToUIThread();
                return _userNotificationServices.Confirm(renamePromptMessage);
            }

            return true;
        }

        private async Task<Solution> GetRenamedSolutionAsync(string oldName, string newFileName, bool isCaseSensitive)
        {
            Project project = GetCurrentProject();

            (bool success, ISymbol symbolToRename) = await TryGetSymbolToRename(project, oldName, newFileName, isCaseSensitive);
            if (!success)
                return null;

            string newName = Path.GetFileNameWithoutExtension(newFileName);

            Solution renamedSolution = await _roslynServices.RenameSymbolAsync(project.Solution, symbolToRename, newName);
            return renamedSolution;
        }
    }
}
