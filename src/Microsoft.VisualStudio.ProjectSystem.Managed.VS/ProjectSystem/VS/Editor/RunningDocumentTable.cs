// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [Export(typeof(IRunningDocumentTable))]
    internal sealed class RunningDocumentTable : OnceInitializedOnceDisposedAsync, IRunningDocumentTable
    {
        private readonly IVsService<SVsRunningDocumentTable, IVsRunningDocumentTable> _rdtService;

        private IVsRunningDocumentTable? _rdt;

        [ImportingConstructor]
        public RunningDocumentTable(
            IVsService<SVsRunningDocumentTable, IVsRunningDocumentTable> rdtService,
            JoinableTaskContext joinableTaskContext)
            : base(new(joinableTaskContext))
        {
            _rdtService = rdtService;
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await JoinableFactory.SwitchToMainThreadAsync(cancellationToken);

            _rdt = await _rdtService.GetValueAsync(cancellationToken);
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            return Task.CompletedTask;
        }

        public async Task<IAsyncDisposable> SubscribeEventsAsync(IVsRunningDocTableEvents eventListener)
        {
            await InitializeAsync();

            Assumes.NotNull(_rdt);

            HResult.Verify(
                _rdt.AdviseRunningDocTableEvents(eventListener, out uint cookie),
                $"Error advising RDT events in {typeof(RunningDocumentTable)}.");

            return new AsyncDisposable(async () =>
            {
                await JoinableFactory.SwitchToMainThreadAsync();

                HResult.Verify(
                    _rdt.UnadviseRunningDocTableEvents(cookie),
                    $"Error unadvising RDT events in {typeof(RunningDocumentTable)}.");
            });
        }
    }
}
