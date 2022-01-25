// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
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
        where T : class, IMultiLifetimeInstance
    {
        private readonly SemaphoreSlim _semLock = new(1, 1);

        private TaskCompletionSource<(T instance, JoinableTask initializeAsyncTask)> _instanceTaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected AbstractMultiLifetimeComponent(JoinableTaskContextNode joinableTaskContextNode)
             : base(joinableTaskContextNode)
        {
        }

        /// <summary>
        ///     Returns a task that will complete when current <see cref="AbstractMultiLifetimeComponent{T}"/> has completed
        ///     loading and has published its instance.
        /// </summary>
        /// <exception cref="OperationCanceledException">
        ///     The result is awaited and the <see cref="ConfiguredProject"/> is unloaded.
        ///     <para>
        ///         -or
        ///     </para>
        ///     The result is awaited and <paramref name="cancellationToken"/> is cancelled.
        /// </exception>
        /// <remarks>
        ///     This method does not initiate loading of the <see cref="AbstractMultiLifetimeComponent{T}"/>, however,
        ///     it will join the load when it starts.
        /// </remarks>
        protected async Task<T> WaitForLoadedAsync(CancellationToken cancellationToken = default)
        {
            await _semLock.WaitAsync();

            // Wait until LoadAsync has been called, force switching to thread-pool in case
            // there's already someone waiting for us on the UI thread.
#pragma warning disable RS0030 // Do not used banned APIs
            (T instance, JoinableTask initializeAsyncTask) = await _instanceTaskSource.Task.WithCancellation(cancellationToken)
                                                                                           .ConfigureAwait(false);
#pragma warning restore RS0030

            _semLock.Release();

            // Now join Instance.InitializeAsync so that if someone is waiting on the UI thread for us, 
            // the instance is allowed to switch to that thread to complete if needed.
            await initializeAsyncTask.JoinAsync(cancellationToken);

            return instance;
        }

        public async Task LoadAsync()
        {
            await InitializeAsync();

            await LoadCoreAsync();
        }

        public async Task LoadCoreAsync()
        {
            await _semLock.WaitAsync();

            try
            {
                if (!_instanceTaskSource.Task.IsCompleted)
                {
                    (T instance, JoinableTask initializeAsyncTask) result = CreateInitializedInstance();
                    _instanceTaskSource.SetResult(result);
                }

                Assumes.True(_instanceTaskSource.Task.IsCompleted);

                // Should throw TaskCanceledException if already cancelled in Dispose
                (T instance, JoinableTask initializeAsyncTask)? oldInstanceTask = await _instanceTaskSource.Task;

                await oldInstanceTask.Value.initializeAsyncTask;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _semLock.Release();
            }
        }

        public async Task UnloadAsync()
        {
            await _semLock.WaitAsync();

            try
            {
                (T instance, JoinableTask initializeAsyncTask)? oldInstanceTask = null;

                if (_instanceTaskSource.Task.IsCompleted)
                {
                    // Should throw TaskCanceledException if already cancelled in Dispose
                    oldInstanceTask = await _instanceTaskSource.Task;
                    _instanceTaskSource = new TaskCompletionSource<(T instance, JoinableTask initializeAsyncTask)>(TaskCreationOptions.RunContinuationsAsynchronously);
                }

                if (oldInstanceTask != null && oldInstanceTask.Value.instance != null)
                {
                    await oldInstanceTask.Value.instance.DisposeAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _semLock.Release();
            }
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            await UnloadAsync();

            await _semLock.WaitAsync();

            _instanceTaskSource.TrySetCanceled();

            _semLock.Release();
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Creates a new instance of the underlying <see cref="IMultiLifetimeInstance"/>.
        /// </summary>
        protected abstract T CreateInstance();

        private (T instance, JoinableTask initializeAsyncTask) CreateInitializedInstance()
        {
            T instance = CreateInstance();

            JoinableTask initializeAsyncTask = JoinableFactory.RunAsync(instance.InitializeAsync);

            return (instance, initializeAsyncTask);
        }
    }
}
