// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Microsoft.VisualStudio.ProjectSystem.VS;

[Export(typeof(ISolutionSource))]
internal sealed class SolutionSource : ISolutionSource
{
    private readonly IVsUIService<SVsSolution, IVsSolution> _solution;
    private readonly JoinableTaskContext _joinableTaskContext;

    [ImportingConstructor]
    public SolutionSource(
        IVsUIService<SVsSolution, IVsSolution> solution,
        JoinableTaskContext joinableTaskContext)
    {
        _solution = solution;
        _joinableTaskContext = joinableTaskContext;
    }

    public IVsSolution Solution
    {
        get
        {
            _joinableTaskContext.VerifyIsOnMainThread();
            return _solution.Value;
        }
    }

    public async Task<IAsyncDisposable> SubscribeAsync(IVsSolutionEvents solutionEvents, CancellationToken cancellationToken)
    {
        await _joinableTaskContext.Factory.SwitchToMainThreadAsync(cancellationToken);

        IVsSolution solution = _solution.Value;

        Verify.HResult(solution.AdviseSolutionEvents(solutionEvents, out uint cookie));

        return new Subscription(solution, _joinableTaskContext.Factory, cookie);
    }

    private sealed class Subscription : IAsyncDisposable
    {
        private readonly IVsSolution _solution;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private int _cookie;

        public Subscription(IVsSolution solution, JoinableTaskFactory joinableTaskFactory, uint cookie)
        {
            _solution = solution;
            _joinableTaskFactory = joinableTaskFactory;
            _cookie = unchecked((int)cookie);
        }

        public async ValueTask DisposeAsync()
        {
            uint cookie = unchecked((uint)Interlocked.Exchange(ref _cookie, (int)VSConstants.VSCOOKIE_NIL));

            if (cookie != VSConstants.VSCOOKIE_NIL)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();

                Verify.HResult(_solution.UnadviseSolutionEvents(cookie));
            }
        }
    }
}
