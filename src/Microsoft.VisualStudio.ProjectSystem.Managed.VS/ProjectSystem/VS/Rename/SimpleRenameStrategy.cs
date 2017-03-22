// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Globalization;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    internal sealed class SimpleRenameStrategy : AbstractRenameStrategy
    {
        private readonly IRoslynServices _roslynServices;

        public SimpleRenameStrategy(IProjectThreadingService threadingService, IUserNotificationServices userNotificationService, IEnvironmentOptions environmentOptions, IRoslynServices roslynServices)
            : base(threadingService, userNotificationService, environmentOptions)
        {
            _roslynServices = roslynServices;
        }

        // For the SimpleRename, it can attempt to handle any situtation
        public override bool CanHandleRename(string oldFileName, string newFileName, bool isCaseSensitive)
        {
            var oldNameBase = Path.GetFileNameWithoutExtension(oldFileName);
            var newNameBase = Path.GetFileNameWithoutExtension(newFileName);
            return SyntaxFacts.IsValidIdentifier(oldNameBase) && SyntaxFacts.IsValidIdentifier(newNameBase) && (!string.Equals(Path.GetFileName(oldNameBase), Path.GetFileName(newNameBase), isCaseSensitive?StringComparison.Ordinal:StringComparison.OrdinalIgnoreCase));
        }

        public override async Task RenameAsync(Project myNewProject, string oldFileName, string newFileName)
        {
            string oldNameBase = Path.GetFileNameWithoutExtension(oldFileName);
            Solution renamedSolution = await GetRenamedSolutionAsync(myNewProject, oldNameBase, newFileName).ConfigureAwait(false);
            if (renamedSolution == null)
                return;

            await _threadingService.SwitchToUIThread();
            var renamedSolutionApplied = _roslynServices.ApplyChangesToSolution(myNewProject.Solution.Workspace, renamedSolution);

            if (!renamedSolutionApplied)
            {
                string failureMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolFailed, oldNameBase);
                await _threadingService.SwitchToUIThread();
                _userNotificationServices.NotifyFailure(failureMessage);
            }
        }

        private async Task<Solution> GetRenamedSolutionAsync(Project myNewProject, string oldNameBase, string newFileName)
        {
            var project = myNewProject;
            Solution renamedSolution = null;
            
            while (project != null)
            {
                var newDocument = GetDocument(project, newFileName);
                if (newDocument == null)
                    return renamedSolution;

                var root = await GetRootNode(newDocument).ConfigureAwait(false);
                if (root == null)
                    return renamedSolution;

                var declarations = root.DescendantNodes().Where(n => HasMatchingSyntaxNode(newDocument, n, oldNameBase));
                var declaration = declarations.FirstOrDefault();
                if (declaration == null)
                    return renamedSolution;

                var semanticModel = await newDocument.GetSemanticModelAsync().ConfigureAwait(false);
                if (semanticModel == null)
                    return renamedSolution;

                var symbol = semanticModel.GetDeclaredSymbol(declaration);
                if (symbol == null)
                    return renamedSolution;

                bool userConfirmed = await CheckUserConfirmation(oldNameBase).ConfigureAwait(false);
                if (!userConfirmed)
                    return renamedSolution;

                string newName = Path.GetFileNameWithoutExtension(newDocument.FilePath);

                // Note that RenameSymbolAsync will return a new snapshot of solution.
                renamedSolution = await _roslynServices.RenameSymbolAsync(newDocument.Project.Solution, symbol, newName).ConfigureAwait(false);
                project = renamedSolution.Projects.Where(p => StringComparers.Paths.Equals(p.FilePath, myNewProject.FilePath)).FirstOrDefault();
            }
            return null;
        }

    }
}
