// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiting
{
    internal partial class VisualStudioOperationWaitIndicator
    {
        internal static class StaticWaitIndicator
        {
            public static void WaitForOperation(IVsThreadedWaitDialogFactory waitDialogFactory, string title, string message, bool allowCancel, Action<CancellationToken> action)
            {
                _ = WaitForOperationWithResult(waitDialogFactory, title, message, allowCancel, action);
            }

            public static T WaitForOperation<T>(IVsThreadedWaitDialogFactory waitDialogFactory, string title, string message, bool allowCancel, Func<CancellationToken, T> action)
            {
                if (typeof(T) == typeof(Task))
                    throw new ArgumentException(nameof(T));

                (_, T result) = WaitForOperationWithResult(waitDialogFactory, title, message, allowCancel, action);
                return result;
            }

            public static void WaitForAsyncOperation(IVsThreadedWaitDialogFactory waitDialogFactory, JoinableTaskContext joinableTaskContext, string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction)
            {
                _ = WaitForAsyncOperationWithResult(waitDialogFactory, joinableTaskContext, title, message, allowCancel, asyncFunction);
            }

            public static T WaitForAsyncOperation<T>(IVsThreadedWaitDialogFactory waitDialogFactory, JoinableTaskContext joinableTaskContext, string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction)
            {
                (_, T result) = WaitForAsyncOperationWithResult(waitDialogFactory, joinableTaskContext, title, message, allowCancel, asyncFunction);
                return result;
            }

            public static WaitIndicatorResult WaitForAsyncOperationWithResult(IVsThreadedWaitDialogFactory waitDialogFactory, JoinableTaskContext joinableTaskContext, string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction)
            {
                (WaitIndicatorResult waitResult, _) = WaitForOperationImpl(waitDialogFactory, title, message, allowCancel, token =>
                {
                    joinableTaskContext.Factory.Run(() => asyncFunction(token));
                    return true;
                });

                return waitResult;
            }

            public static WaitIndicatorResult WaitForOperationWithResult(IVsThreadedWaitDialogFactory waitDialogFactory, string title, string message, bool allowCancel, Action<CancellationToken> action)
            {
                (WaitIndicatorResult waitResult, _) = WaitForOperationImpl(waitDialogFactory, title, message, allowCancel, token =>
                {
                    action(token);
                    return true;
                });

                return waitResult;
            }

            public static (WaitIndicatorResult, T) WaitForAsyncOperationWithResult<T>(IVsThreadedWaitDialogFactory waitDialogFactory, JoinableTaskContext joinableTaskContext, string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction)
            {
                return WaitForOperationImpl(waitDialogFactory, title, message, allowCancel, token => joinableTaskContext.Factory.Run(() => asyncFunction(token)));
            }

            public static (WaitIndicatorResult, T) WaitForOperationWithResult<T>(IVsThreadedWaitDialogFactory waitDialogFactory, string title, string message, bool allowCancel, Func<CancellationToken, T> function)
            {
                return WaitForOperationImpl(waitDialogFactory, title, message, allowCancel, function);
            }

            private static (WaitIndicatorResult, T) WaitForOperationImpl<T>(IVsThreadedWaitDialogFactory waitDialogFactory, string title, string message, bool allowCancel, Func<CancellationToken, T> function)
            {
                using (IWaitContext waitContext = StartWait(waitDialogFactory, title, message, allowCancel))
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

            private static IWaitContext StartWait(IVsThreadedWaitDialogFactory waitDialogFactory, string title, string message, bool allowCancel)
                => new VisualStudioWaitContext(waitDialogFactory, title, message, allowCancel);
        }
    }
}
