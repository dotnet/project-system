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

        public WaitIndicatorResult Run(string title, string message, bool allowCancel, Func<IWaitContext, Task> asyncMethod, int totalSteps = 0)
        {
            _joinableTaskContext.VerifyIsOnMainThread();

            using IWaitContext waitContext = new VisualStudioWaitContext(_waitDialogFactoryService.Value, title, message, allowCancel, totalSteps);

            try
            {
#pragma warning disable VSTHRD102 // Deliberate usage  
                _joinableTaskContext.Factory.Run(() => asyncMethod(waitContext));
#pragma warning restore VSTHRD102

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

        public WaitIndicatorResult<T> Run<T>(string title, string message, bool allowCancel, Func<IWaitContext, Task<T>> asyncMethod, int totalSteps = 0)
        {
            _joinableTaskContext.VerifyIsOnMainThread();

            using IWaitContext waitContext = new VisualStudioWaitContext(_waitDialogFactoryService.Value, title, message, allowCancel, totalSteps);

            try
            {
#pragma warning disable VSTHRD102 // Deliberate usage  
                T result = _joinableTaskContext.Factory.Run(() => asyncMethod(waitContext));
#pragma warning restore VSTHRD102

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
