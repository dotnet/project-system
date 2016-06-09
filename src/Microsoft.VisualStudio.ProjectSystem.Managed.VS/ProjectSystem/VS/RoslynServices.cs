// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Composition;
using RoslynRenamer = Microsoft.CodeAnalysis.Rename;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IRoslynServices"/> that delegates onto 
    /// </summary>
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
        /// <summary>
        ///  Applies the provided solution to the workspace 
        ///  Make sure this is always called from an UI Thread.
        /// </summary>
        /// <param name="ws"></param>
        /// <param name="renamedSolution"></param>
        /// <returns></returns>
        public async Task<bool> ApplyChangesToSolutionAsync(Workspace ws, Solution renamedSolution)
        {
            await _threadingService.SwitchToUIThread();
            return ws.TryApplyChanges(renamedSolution);
        }
    }
}
