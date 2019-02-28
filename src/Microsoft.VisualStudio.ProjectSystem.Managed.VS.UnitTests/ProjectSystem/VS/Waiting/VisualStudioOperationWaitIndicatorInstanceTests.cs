// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Mocks;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiting
{
    public class VisualStudioOperationWaitIndicatorInstanceTests
    {
        [Fact]
        public static async Task Dispose()
        {
            var (instance, _) = await CreateAsync();
            instance.Dispose();
            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public static async Task DisposeAsync()
        {
            var (instance, _) = await CreateAsync();
            await instance.DisposeAsync();
            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public static async Task UsingBlock()
        {
            var (instance, _) = await CreateAsync();
            using (instance)
            {
                Assert.True(!instance.IsDisposed);
            }
        }

        [Fact]
        public static async Task WaitForOperation_Exception()
        {
            var (instance, _) = await CreateAsync();
            using (instance)
            {
                Assert.Throws<Exception>(() =>
                {
                    instance.WaitForAsyncOperation("", "", false, _
                        => throw new Exception());
                });

                Assert.True(!instance.IsDisposed);
            }
        }

        [Fact]
        public static async Task WaitForOperation_Wrapped_Exception()
        {
            var (instance, _) = await CreateAsync();
            using (instance)
            {
                Assert.Throws<Exception>(() =>
                {
                    instance.WaitForAsyncOperation("", "", false, async _
                        => await Task.FromException(new Exception()));
                });

                Assert.True(!instance.IsDisposed);
            }
        }

        [Fact]
        public static async Task WaitForOperation_Wrapped_Canceled()
        {
            var (instance, _) = await CreateAsync();
            using (instance)
            {
                instance.WaitForAsyncOperation("", "", false, async _
                    => await Task.FromException(new OperationCanceledException()));

                Assert.True(!instance.IsDisposed);
            }
        }


        [Fact]
        public static async Task WaitForOperation_DoNotReturnTask()
        {
            var (instance, _) = await CreateAsync();
            using (instance)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    instance.WaitForOperation("", "", false, async _ =>
                    {
                        await Task.FromException(new OperationCanceledException());
                    });
                });
            }
        }

        [Fact]
        public static async Task WaitForOperation_Wrapped_Canceled2()
        {
            var (instance, _) = await CreateAsync();
            using (instance)
            {
                instance.WaitForAsyncOperation("", "", false, async _ =>
                {
                    await Task.WhenAll(
                        Task.Run(() => throw new OperationCanceledException()),
                        Task.Run(() => throw new OperationCanceledException()));
                });
                Assert.True(!instance.IsDisposed);
            }
        }

        [Fact]
        public static async Task WaitForOperation_Wrapped_Canceled3()
        {
            var (instance, _) = await CreateAsync();
            using (instance)
            {
                instance.WaitForOperation("", "", false, _ =>
                {
                    throw new AggregateException(new[] { new OperationCanceledException(), new OperationCanceledException() });
                });
                Assert.True(!instance.IsDisposed);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForOperation(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            instance.WaitForOperation(title, message, isCancelable, _ =>
            {
                wasCalled = true;
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForOperationCanceled(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            instance.WaitForOperation(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                throw new OperationCanceledException();
            });

            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForOperationWithResult(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test02";
            string message = "Testing02";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            instance.WaitForOperationWithResult(title, message, isCancelable, _ =>
            {
                wasCalled = true;
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForOperationWithResultCanceled(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test02";
            string message = "Testing02";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            instance.WaitForOperationWithResult(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                throw new OperationCanceledException();
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncOperation(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test03";
            string message = "Testing03";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            instance.WaitForAsyncOperation(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                return Task.CompletedTask;
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncOperationCanceled(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test03";
            string message = "Testing03";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            instance.WaitForAsyncOperation(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                throw new OperationCanceledException();
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncOperationWithResult(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test04";
            string message = "Testing04";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var result = instance.WaitForAsyncOperationWithResult(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                return Task.CompletedTask;
            });
            Assert.True(wasCalled);
            Assert.Equal(WaitIndicatorResult.Completed, result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncOperationWithResultCanceled(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test04";
            string message = "Testing04";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var result = instance.WaitForAsyncOperationWithResult(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                throw new OperationCanceledException();
            });
            Assert.True(wasCalled);
            Assert.Equal(WaitIndicatorResult.Canceled, result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForOperationReturns(bool isCancelable)
        {
            string title = "Test05";
            string message = "Testing05";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var result = instance.WaitForOperation(title, message, isCancelable, _ =>
            {
                return 42;
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static async Task WaitForOperationReturnsCanceled(bool isCancelable)
        {
            string title = "Test05";
            string message = "Testing05";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var result = instance.WaitForOperation(title, message, isCancelable, _ =>
            {
                if (isCancelable)
                {
                    throw new OperationCanceledException();
                }

                return 42;
            });
            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForOperationWithResultReturns(bool isCancelable)
        {
            string title = "Test06";
            string message = "Testing06";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var (cancelled, result) = instance.WaitForOperationWithResult(title, message, isCancelable, _ =>
            {
                return 42;
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static async Task WaitForOperationWithResultReturnsCanceled(bool isCancelable)
        {
            string title = "Test06";
            string message = "Testing06";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var (cancelled, result) = instance.WaitForOperationWithResult(title, message, isCancelable, _ =>
            {
                if (isCancelable)
                {
                    throw new OperationCanceledException();
                }

                return 42;
            });
            Assert.Equal(0, result);
            Assert.Equal(WaitIndicatorResult.Canceled, cancelled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncOperationReturns(bool isCancelable)
        {
            string title = "Test07";
            string message = "Testing07";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var result = instance.WaitForAsyncOperation(title, message, isCancelable, _ =>
            {
                return Task.FromResult(42);
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static async Task WaitForAsyncOperationReturnsCanceled(bool isCancelable)
        {
            string title = "Test07";
            string message = "Testing07";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            object result = instance.WaitForAsyncOperation(title, message, isCancelable, _ =>
            {
                if (isCancelable)
                {
                    throw new OperationCanceledException();
                }

                return Task.FromResult(default(object));
            });
            Assert.Null(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncOperationWithResultReturns(bool isCancelable)
        {
            string title = "Test08";
            string message = "Testing08";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var (_, result) = instance.WaitForAsyncOperationWithResult(title, message, isCancelable, _ =>
            {
                return Task.FromResult(42);
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static async Task WaitForAsyncOperationWithResultReturnsCanceled(bool isCancelable)
        {
            string title = "Test08";
            string message = "Testing08";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var (canceled, result) = instance.WaitForAsyncOperationWithResult(title, message, isCancelable, _ =>
            {
                if (isCancelable)
                {
                    throw new OperationCanceledException();
                }

                return Task.FromResult(42);
            });
            Assert.Equal(0, result);
            Assert.Equal(WaitIndicatorResult.Canceled, canceled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForOperation_Cancellation(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (instance, cancel) = await CreateAsync(title, message, isCancelable);
            instance.WaitForOperation(title, message, isCancelable, token =>
            {
                cancel();
                Assert.Equal(isCancelable, token.IsCancellationRequested);
                wasCalled = true;
            });
            Assert.True(wasCalled);
        }

        private static async Task<(VisualStudioOperationWaitIndicator.Instance, Action cancel)> CreateAsync(string title = "", string message = "", bool isCancelable = false)
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var threadedWaitDialogFactoryServiceMock = new Mock<IVsService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>>();
            var (threadedWaitDialogFactory, cancel) = IVsThreadedWaitDialogFactoryFactory.Create(title, message, isCancelable);
            threadedWaitDialogFactoryServiceMock.Setup(m => m.GetValueAsync(default)).ReturnsAsync(threadedWaitDialogFactory);

            var instance = new VisualStudioOperationWaitIndicator.Instance(threadingService, threadedWaitDialogFactoryServiceMock.Object);
            await instance.InitializeAsync();
            return (instance, cancel);
        }
    }
}
