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
    [Export(typeof(IWaitIndicator))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal partial class VisualStudioWaitIndicator : AbstractMultiLifetimeComponent<VisualStudioWaitIndicator.Instance>, IImplicitlyActiveService, IWaitIndicator
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly IVsService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory> _waitDialogFactoryService;

        [ImportingConstructor]
        public VisualStudioWaitIndicator(UnconfiguredProject unconfiguredProject,
                                         IProjectThreadingService threadingService,
                                         IVsService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory> waitDialogFactoryService)
            : base(threadingService.JoinableTaskContext)
        {
            _threadingService = threadingService;
            _waitDialogFactoryService = waitDialogFactoryService;
        }

        public Task ActivateAsync() => LoadAsync();
        public Task DeactivateAsync() => UnloadAsync();

        protected override Instance CreateInstance() => new Instance(_threadingService, _waitDialogFactoryService);

        public void Wait(string title, string message, bool allowCancel, Action<CancellationToken> action)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(action, nameof(action));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            instance.Wait(title, message, allowCancel, action);
        }

        public T Wait<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> action)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(action, nameof(action));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            return instance.Wait(title, message, allowCancel, action);
        }

        public void WaitForAsyncFunction(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(asyncFunction, nameof(asyncFunction));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            instance.WaitForAsyncFunction(title, message, allowCancel, asyncFunction);
        }

        public T WaitForAsyncFunction<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(asyncFunction, nameof(asyncFunction));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            return instance.WaitForAsyncFunction(title, message, allowCancel, asyncFunction);
        }

        public WaitIndicatorResult WaitWithResult(string title, string message, bool allowCancel, Action<CancellationToken> action)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(action, nameof(action));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            return instance.WaitWithResult(title, message, allowCancel, action);
        }

        public (WaitIndicatorResult, T) WaitWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, T> function)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(function, nameof(function));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            return instance.WaitWithResult(title, message, allowCancel, function);
        }

        public WaitIndicatorResult WaitForAsyncFunctionWithResult(string title, string message, bool allowCancel, Func<CancellationToken, Task> asyncFunction)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(asyncFunction, nameof(asyncFunction));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            return instance.WaitForAsyncFunctionWithResult(title, message, allowCancel, asyncFunction);
        }

        public (WaitIndicatorResult, T) WaitForAsyncFunctionWithResult<T>(string title, string message, bool allowCancel, Func<CancellationToken, Task<T>> asyncFunction)
        {
            Requires.NotNull(title, nameof(title));
            Requires.NotNull(message, nameof(message));
            Requires.NotNull(asyncFunction, nameof(asyncFunction));

            Instance instance = _threadingService.ExecuteSynchronously(() => WaitForLoadedAsync());
            return instance.WaitForAsyncFunctionWithResult(title, message, allowCancel, asyncFunction);
        }
    }
}
