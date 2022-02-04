// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    [Export(typeof(ISolutionBuildEvents))]
    internal sealed class SolutionBuildEvents : OnceInitializedOnceDisposedAsync, ISolutionBuildEvents
    {
        private readonly IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager2> _solutionBuildManagerService;

        private IVsSolutionBuildManager2? _solutionBuildManager;

        [ImportingConstructor]
        public SolutionBuildEvents(
            IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager2> solutionBuildManagerService,
            JoinableTaskContext joinableTaskContext)
            : base(new(joinableTaskContext))
        {
            _solutionBuildManagerService = solutionBuildManagerService;
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await JoinableFactory.SwitchToMainThreadAsync(cancellationToken);

            _solutionBuildManager = await _solutionBuildManagerService.GetValueAsync(cancellationToken);
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            return Task.CompletedTask;
        }

        public async Task<IAsyncDisposable> SubscribeAsync(IVsUpdateSolutionEvents eventListener)
        {
            await InitializeAsync();

            Assumes.NotNull(_solutionBuildManager);

            HResult.Verify(
                _solutionBuildManager.AdviseUpdateSolutionEvents(eventListener, out uint cookie),
                $"Error advising solution events in {typeof(SolutionBuildEvents)}.");

            return new AsyncDisposable(async () =>
            {
                await JoinableFactory.SwitchToMainThreadAsync();

                HResult.Verify(
                    _solutionBuildManager.UnadviseUpdateSolutionEvents(cookie),
                    $"Error unadvising solution events in {typeof(SolutionBuildEvents)}.");
            });
        }
    }
}
