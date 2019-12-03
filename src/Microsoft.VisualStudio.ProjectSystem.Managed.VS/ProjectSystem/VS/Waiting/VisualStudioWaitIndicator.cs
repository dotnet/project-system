// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public void Wait(string title, string message, bool allowCancel, Action<CancellationToken> action)
        {
            _ = WaitWithResult(title, message, allowCancel, action);
        }

        public T Wait<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> action)
        {
            if (typeof(T) == typeof(Task))
                throw new ArgumentException("Type argument must not be Task", nameof(T));

            (_, T result) = WaitWithResult(title, message, allowCancel, action);
            return result;
        }

        public void WaitForAsyncFunction(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction)
        {
            _ = WaitForAsyncFunctionWithResult(title, message, allowCancel, asyncFunction);
        }

        public T WaitForAsyncFunction<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction)
        {
            (_, T result) = WaitForAsyncFunctionWithResult(title, message, allowCancel, asyncFunction);
            return result;
        }

        public WaitIndicatorResult WaitForAsyncFunctionWithResult(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction)
        {
            (WaitIndicatorResult waitResult, _) = WaitForOperationImpl(title, message, allowCancel, token =>
            {
                _joinableTaskContext.Factory.Run(() => asyncFunction(token));
                return true;
            });

            return waitResult;
        }

        public WaitIndicatorResult WaitWithResult(string title, string message, bool allowCancel, Action<CancellationToken> action)
        {
            (WaitIndicatorResult waitResult, _) = WaitForOperationImpl(title, message, allowCancel, token =>
            {
                action(token);
                return true;
            });

            return waitResult;
        }

        public (WaitIndicatorResult, T) WaitForAsyncFunctionWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction)
        {
            return WaitForOperationImpl(title, message, allowCancel, token => _joinableTaskContext.Factory.Run(() => asyncFunction(token)));
        }

        public (WaitIndicatorResult, T) WaitWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> function)
        {
            return WaitForOperationImpl(title, message, allowCancel, function);
        }

        private (WaitIndicatorResult, T) WaitForOperationImpl<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> function)
        {
            Assumes.True(_joinableTaskContext.IsOnMainThread);

            using IWaitContext waitContext = StartWait(title, message, allowCancel);

            try
            {
                T result = function(waitContext.CancellationToken);

                return (WaitIndicatorResult.Completed, result);
            }
            catch (OperationCanceledException)
            {
                // TODO track https://github.com/dotnet/roslyn/issues/37069 regarding these suppressions
#pragma warning disable CS8653
                return (WaitIndicatorResult.Canceled, default);
#pragma warning restore CS8653
            }
            catch (AggregateException aggregate) when (aggregate.InnerExceptions.All(e => e is OperationCanceledException))
            {
                // TODO track https://github.com/dotnet/roslyn/issues/37069 regarding these suppressions
#pragma warning disable CS8653
                return (WaitIndicatorResult.Canceled, default);
#pragma warning restore CS8653
            }
        }

        private IWaitContext StartWait(string title, string message, bool allowCancel)
        {
            IVsThreadedWaitDialogFactory? vsThreadedWaitDialogFactory = _waitDialogFactoryService.Value;
            Assumes.Present(vsThreadedWaitDialogFactory);

            return new VisualStudioWaitContext(vsThreadedWaitDialogFactory, title, message, allowCancel);
        }
    }
}
