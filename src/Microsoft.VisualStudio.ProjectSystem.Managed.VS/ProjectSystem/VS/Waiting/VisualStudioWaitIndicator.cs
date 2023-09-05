// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiting
{
    [Export(typeof(IWaitIndicator))]
    internal partial class VisualStudioWaitIndicator : IWaitIndicator
    {
        private readonly JoinableTaskContext _joinableTaskContext;
        private readonly IVsUIService<IVsThreadedWaitDialogFactory> _waitDialogFactoryService;

        [ImportingConstructor]
        public VisualStudioWaitIndicator(JoinableTaskContext joinableTaskContext,
                                         IVsUIService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory> waitDialogFactoryService)
        {
            _joinableTaskContext = joinableTaskContext;
            _waitDialogFactoryService = waitDialogFactoryService;
        }

        public async Task<WaitIndicatorResult> RunAsync(string title, string message, bool allowCancel, Func<IWaitContext, Task> asyncMethod, int totalSteps = 0)
        {
            await _joinableTaskContext.Factory.SwitchToMainThreadAsync();

            using IWaitContext waitContext = new VisualStudioWaitContext(_waitDialogFactoryService.Value, title, message, allowCancel, totalSteps);

            try
            {
                await asyncMethod(waitContext);

                return WaitIndicatorResult.Completed;
            }
            catch (OperationCanceledException)
            {
                return WaitIndicatorResult.Cancelled;
            }
            catch (AggregateException aggregate) when (aggregate.InnerExceptions.All(e => e is OperationCanceledException))
            {
                return WaitIndicatorResult.Cancelled;
            }
        }

        public async Task<WaitIndicatorResult<T>> RunAsync<T>(string title, string message, bool allowCancel, Func<IWaitContext, Task<T>> asyncMethod, int totalSteps = 0)
        {
            await _joinableTaskContext.Factory.SwitchToMainThreadAsync();

            using IWaitContext waitContext = new VisualStudioWaitContext(_waitDialogFactoryService.Value, title, message, allowCancel, totalSteps);

            try
            {
                T result = await asyncMethod(waitContext);

                return WaitIndicatorResult<T>.FromResult(result);
            }
            catch (OperationCanceledException)
            {
                return WaitIndicatorResult<T>.Cancelled;
            }
            catch (AggregateException aggregate) when (aggregate.InnerExceptions.All(e => e is OperationCanceledException))
            {
                return WaitIndicatorResult<T>.Cancelled;
            }
        }
    }
}
