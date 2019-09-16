// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable RS0030 // Do not used banned APIs (wrapping IAsyncServiceProvider/SAsyncServiceProvider)

using System;
using System.ComponentModel.Composition;
using System.Threading;
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
    internal class VsService<T> : IVsService<T> where T : class
    {
        private readonly AsyncLazy<T?> _value;

        [ImportingConstructor]
        public VsService([Import(typeof(SAsyncServiceProvider))]IAsyncServiceProvider serviceProvider, JoinableTaskContext joinableTaskContext)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(joinableTaskContext, nameof(joinableTaskContext));

            _value = new AsyncLazy<T?>(async () =>
            {
                // If the service request requires a package load, GetServiceAsync will 
                // happily do that on a background thread.
                object? iunknown = await serviceProvider.GetServiceAsync(ServiceType);

                // We explicitly switch to the UI thread to avoid doing a QueryInterface 
                // via blocking RPC for STA objects when we cast explicitly to the type
                await joinableTaskContext.Factory.SwitchToMainThreadAsync();

                return (T?)iunknown;

            }, joinableTaskContext.Factory);
        }

        public Task<T?> GetValueAsync(CancellationToken cancellationToken = default)
        {
            return _value.GetValueAsync(cancellationToken);
        }

        protected virtual Type ServiceType
        {
            get { return typeof(T); }
        }
    }
}
