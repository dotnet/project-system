// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Composition;
using RoslynRenamer = Microsoft.CodeAnalysis.Rename;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IRoslynServices))]
    internal class RoslynServices : IRoslynServices
    {
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public RoslynServices(IProjectThreadingService threadingService)
        {
            Requires.NotNull(threadingService, nameof(threadingService));
            _threadingService = threadingService;
        }

        public async Task<Solution> RenameSymbolAsync(Solution solution, ISymbol symbol, string newName)
        {
            var optionSet = solution.Workspace.Options;
            return await RoslynRenamer.Renamer.RenameSymbolAsync(solution, symbol, newName, optionSet).ConfigureAwait(false);
        }
        
        public bool ApplyChangesToSolution(Workspace ws, Solution renamedSolution)
        {
            _threadingService.VerifyOnUIThread();

            // Always make sure TryApplyChanges is called from an UI thread.
            return ws.TryApplyChanges(renamedSolution);
        }
    }
}
