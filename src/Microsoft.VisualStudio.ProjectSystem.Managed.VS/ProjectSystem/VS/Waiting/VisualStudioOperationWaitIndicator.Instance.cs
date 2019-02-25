// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiting
{
    internal partial class VisualStudioOperationWaitIndicator
    {
        internal class Instance : OnceInitializedOnceDisposedAsync, IMultiLifetimeInstance
        {
            private readonly IProjectThreadingService _threadingService;
            private readonly IVsService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory> _threadedWaitDialogFactoryService;
            private IVsThreadedWaitDialogFactory _threadedWaitDialogFactory;

            public Instance(IProjectThreadingService threadingService, IVsService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory> threadedWaitDialogFactoryService)
                : base(threadingService.JoinableTaskContext)
            {
                _threadingService = threadingService;
                _threadedWaitDialogFactoryService = threadedWaitDialogFactoryService;
            }

            public void WaitForOperation(string title, string message, bool allowCancel, Action<CancellationToken> action)
            {
                _ = WaitForOperationWithResult(title, message, allowCancel, action);
            }

            public T WaitForOperation<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> action)
            {
                if (typeof(T) == typeof(Task))
                    throw new ArgumentException(nameof(T));

                (_, T result) = WaitForOperationWithResult(title, message, allowCancel, action);
                return result;
            }

            public void WaitForAsyncOperation(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction)
            {
                _ = WaitForAsyncOperationWithResult(title, message, allowCancel, asyncFunction);
            }

            public T WaitForAsyncOperation<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction)
            {
                (_, T result) = WaitForAsyncOperationWithResult(title, message, allowCancel, asyncFunction);
                return result;
            }

            public WaitIndicatorResult WaitForAsyncOperationWithResult(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction)
            {
                (WaitIndicatorResult waitResult, _) = WaitForOperationImpl(title, message, allowCancel, token =>
                {
                    JoinableFactory.Run(() => asyncFunction(token));
                    return true;
                });

                return waitResult;
            }

            public WaitIndicatorResult WaitForOperationWithResult(string title, string message, bool allowCancel, Action<CancellationToken> action)
            {
                (WaitIndicatorResult waitResult, _) = WaitForOperationImpl(title, message, allowCancel, token =>
                {
                    action(token);
                    return true;
                });

                return waitResult;
            }

            public (WaitIndicatorResult, T) WaitForAsyncOperationWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction)
            {
                return WaitForOperationImpl(title, message, allowCancel, token => JoinableFactory.Run(() => asyncFunction(token)));
            }

            public (WaitIndicatorResult, T) WaitForOperationWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> function)
            {
                return WaitForOperationImpl(title, message, allowCancel, function);
            }

            private (WaitIndicatorResult, T) WaitForOperationImpl<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> function)
            {
                _threadingService.VerifyOnUIThread();
                using (IWaitContext waitContext = StartWait(title, message, allowCancel))
                {
                    try
                    {
                        T result = function(waitContext.CancellationToken);

                        return (WaitIndicatorResult.Completed, result);
                    }
                    catch (OperationCanceledException)
                    {
                        return (WaitIndicatorResult.Canceled, default);
                    }
                    catch (AggregateException aggregate) when (aggregate.InnerExceptions.All(e => e is OperationCanceledException))
                    {
                        return (WaitIndicatorResult.Canceled, default);
                    }
                }
            }

            private IWaitContext StartWait(string title, string message, bool allowCancel)
                => new VisualStudioWaitContext(_threadedWaitDialogFactory, title, message, allowCancel);

            protected override Task DisposeCoreAsync(bool initialized) => Task.CompletedTask;
            protected override Task InitializeCoreAsync(CancellationToken cancellationToken) => InitializeAsync();

            public async Task InitializeAsync()
            {
                await _threadingService.SwitchToUIThread();
                _threadedWaitDialogFactory = await _threadedWaitDialogFactoryService.GetValueAsync();
            }
        }
    }
}
