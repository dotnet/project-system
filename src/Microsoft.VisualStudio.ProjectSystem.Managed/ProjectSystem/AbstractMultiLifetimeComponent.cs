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
    internal abstract class AbstractMultiLifetimeComponent<T> : OnceInitializedOnceDisposedAsync
        where T : IMultiLifetimeInstance
    {
        private readonly object _lock = new object();
        private TaskCompletionSource<object> _loadedSource = new TaskCompletionSource<object>();
        private T _instance;

        protected AbstractMultiLifetimeComponent(JoinableTaskContextNode joinableTaskContextNode)
            : base(joinableTaskContextNode)
        {
        }

        public T Instance
        {
            get { return _instance; }
        }

        /// <summary>
        ///     Gets a task that is completed when current <see cref="AbstractMultiLifetimeComponent{T}"/> has 
        ///     completed loading.
        /// </summary>
        /// <remarks>
        ///     The returned <see cref="Task"/> is canceled when the <see cref="AbstractMultiLifetimeComponent{T}"/> 
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
                    _instance = default;
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
        protected abstract T CreateInstance();
    }
}
