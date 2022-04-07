// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class OnceInitializedOnceDisposedUnderLockAsyncTests
    {
        [Fact]
        public void ExecuteUnderLockAsync_NullAsAction_ThrowsArgumentNullException()
        {
            var instance = CreateInstance();

            Assert.ThrowsAsync<ArgumentNullException>(() =>
            {
                return instance.ExecuteUnderLockAsync(null!, CancellationToken.None);
            });
        }

        [Fact]
        public void ExecuteUnderLockAsyncOfT_NullAsAction_ThrowsArgumentNullException()
        {
            var instance = CreateInstance();

            Assert.ThrowsAsync<ArgumentNullException>(() =>
            {
                return instance.ExecuteUnderLockAsync<string>(null!, CancellationToken.None);
            });
        }

        [Fact]
        public async Task ExecuteUnderLockAsync_PassesCancellationTokenToAction()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var instance = CreateInstance();

            bool result = false;
            await instance.ExecuteUnderLockAsync(ct =>
            {
                cancellationTokenSource.Cancel();

                result = ct.IsCancellationRequested;

                return Task.CompletedTask;
            }, cancellationTokenSource.Token);

            Assert.True(result);
        }

        [Fact]
        public async Task ExecuteUnderLockAsyncOfT_PassesCancellationTokenToAction()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var instance = CreateInstance();

            bool result = false;
            await instance.ExecuteUnderLockAsync(ct =>
            {
                cancellationTokenSource.Cancel();

                result = ct.IsCancellationRequested;

                return TaskResult.Null<string>();
            }, cancellationTokenSource.Token);

            Assert.True(result);
        }

        [Fact]
        public async Task ExecuteUnderLockAsync_WhenPassedCancelledToken_DoesNotExecuteAction()
        {
            var cancellationToken = new CancellationToken(canceled: true);

            var instance = CreateInstance();

            bool called = false;
            var result = instance.ExecuteUnderLockAsync(ct => { called = true; return Task.CompletedTask; }, cancellationToken);

            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => result);
            Assert.False(called);
            Assert.Equal(cancellationToken, exception.CancellationToken);
        }

        [Fact]
        public async Task ExecuteUnderLockAsyncOfT_WhenPassedCancelledToken_DoesNotExecuteAction()
        {
            var cancellationToken = new CancellationToken(canceled: true);

            var instance = CreateInstance();

            bool called = false;
            var result = instance.ExecuteUnderLockAsync(ct => { called = true; return TaskResult.Null<string>(); }, cancellationToken);

            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => result);
            Assert.False(called);
            Assert.Equal(cancellationToken, exception.CancellationToken);
        }

        [Fact]
        public async Task ExecuteUnderLockAsync_WithNoContention_ExecutesAction()
        {
            var instance = CreateInstance();

            int callCount = 0;
            await instance.ExecuteUnderLockAsync((ct) => { callCount++; return Task.CompletedTask; }, CancellationToken.None);

            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task ExecuteUnderLockAsyncOfT_WithNoContention_ExecutesAction()
        {
            var instance = CreateInstance();

            int callCount = 0;
            await instance.ExecuteUnderLockAsync((ct) => { callCount++; return TaskResult.Null<string>(); }, CancellationToken.None);

            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task ExecuteUnderLockAsync_AvoidsOverlappingActions()
        {
            var firstEntered = new AsyncManualResetEvent();
            var firstRelease = new AsyncManualResetEvent();
            var secondEntered = new AsyncManualResetEvent();

            var instance = CreateInstance();

            Task firstAction() => instance.ExecuteUnderLockAsync(async (ct) =>
            {
                firstEntered.Set();
                await firstRelease;
            }, CancellationToken.None);

            Task secondAction() => Task.Run(() => instance.ExecuteUnderLockAsync((ct) =>
            {
                secondEntered.Set();
                return Task.CompletedTask;
            }, CancellationToken.None));

            await AssertNoOverlap(firstAction, secondAction, firstEntered, firstRelease, secondEntered);
        }

        [Fact]
        public async Task ExecuteUnderLockAsyncOfT_AvoidsOverlappingActions()
        {
            var firstEntered = new AsyncManualResetEvent();
            var firstRelease = new AsyncManualResetEvent();
            var secondEntered = new AsyncManualResetEvent();

            var instance = CreateInstance();

            Task firstAction() => instance.ExecuteUnderLockAsync(async (ct) =>
            {
                firstEntered.Set();
                await firstRelease;

                return string.Empty;
            }, CancellationToken.None);

            Task secondAction() => Task.Run(() => instance.ExecuteUnderLockAsync((ct) =>
            {
                secondEntered.Set();
                return TaskResult.Null<string>();
            }, CancellationToken.None));

            await AssertNoOverlap(firstAction, secondAction, firstEntered, firstRelease, secondEntered);
        }

        [Fact]
        public async Task ExecuteUnderLockAsyncOfT_AvoidsOverlappingActionsWithExecuteUnderLockAsync()
        {
            var firstEntered = new AsyncManualResetEvent();
            var firstRelease = new AsyncManualResetEvent();
            var secondEntered = new AsyncManualResetEvent();

            var instance = CreateInstance();

            Task firstAction() => instance.ExecuteUnderLockAsync(async (ct) =>
            {
                firstEntered.Set();
                await firstRelease;
            }, CancellationToken.None);

            Task secondAction() => Task.Run(() => instance.ExecuteUnderLockAsync((ct) =>
            {
                secondEntered.Set();
                return TaskResult.Null<string>();
            }, CancellationToken.None));

            await AssertNoOverlap(firstAction, secondAction, firstEntered, firstRelease, secondEntered);
        }

        [Fact]
        public async Task ExecuteUnderLockAsync_AvoidsOverlappingActionsWithExecuteUnderLockAsyncOfT()
        {
            var firstEntered = new AsyncManualResetEvent();
            var firstRelease = new AsyncManualResetEvent();
            var secondEntered = new AsyncManualResetEvent();

            var instance = CreateInstance();

            Task firstAction() => instance.ExecuteUnderLockAsync(async (ct) =>
            {
                firstEntered.Set();
                await firstRelease;

                return string.Empty;
            }, CancellationToken.None);

            Task secondAction() => Task.Run(() => instance.ExecuteUnderLockAsync((ct) =>
            {
                secondEntered.Set();
                return Task.CompletedTask;
            }, CancellationToken.None));

            await AssertNoOverlap(firstAction, secondAction, firstEntered, firstRelease, secondEntered);
        }

        [Fact]
        public async Task ExecuteUnderLockAsync_CanBeNested()
        {
            var instance = CreateInstance();

            int callCount = 0;
            await instance.ExecuteUnderLockAsync(async (_) =>
            {
                await instance.ExecuteUnderLockAsync(async (_) =>
                {
                    await instance.ExecuteUnderLockAsync((_) =>
                    {
                        callCount++;
                        return Task.CompletedTask;
                    });
                });
            });

            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task ExecuteUnderLockAsyncOfT_CanBeNested()
        {
            var instance = CreateInstance();

            int callCount = 0;
            await instance.ExecuteUnderLockAsync(async (_) =>
            {
                await instance.ExecuteUnderLockAsync(async (_) =>
                {
                    await instance.ExecuteUnderLockAsync((_) =>
                    {
                        callCount++;
                        return TaskResult.Null<string>();
                    });

                    return string.Empty;
                });

                return string.Empty;
            });

            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task ExecuteUnderLockAsync_AvoidsOverlappingWithDispose()
        {
            var firstEntered = new AsyncManualResetEvent();
            var firstRelease = new AsyncManualResetEvent();
            var disposeEntered = new AsyncManualResetEvent();

            ConcreteOnceInitializedOnceDisposedUnderLockAsync? instance;

            Task firstAction() => instance.ExecuteUnderLockAsync(async (ct) =>
            {
                firstEntered.Set();
                await firstRelease.WaitAsync();
            }, CancellationToken.None);

            instance = CreateInstance(() =>
            {
                disposeEntered.Set();
                return Task.CompletedTask;
            });

            Task disposeAction() => Task.Run(instance.DisposeAsync);

            await AssertNoOverlap(firstAction, disposeAction, firstEntered, firstRelease, disposeEntered);
        }

        [Fact]
        public async Task ExecuteUnderLockAsyncOfT_AvoidsOverlappingWithDispose()
        {
            var firstEntered = new AsyncManualResetEvent();
            var firstRelease = new AsyncManualResetEvent();
            var disposeEntered = new AsyncManualResetEvent();

            ConcreteOnceInitializedOnceDisposedUnderLockAsync? instance;

            Task firstAction() => instance.ExecuteUnderLockAsync(async (ct) =>
            {
                firstEntered.Set();
                await firstRelease.WaitAsync();

                return string.Empty;
            }, CancellationToken.None);

            instance = CreateInstance(() =>
            {
                disposeEntered.Set();
                return Task.CompletedTask;
            });

            Task disposeAction() => Task.Run(instance.DisposeAsync);

            await AssertNoOverlap(firstAction, disposeAction, firstEntered, firstRelease, disposeEntered);
        }

        [Fact]
        public async Task DisposeAsync_DoesNotThrow()
        {
            var instance = CreateInstance();

            await instance.DisposeAsync();

            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public async Task ExecuteUnderLockAsync_WhenDisposed_ThrowsOperationCanceled()
        {
            var instance = CreateInstance();

            await instance.DisposeAsync();

            var result = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            {
                return instance.ExecuteUnderLockAsync((ct) => { return Task.CompletedTask; }, CancellationToken.None);
            });

            Assert.Equal(instance.DisposalToken, result.CancellationToken);
        }

        [Fact]
        public async Task ExecuteUnderLockAsyncOfT_WhenDisposed_ThrowsOperationCancelled()
        {
            var instance = CreateInstance();

            await instance.DisposeAsync();

            var result = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            {
                return instance.ExecuteUnderLockAsync((ct) => { return TaskResult.Null<string>(); }, CancellationToken.None);
            });

            Assert.Equal(instance.DisposalToken, result.CancellationToken);
        }

        private static async Task AssertNoOverlap(Func<Task> firstAction, Func<Task> secondAction, AsyncManualResetEvent firstEntered, AsyncManualResetEvent firstRelease, AsyncManualResetEvent secondEntered)
        {
            // Run first task and wait until we've entered it
            var firstTask = firstAction();
            await firstEntered.WaitAsync();

            // Run second task, we should never enter it
            var secondTask = secondAction();
            await Assert.ThrowsAsync<TimeoutException>(() => secondEntered.WaitAsync().WithTimeout(TimeSpan.FromMilliseconds(50)));

            // Now release first
            firstRelease.Set();

            // Now we should enter first one
            await secondEntered.WaitAsync();
            await Task.WhenAll(firstTask, secondTask);
        }

        private static ConcreteOnceInitializedOnceDisposedUnderLockAsync CreateInstance(Func<Task>? disposed = null)
        {
            var threadingService = IProjectThreadingServiceFactory.Create();

            return new ConcreteOnceInitializedOnceDisposedUnderLockAsync(threadingService.JoinableTaskContext, disposed);
        }

        private class ConcreteOnceInitializedOnceDisposedUnderLockAsync : OnceInitializedOnceDisposedUnderLockAsync
        {
            private readonly Func<Task> _disposed;

            public ConcreteOnceInitializedOnceDisposedUnderLockAsync(JoinableTaskContextNode joinableTaskContextNode, Func<Task>? disposed)
                : base(joinableTaskContextNode)
            {
                disposed ??= () => Task.CompletedTask;

                _disposed = disposed;
            }

            public new CancellationToken DisposalToken
            {
                get { return base.DisposalToken; }
            }

            public new Task ExecuteUnderLockAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
            {
                return base.ExecuteUnderLockAsync(action, cancellationToken);
            }

            public new Task<T> ExecuteUnderLockAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
            {
                return base.ExecuteUnderLockAsync(action, cancellationToken);
            }

            protected override Task DisposeCoreUnderLockAsync(bool initialized)
            {
                return _disposed();
            }

            protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
