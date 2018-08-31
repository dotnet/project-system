// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     An <see langword="abstract"/> base class that simplifies the lifetime of 
    ///     a component that is loaded and unloaded multiple times.
    /// </summary>
    internal abstract partial class AbstractMultiLifetimeComponent : OnceInitializedOnceDisposedAsync
    {
        private readonly object _lock = new object();
        private AbstractMultiLifetimeInstance _instance;

        protected AbstractMultiLifetimeComponent(JoinableTaskContextNode joinableTaskContextNode)
            : base(joinableTaskContextNode)
        {
        }

        public AbstractMultiLifetimeInstance Instance
        {
            get { return _instance; }
        }

        public async Task LoadAsync()
        {
            await InitializeAsync().ConfigureAwait(false);

            await LoadCoreAsync().ConfigureAwait(false);
        }

        private Task LoadCoreAsync()
        {
            AbstractMultiLifetimeInstance instance;
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = CreateInstance();
                }

                instance = _instance;
            }

            return instance.InitializeAsync();
        }

        public Task UnloadAsync()
        {
            AbstractMultiLifetimeInstance instance = null;

            lock (_lock)
            {
                if (_instance != null)
                {
                    instance = _instance;
                    _instance = null;
                }
            }

            if (instance != null)
            {
                return instance.DisposeAsync();
            }

            return Task.CompletedTask;
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            return UnloadAsync();
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Creates a new instance of the underlying <see cref="AbstractMultiLifetimeInstance"/>.
        /// </summary>
        protected abstract AbstractMultiLifetimeInstance CreateInstance();
    }
}
