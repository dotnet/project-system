// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Waiting
{
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IWaitIndicator
    {
        void Wait(string title, string message, bool allowCancel, Action<CancellationToken> action);
        T Wait<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> action);
        void WaitForAsyncFunction(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction);
        T WaitForAsyncFunction<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction);
        WaitIndicatorResult WaitWithResult(string title, string message, bool allowCancel, Action<CancellationToken> action);
        (WaitIndicatorResult, T) WaitWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> function);
        WaitIndicatorResult WaitForAsyncFunctionWithResult(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction);
        (WaitIndicatorResult, T) WaitForAsyncFunctionWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction);
    }
}
