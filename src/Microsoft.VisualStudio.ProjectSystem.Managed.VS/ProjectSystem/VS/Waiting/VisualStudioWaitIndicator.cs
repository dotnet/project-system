// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public WaitIndicatorResult<T> Run<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncMethod) where T : class?
        {
            _joinableTaskContext.VerifyIsOnMainThread();

            using IWaitContext waitContext = StartWait(title, message, allowCancel);

            try
            {
#pragma warning disable VSTHRD102 // Deliberate usage  
                T result = _joinableTaskContext.Factory.Run(() => asyncMethod(waitContext.CancellationToken));
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

        private IWaitContext StartWait(string title, string message, bool allowCancel)
        {
            return new VisualStudioWaitContext(_waitDialogFactoryService.Value, title, message, allowCancel);
        }
    }
}
