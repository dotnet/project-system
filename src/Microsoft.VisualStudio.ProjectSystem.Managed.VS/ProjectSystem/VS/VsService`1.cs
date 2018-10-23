// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IVsService{T}"/> that calls into Visual Studio's <see cref="IAsyncServiceProvider"/>.
    /// </summary>
    [Export(typeof(IVsService<>))]
    internal class VsService<T>
    {
        private readonly AsyncLazy<T> _value;
        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public VsService([Import(typeof(SAsyncServiceProvider))]IAsyncServiceProvider serviceProvider, IProjectThreadingService threadingService)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(threadingService, nameof(threadingService));

            _value = new AsyncLazy<T>(GetServiceAsync, threadingService.JoinableTaskFactory);
            _serviceProvider = serviceProvider;
            _threadingService = threadingService;
        }

        public Task<T> GetValueAsync()
        {
            return _value.GetValueAsync();
        }

        protected virtual Type ServiceType
        {
            get { return typeof(T); }
        }

        private async Task<T> GetServiceAsync()
        {
            // If the service request requires a package load, GetServiceAsync will 
            // happily do that on a background thread.
            object iunknown = await _serviceProvider.GetServiceAsync(ServiceType);

            // We explicitly switch to the UI thread to avoid doing a QueryInterface 
            // via blocking RPC for STA objects when we cast explicitly to the type
            await _threadingService.SwitchToUIThread();

            return (T)iunknown;
        }
    }
}
