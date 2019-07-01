// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Mocks;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiting
{
    public class VisualStudioWaitIndicatorInstanceTests
    {
        [Fact]
        public static async Task Dispose_Test()
        {
            var (instance, _) = await CreateAsync();
            instance.Dispose();
            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public static async Task DisposeAsync_Test()
        {
            var (instance, _) = await CreateAsync();
            await instance.DisposeAsync();
            Assert.True(instance.IsDisposed);
        }

        [Fact]
        public static async Task UsingBlock_Test()
        {
            var (instance, _) = await CreateAsync();
            using (instance)
            {
                Assert.True(!instance.IsDisposed);
            }
        }

        [Fact]
        public static async Task Wait_Exception_Test()
        {
            var (instance, _) = await CreateAsync();
            using (instance)
            {
                Assert.Throws<Exception>(() =>
                {
                    instance.Wait("", "", false, _
                        => throw new Exception());
                });

                Assert.True(!instance.IsDisposed);
            }
        }

        [Fact]
        public static async Task WaitForAsyncFunction_Wrapped_Exception_Test()
        {
            var (instance, _) = await CreateAsync();
            using (instance)
            {
                Assert.Throws<Exception>(() =>
                {
                    instance.WaitForAsyncFunction("", "", false, async _
                        => await Task.FromException(new Exception()));
                });

                Assert.True(!instance.IsDisposed);
            }
        }

        [Fact]
        public static async Task WaitForAsyncFunction_Wrapped_Canceled_Test()
        {
            var (instance, _) = await CreateAsync();
            using (instance)
            {
                instance.WaitForAsyncFunction("", "", false, async _
                    => await Task.FromException(new OperationCanceledException()));

                Assert.True(!instance.IsDisposed);
            }
        }


        [Fact]
        public static async Task Wait_DoNotReturnTask_Test()
        {
            var (instance, _) = await CreateAsync();
            using (instance)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    instance.Wait("", "", false, async _ =>
                    {
                        await Task.FromException(new OperationCanceledException());
                    });
                });
            }
        }

        [Fact]
        public static async Task WaitForAsyncFunction_Wrapped_Canceled_Test2()
        {
            var (instance, _) = await CreateAsync();
            using (instance)
            {
                instance.WaitForAsyncFunction("", "", false, async _ =>
                {
                    await Task.WhenAll(
                        Task.Run(() => throw new OperationCanceledException()),
                        Task.Run(() => throw new OperationCanceledException()));
                });
                Assert.True(!instance.IsDisposed);
            }
        }

        [Fact]
        public static async Task WaitForAsyncFunction_Wrapped_Canceled_Test3()
        {
            var (instance, _) = await CreateAsync();
            using (instance)
            {
                instance.WaitForAsyncFunction("", "", false, _ =>
                {
                    throw new AggregateException(new[] { new OperationCanceledException(), new OperationCanceledException() });
                });
                Assert.True(!instance.IsDisposed);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncFunction_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            instance.WaitForAsyncFunction(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                return Task.CompletedTask;
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task Wait_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            instance.Wait(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                throw new OperationCanceledException();
            });

            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitWithResult_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test02";
            string message = "Testing02";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            instance.WaitWithResult(title, message, isCancelable, _ =>
            {
                wasCalled = true;
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitWithResult_Canceled_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test02";
            string message = "Testing02";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            instance.WaitWithResult(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                throw new OperationCanceledException();
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncFunction_Test2(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test03";
            string message = "Testing03";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            instance.WaitForAsyncFunction(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                return Task.CompletedTask;
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncFunction_Canceled_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test03";
            string message = "Testing03";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            instance.WaitForAsyncFunction(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                throw new OperationCanceledException();
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncFunctionWithResult_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test04";
            string message = "Testing04";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var result = instance.WaitForAsyncFunctionWithResult(title, message, isCancelable, _ =>
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
        public static async Task WaitForAsyncFunctionWithResult_Canceled_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test04";
            string message = "Testing04";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var result = instance.WaitForAsyncFunctionWithResult(title, message, isCancelable, _ =>
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
        public static async Task Wait_Returns_Test(bool isCancelable)
        {
            string title = "Test05";
            string message = "Testing05";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var result = instance.Wait(title, message, isCancelable, _ =>
            {
                return 42;
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static async Task WaitReturns_Canceled_Test(bool isCancelable)
        {
            string title = "Test05";
            string message = "Testing05";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var result = instance.Wait(title, message, isCancelable, _ =>
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
        public static async Task WaitWithResult_Returns_Test(bool isCancelable)
        {
            string title = "Test06";
            string message = "Testing06";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var (cancelled, result) = instance.WaitWithResult(title, message, isCancelable, _ =>
            {
                return 42;
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static async Task WaitWithResult_Canceled_Test2(bool isCancelable)
        {
            string title = "Test06";
            string message = "Testing06";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var (cancelled, result) = instance.WaitWithResult(title, message, isCancelable, _ =>
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
        public static async Task WaitForAsyncOperation_Returns_Test(bool isCancelable)
        {
            string title = "Test07";
            string message = "Testing07";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var result = instance.WaitForAsyncFunction(title, message, isCancelable, _ =>
            {
                return Task.FromResult(42);
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static async Task WaitForAsyncFunctionReturns_Canceled_Test(bool isCancelable)
        {
            string title = "Test07";
            string message = "Testing07";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            object result = instance.WaitForAsyncFunction(title, message, isCancelable, _ =>
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
        public static async Task WaitForAsyncFunctionWithResult_Returns_Test(bool isCancelable)
        {
            string title = "Test08";
            string message = "Testing08";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var (_, result) = instance.WaitForAsyncFunctionWithResult(title, message, isCancelable, _ =>
            {
                return Task.FromResult(42);
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static async Task WaitForAsyncFunctionWithResult_Returns_Canceled_Test(bool isCancelable)
        {
            string title = "Test08";
            string message = "Testing08";
            var (instance, _) = await CreateAsync(title, message, isCancelable);
            var (canceled, result) = instance.WaitForAsyncFunctionWithResult(title, message, isCancelable, _ =>
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
        public static async Task Wait_Cancellation_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (instance, cancel) = await CreateAsync(title, message, isCancelable);
            instance.Wait(title, message, isCancelable, token =>
            {
                cancel();
                Assert.Equal(isCancelable, token.IsCancellationRequested);
                wasCalled = true;
            });
            Assert.True(wasCalled);
        }

        private static async Task<(VisualStudioWaitIndicator.Instance, Action cancel)> CreateAsync(string title = "", string message = "", bool isCancelable = false)
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var (threadedWaitDialogFactory, cancel) = IVsThreadedWaitDialogFactoryFactory.Create(title, message, isCancelable);
            var threadedWaitDialogFactoryService = IVsServiceFactory.Create<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>(threadedWaitDialogFactory);

            var instance = new VisualStudioWaitIndicator.Instance(threadingService, threadedWaitDialogFactoryService);
            await instance.InitializeAsync();
            return (instance, cancel);
        }
    }
}
