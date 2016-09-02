// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
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

        public SimpleRenameStrategy(IProjectThreadingService threadingService, IUserNotificationServices userNotificationService, IOptionsSettings optionsSettings, IRoslynServices roslynServices)
            : base(threadingService, userNotificationService, optionsSettings)
        {
            _roslynServices = roslynServices;
        }

        // For the SimpleRename, it can attempt to handle any situtation
        public override bool CanHandleRename(string oldFileName, string newFileName)
        {
            var oldNameBase = Path.GetFileNameWithoutExtension(oldFileName);
            var newNameBase = Path.GetFileNameWithoutExtension(newFileName);
            return SyntaxFacts.IsValidIdentifier(oldNameBase) && SyntaxFacts.IsValidIdentifier(newNameBase);
        }

        public override async Task RenameAsync(Project myNewProject, string oldFileName, string newFileName)
        {
            Solution renamedSolution = await GetRenamedSolutionAsync(myNewProject, oldFileName, newFileName).ConfigureAwait(false);
            if (renamedSolution == null)
                return;

            await _threadingService.SwitchToUIThread();
            var renamedSolutionApplied = _roslynServices.ApplyChangesToSolution(myNewProject.Solution.Workspace, renamedSolution);

            if (!renamedSolutionApplied)
            {
                string failureMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolFailed, oldFileName);
                await _threadingService.SwitchToUIThread();
                _userNotificationServices.NotifyFailure(failureMessage);
            }
        }

        private async Task<Solution> GetRenamedSolutionAsync(Project myNewProject, string oldFileName, string newFileName)
        {
            var project = myNewProject;
            Solution renamedSolution = null;
            string oldNameBase = Path.GetFileNameWithoutExtension(oldFileName);

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

                bool userConfirmed = await CheckUserConfirmation(oldFileName).ConfigureAwait(false);
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
