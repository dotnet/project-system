// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using EnvDTE;

using Microsoft.VisualStudio.ProjectSystem.Refactor;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Refactor
{
    [Export(typeof(IImplicitlyActiveService))]
    [Export(typeof(IRefactorNotifyService))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal sealed partial class VisualStudioRefactorNotifyService : AbstractMultiLifetimeComponent<VisualStudioRefactorNotifyService.Instance>, IImplicitlyActiveService, IRefactorNotifyService
    {
        private readonly IVsService<SDTE, DTE> _dte;
        private readonly IVsService<SVsSolution, IVsSolution> _solutionService;

        [ImportingConstructor]
        public VisualStudioRefactorNotifyService(UnconfiguredProject unconfiguredProject,
                                                 IProjectThreadingService threadingService,
                                                 IVsService<SDTE, DTE> dte,
                                                 IVsService<SVsSolution, IVsSolution> solutionService)
            : base(threadingService.JoinableTaskContext)
        {
            _dte = dte;
            _solutionService = solutionService;
        }

        public Task ActivateAsync() => LoadAsync();
        public Task DeactivateAsync() => UnloadAsync();

        protected override Instance CreateInstance() => new Instance(_dte, _solutionService);

        public async Task<bool> TryOnBeforeGlobalSymbolRenamedAsync(string projectPath, IEnumerable<string> filePaths, string rqName, string newName)
        {
            Requires.NotNull(projectPath, nameof(projectPath));
            Requires.NotNull(filePaths, nameof(filePaths));
            Requires.NotNull(rqName, nameof(rqName));
            Requires.NotNull(newName, nameof(newName));

            Instance instance = await WaitForLoadedAsync();
            return await instance.TryOnBeforeGlobalSymbolRenamedAsync(projectPath, filePaths, rqName, newName);
        }

        public async Task<bool> TryOnAfterGlobalSymbolRenamedAsync(string projectPath, IEnumerable<string> filePaths, string rqName, string newName)
        {
            Requires.NotNull(projectPath, nameof(projectPath));
            Requires.NotNull(filePaths, nameof(filePaths));
            Requires.NotNull(rqName, nameof(rqName));
            Requires.NotNull(newName, nameof(newName));

            Instance instance = await WaitForLoadedAsync();
            return await instance.TryOnAfterGlobalSymbolRenamedAsync(projectPath, filePaths, rqName, newName);
        }
    }
}
