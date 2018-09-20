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
        private TaskCompletionSource<object> _loadedSource = new TaskCompletionSource<object>();
        private IMultiLifetimeInstance _instance;

        protected AbstractMultiLifetimeComponent(JoinableTaskContextNode joinableTaskContextNode)
            : base(joinableTaskContextNode)
        {
        }

        public IMultiLifetimeInstance Instance
        {
            get { return _instance; }
        }

        /// <summary>
        ///     Gets a task that is completed when current <see cref="AbstractMultiLifetimeComponent"/> has 
        ///     completed loading.
        /// </summary>
        /// <remarks>
        ///     The returned <see cref="Task"/> is canceled when the <see cref="AbstractMultiLifetimeComponent"/> 
        ///     is disposed.
        /// </remarks>
        public Task Loaded
        {
            get { return _loadedSource.Task; }
        }

        public async Task LoadAsync()
        {
            await InitializeAsync();

            await LoadCoreAsync();
        }

        private async Task LoadCoreAsync()
        {
            TaskCompletionSource<object> loadedSource = null;
            IMultiLifetimeInstance instance;
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = CreateInstance();
                    loadedSource = _loadedSource;
                }

                instance = _instance;
            }

            // While all callers should wait on InitializeAsync, 
            // only one should complete the completion source
            await instance.InitializeAsync();
            loadedSource?.SetResult(null);
        }

        public Task UnloadAsync()
        {
            IMultiLifetimeInstance instance = null;

            lock (_lock)
            {
                if (_instance != null)
                {
                    instance = _instance;
                    _instance = null;
                    _loadedSource = new TaskCompletionSource<object>();
                }
            }

            if (instance != null)
            {
                return instance.DisposeAsync();
            }

            return Task.CompletedTask;
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            await UnloadAsync();

            _loadedSource.TrySetCanceled();
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Creates a new instance of the underlying <see cref="IMultiLifetimeInstance"/>.
        /// </summary>
        protected abstract IMultiLifetimeInstance CreateInstance();
    }
}
