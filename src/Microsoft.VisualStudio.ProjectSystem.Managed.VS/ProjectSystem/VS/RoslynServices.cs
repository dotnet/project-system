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

        public Task<Solution> RenameSymbolAsync(Solution solution, ISymbol symbol, string newName)
        {
            return RoslynRenamer.Renamer.RenameSymbolAsync(solution, symbol, newName, solution.Workspace.Options);
        }
        
        public bool ApplyChangesToSolution(Workspace ws, Solution renamedSolution)
        {
            _threadingService.VerifyOnUIThread();

            // Always make sure TryApplyChanges is called from an UI thread.
            return ws.TryApplyChanges(renamedSolution);
        }
    }
}
