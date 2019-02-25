// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;


namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiting
{
    [Export(typeof(IImplicitlyActiveService))]
    [Export(typeof(IOperationWaitIndicator))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal partial class VisualStudioOperationWaitIndicator : AbstractMultiLifetimeComponent<VisualStudioOperationWaitIndicator.Instance>, IImplicitlyActiveService, IOperationWaitIndicator
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly IVsService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory> _waitDialogFactoryService;

        [ImportingConstructor]
        public VisualStudioOperationWaitIndicator(UnconfiguredProject unconfiguredProject,
                                                  IProjectThreadingService threadingService,
                                                  IVsService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory> waitDialogFactoryService)
            : base(threadingService.JoinableTaskContext)
        {
            _threadingService = threadingService;
            _waitDialogFactoryService = waitDialogFactoryService;
        }

        public Task ActivateAsync() => LoadAsync();
        public Task DeactivateAsync() => UnloadAsync();

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            return base.InitializeCoreAsync(cancellationToken);
        }

        protected override Instance CreateInstance() => new Instance(_threadingService, _waitDialogFactoryService);

        public void WaitForOperation(string title, string message, bool allowCancel, Action<CancellationToken> action)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(action, nameof(action));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            instance.WaitForOperation(title, message, allowCancel, action);
        }

        public T WaitForOperation<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> action)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(action, nameof(action));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            return instance.WaitForOperation(title, message, allowCancel, action);
        }

        public void WaitForAsyncOperation(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(asyncFunction, nameof(asyncFunction));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            instance.WaitForAsyncOperation(title, message, allowCancel, asyncFunction);
        }

        public T WaitForAsyncOperation<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(asyncFunction, nameof(asyncFunction));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            return instance.WaitForAsyncOperation(title, message, allowCancel, asyncFunction);
        }

        public WaitIndicatorResult WaitForOperationWithResult(string title, string message, bool allowCancel, Action<CancellationToken> action)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(action, nameof(action));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            return instance.WaitForOperationWithResult(title, message, allowCancel, action);
        }

        public (WaitIndicatorResult, T) WaitForOperationWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> function)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(function, nameof(function));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            return instance.WaitForOperationWithResult(title, message, allowCancel, function);
        }

        public WaitIndicatorResult WaitForAsyncOperationWithResult(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(asyncFunction, nameof(asyncFunction));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            return instance.WaitForAsyncOperationWithResult(title, message, allowCancel, asyncFunction);
        }

        public (WaitIndicatorResult, T) WaitForAsyncOperationWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(asyncFunction, nameof(asyncFunction));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            return instance.WaitForAsyncOperationWithResult(title, message, allowCancel, asyncFunction);
        }
    }
}
