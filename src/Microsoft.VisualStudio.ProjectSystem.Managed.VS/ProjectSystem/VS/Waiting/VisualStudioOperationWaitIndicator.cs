// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiting
{
    [Export(typeof(IOperationWaitIndicator))]
    internal partial class VisualStudioOperationWaitIndicator : IOperationWaitIndicator
    {
        private readonly IVsUIService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory> _waitDialogFactoryService;
        private readonly JoinableTaskContext _joinableTaskContext;

        [ImportingConstructor]
        public VisualStudioOperationWaitIndicator(IVsUIService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory> waitDialogFactoryService, JoinableTaskContext joinableTaskContext)
        {
            _waitDialogFactoryService = waitDialogFactoryService;
            _joinableTaskContext = joinableTaskContext;
        }

        public void WaitForOperation(string title, string message, bool allowCancel, Action<CancellationToken> action)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(action, nameof(action));

            StaticWaitIndicator.WaitForOperation(_waitDialogFactoryService.Value, title, message, allowCancel, action);
        }

        public T WaitForOperation<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> action)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(action, nameof(action));

            return StaticWaitIndicator.WaitForOperation(_waitDialogFactoryService.Value, title, message, allowCancel, action);
        }

        public void WaitForAsyncOperation(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(asyncFunction, nameof(asyncFunction));

            StaticWaitIndicator.WaitForAsyncOperation(_waitDialogFactoryService.Value, _joinableTaskContext, title, message, allowCancel, asyncFunction);
        }

        public T WaitForAsyncOperation<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(asyncFunction, nameof(asyncFunction));

            return StaticWaitIndicator.WaitForAsyncOperation(_waitDialogFactoryService.Value, _joinableTaskContext, title, message, allowCancel, asyncFunction);
        }

        public WaitIndicatorResult WaitForOperationWithResult(string title, string message, bool allowCancel, Action<CancellationToken> action)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(action, nameof(action));

            return StaticWaitIndicator.WaitForOperationWithResult(_waitDialogFactoryService.Value, title, message, allowCancel, action);
        }

        public (WaitIndicatorResult, T) WaitForOperationWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> function)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(function, nameof(function));

            return StaticWaitIndicator.WaitForOperationWithResult(_waitDialogFactoryService.Value, title, message, allowCancel, function);
        }

        public WaitIndicatorResult WaitForAsyncOperationWithResult(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(asyncFunction, nameof(asyncFunction));

            return StaticWaitIndicator.WaitForAsyncOperationWithResult(_waitDialogFactoryService.Value, _joinableTaskContext, title, message, allowCancel, asyncFunction);
        }

        public (WaitIndicatorResult, T) WaitForAsyncOperationWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(asyncFunction, nameof(asyncFunction));

            return StaticWaitIndicator.WaitForAsyncOperationWithResult(_waitDialogFactoryService.Value, _joinableTaskContext, title, message, allowCancel, asyncFunction);
        }
    }
}
