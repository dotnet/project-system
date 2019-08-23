// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiting
{
    public class VisualStudioWaitIndicatorTests
    {
        [Fact]
        public static async Task Dispose_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.ActivateAsync();
            Assert.False(waitIndicator.IsDisposed);
            waitIndicator.Dispose();
            Assert.True(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task DisposeAsync_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.ActivateAsync();
            Assert.False(waitIndicator.IsDisposed);
            await waitIndicator.DisposeAsync();
            Assert.True(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task DisposeBlock_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.ActivateAsync();
            using (waitIndicator)
            {
                Assert.False(waitIndicator.IsDisposed);
            }
            Assert.True(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task DeactivateAsync_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.ActivateAsync();
            await waitIndicator.DeactivateAsync();
            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task ActivateAsyncTwice_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.ActivateAsync();
            await waitIndicator.ActivateAsync();
            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task DeactivateTwiceAsync_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.DeactivateAsync();
            await waitIndicator.DeactivateAsync();
            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task LoadAsyncAndUnloadAsync_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();
            await waitIndicator.UnloadAsync();
            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task LoadAsyncTwice_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();
            await waitIndicator.LoadAsync();
            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task UnloadAsyncTwice_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.UnloadAsync();
            await waitIndicator.UnloadAsync();
            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task Wait_ArgumentNullException_Test(string title, string message)
        {
            bool isCancelable = false;
            var (waitIndicator, cancel) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            using (waitIndicator)
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    waitIndicator.Wait(title, message, isCancelable, _ =>
                    {
                        throw new Exception();
                    });
                });

                Assert.False(waitIndicator.IsDisposed);
            }
        }

        [Fact]
        public static async Task Wait_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, cancel) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            using (waitIndicator)
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    waitIndicator.Wait("", "", false, null!);
                });

                Assert.False(waitIndicator.IsDisposed);
            }
        }

        [Fact]
        public static async Task Wait_Exception_Test()
        {
            var (waitIndicator, cancel) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            using (waitIndicator)
            {
                Assert.Throws<Exception>(() =>
                {
                    waitIndicator.Wait("", "", false, _ =>
                    {
                        throw new Exception();
                    });
                });

                Assert.False(waitIndicator.IsDisposed);
            }
        }

        [Fact]
        public static async Task WaitForAsyncFunction_Wrapped_Exception_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            using (waitIndicator)
            {
                Assert.Throws<Exception>(() =>
                {
                    waitIndicator.WaitForAsyncFunction("", "", false, async _ =>
                    {
                        await Task.FromException(new Exception());
                    });
                });

                Assert.False(waitIndicator.IsDisposed);
            }
        }

        [Fact]
        public static async Task Wait_Wrapped_Canceled_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            using (waitIndicator)
            {
                waitIndicator.Wait("", "", false, _ =>
                {
                    Task.FromException(new OperationCanceledException());
                });

                Assert.False(waitIndicator.IsDisposed);
            }
        }

        [Fact]
        public static async Task WaitForAsyncFunction_Wrapped_Canceled_Test2Async()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            using (waitIndicator)
            {
                waitIndicator.WaitForAsyncFunction("", "", false, async _ =>
                {
                    await Task.WhenAll(
                        Task.Run(() => throw new OperationCanceledException()),
                        Task.Run(() => throw new OperationCanceledException()));
                });

                Assert.False(waitIndicator.IsDisposed);
            }
        }

        [Fact]
        public static async Task Wait_Wrapped_Canceled_Test3Async()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            using (waitIndicator)
            {
                waitIndicator.Wait("", "", false, _ =>
                {
                    throw new AggregateException(new[] { new OperationCanceledException(), new OperationCanceledException() });
                });

                Assert.False(waitIndicator.IsDisposed);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task Wait_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            waitIndicator.Wait(title, message, isCancelable, _ =>
            {
                wasCalled = true;
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task Wait_Canceled_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            waitIndicator.Wait(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                throw new OperationCanceledException();
            });

            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task WaitWithResult_ArgumentNullException_Test(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                waitIndicator.WaitWithResult(title, message, false, _ =>
                {
                    throw new Exception();
                });
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task WaitWithResult_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                waitIndicator.WaitWithResult("", "", false, null!);
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitWithResult_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test02";
            string message = "Testing02";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            waitIndicator.WaitWithResult(title, message, isCancelable, _ =>
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
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            waitIndicator.WaitWithResult(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                throw new OperationCanceledException();
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncFunction_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test03";
            string message = "Testing03";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            waitIndicator.WaitForAsyncFunction(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                return Task.CompletedTask;
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task WaitForAsyncFunction_ArgumentNullException_Test(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                waitIndicator.WaitForAsyncFunction(title, message, false, _ =>
                {
                    throw new Exception();
                });
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task WaitForAsyncFunction_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                waitIndicator.WaitForAsyncFunction("", "", false, null!);
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncFunction_Canceled_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test03";
            string message = "Testing03";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            waitIndicator.WaitForAsyncFunction(title, message, isCancelable, _ =>
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
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var result = waitIndicator.WaitForAsyncFunctionWithResult(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                return Task.CompletedTask;
            });
            Assert.True(wasCalled);
            Assert.Equal(WaitIndicatorResult.Completed, result);
        }


        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task WaitForAsyncFunctionWithResult_ArgumentNullException_Test(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForAsyncFunctionWithResult(title, message, false, _ =>
                {
                    throw new Exception();
                });
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task WaitForAsyncFunctionWithResult_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForAsyncFunctionWithResult("", "", false, null!);
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncFunctionWithResult_Canceled_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test04";
            string message = "Testing04";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var result = waitIndicator.WaitForAsyncFunctionWithResult(title, message, isCancelable, _ =>
            {
                wasCalled = true;
                throw new OperationCanceledException();
            });
            Assert.True(wasCalled);
            Assert.Equal(WaitIndicatorResult.Canceled, result);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task Wait_Returns_ArgumentNullException_Test(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.Wait(title, message, false, _ =>
                {
                    return 42;
                });
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task WaitForOperationReturns_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.Wait("", "", false, (Func<CancellationToken, int>)null!);
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task Wait_Returns_Test(bool isCancelable)
        {
            string title = "Test05";
            string message = "Testing05";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var result = waitIndicator.Wait(title, message, isCancelable, _ =>
            {
                return 42;
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static async Task Wait_Canceled_Test2(bool isCancelable)
        {
            string title = "Test05";
            string message = "Testing05";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var result = waitIndicator.Wait(title, message, isCancelable, _ =>
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
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task WaitWithResult_Returns_ArgumentNullException_Test(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var (canceled, result) = waitIndicator.WaitWithResult(title, message, false, _ =>
                {
                    return 42;
                });
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task WaitWithResult_ResultReturns_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var (canceled, result) = waitIndicator.WaitWithResult("", "", false, (Func<CancellationToken, int>)null!);
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitWithResult_Returns_Test(bool isCancelable)
        {
            string title = "Test06";
            string message = "Testing06";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var (canceled, result) = waitIndicator.WaitWithResult(title, message, isCancelable, _ =>
            {
                return 42;
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static async Task WaitWithResult_ReturnsCanceled_Test(bool isCancelable)
        {
            string title = "Test06";
            string message = "Testing06";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var (canceled, result) = waitIndicator.WaitWithResult(title, message, isCancelable, _ =>
            {
                if (isCancelable)
                {
                    throw new OperationCanceledException();
                }

                return 42;
            });
            Assert.Equal(0, result);
            Assert.Equal(WaitIndicatorResult.Canceled, canceled);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task WaitForAsyncFunction_Returns_ArgumentNullException_Test(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForAsyncFunction(title, message, false, _ =>
                {
                    return Task.FromResult(42);
                });
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task WaitForAsyncFunction_Returns_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForAsyncFunction("", "", false, (Func<CancellationToken, Task<int>>)null!);
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncFunction_Returns_Test(bool isCancelable)
        {
            string title = "Test07";
            string message = "Testing07";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var result = waitIndicator.WaitForAsyncFunction(title, message, isCancelable, _ =>
            {
                return Task.FromResult(42);
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static async Task WaitForAsyncFunction_Returns_Canceled_Test(bool isCancelable)
        {
            string title = "Test07";
            string message = "Testing07";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            object? result = waitIndicator.WaitForAsyncFunction(title, message, isCancelable, _ =>
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
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task WaitForAsyncFunctionWithResult_Returns_ArgumentNullException_Test(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var (_, result) = waitIndicator.WaitForAsyncFunctionWithResult(title, message, false, _ =>
                {
                    return Task.FromResult(42);
                });
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task WaitForAsyncFunctionWithResult_Returns_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var (_, result) = waitIndicator.WaitForAsyncFunctionWithResult("", "", false, (Func<CancellationToken, Task<int>>)null!);
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncOperationWithResultReturns_Test(bool isCancelable)
        {
            string title = "Test08";
            string message = "Testing08";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var (_, result) = waitIndicator.WaitForAsyncFunctionWithResult(title, message, isCancelable, _ =>
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
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var (canceled, result) = waitIndicator.WaitForAsyncFunctionWithResult(title, message, isCancelable, _ =>
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
        public static async Task WaitForOperation_Cancellation_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (waitIndicator, cancel) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            waitIndicator.Wait(title, message, isCancelable, token =>
            {
                cancel();
                Assert.Equal(isCancelable, token.IsCancellationRequested);
                wasCalled = true;
            });
            Assert.True(wasCalled);
        }

        private static (VisualStudioWaitIndicator, Action cancel) CreateVisualStudioWaitIndicator(string title = "", string message = "", bool isCancelable = false)
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var (threadedWaitDialogFactory, cancel) = IVsThreadedWaitDialogFactoryFactory.Create(title, message, isCancelable);
            var threadedWaitDialogFactoryService = IVsServiceFactory.Create<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>(threadedWaitDialogFactory);
            var unconfiguredProject = UnconfiguredProjectFactory.Create();

            var waitIndicator = new VisualStudioWaitIndicator(unconfiguredProject, threadingService, threadedWaitDialogFactoryService);
            return (waitIndicator, cancel);
        }
    }
}
