// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Waiting
{
    internal interface IOperationWaitIndicator
    {
        void WaitForOperation(string title, string message, bool allowCancel, Action<CancellationToken> action);
        T WaitForOperation<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> action);
        void WaitForAsyncOperation(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction);
        T WaitForAsyncOperation<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction);
        WaitIndicatorResult WaitForOperationWithResult(string title, string message, bool allowCancel, Action<CancellationToken> action);
        (WaitIndicatorResult, T) WaitForOperationWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> function);
        WaitIndicatorResult WaitForAsyncOperationWithResult(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction);
        (WaitIndicatorResult, T) WaitForAsyncOperationWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction);
    }
}
