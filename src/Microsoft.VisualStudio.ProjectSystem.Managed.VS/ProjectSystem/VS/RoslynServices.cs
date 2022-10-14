// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

using RoslynRenamer = Microsoft.CodeAnalysis.Rename;
using Workspace = Microsoft.CodeAnalysis.Workspace;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IRoslynServices))]
    internal class RoslynServices : IRoslynServices
    {
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public RoslynServices(
            IProjectThreadingService threadingService,
            UnconfiguredProject project)
        {
            _threadingService = threadingService;
            SyntaxFactsServicesImpl = new OrderPrecedenceImportCollection<ISyntaxFactsService>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        protected OrderPrecedenceImportCollection<ISyntaxFactsService> SyntaxFactsServicesImpl { get; }

        private ISyntaxFactsService? SyntaxFactsService
        {
            get
            {
                return SyntaxFactsServicesImpl.FirstOrDefault()?.Value;
            }
        }

        public Task<Solution> RenameSymbolAsync(Solution solution, ISymbol symbol, string newName, CancellationToken token = default)
        {
#pragma warning disable CS0618 // Type or member is obsolete https://github.com/dotnet/project-system/issues/8591
            return RoslynRenamer.Renamer.RenameSymbolAsync(solution, symbol, newName, solution.Workspace.Options, token);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public bool ApplyChangesToSolution(Workspace ws, Solution renamedSolution)
        {
            _threadingService.VerifyOnUIThread();

            // Always make sure TryApplyChanges is called from an UI thread.
            return ws.TryApplyChanges(renamedSolution);
        }

        public bool IsValidIdentifier(string identifierName)
        {
            return SyntaxFactsService?.IsValidIdentifier(identifierName) ?? false;
        }
    }
}
