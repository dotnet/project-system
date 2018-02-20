// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     An <see langword="abstract"/> base class that simplifies the lifetime of a
    ///     <see cref="IProjectDynamicLoadComponent"/> implementation.
    /// </summary>
    internal abstract partial class AbstractProjectDynamicLoadComponent : OnceInitializedOnceDisposedAsync, IProjectDynamicLoadComponent
    {
        private readonly object _lock = new object();
        private AbstractProjectDynamicLoadInstance _instance;

        protected AbstractProjectDynamicLoadComponent(JoinableTaskContextNode joinableTaskContextNode)
            : base(joinableTaskContextNode)
        {
        }

        public Task LoadAsync()
        {
            AbstractProjectDynamicLoadInstance instance;
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
            AbstractProjectDynamicLoadInstance instance = null;

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
        ///     Creates a new instance of the underlying <see cref="AbstractProjectDynamicLoadInstance"/>.
        /// </summary>
        protected abstract AbstractProjectDynamicLoadInstance CreateInstance();
    }
}
