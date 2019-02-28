// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Mocks;

using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiting
{
    public class VisualStudioOperationWaitIndicatorTests
    {
        [Fact]
        public static void WaitForOperation_Exception()
        {
            var (instance, _) = CreateVisualStudioWaitIndicator();
            Assert.Throws<Exception>(() =>
            {
                instance.WaitForAsyncOperation("", "", false, _
                    => throw new Exception());
            });

        }

        [Fact]
        public static void WaitForOperation_Wrapped_Exception()
        {
            var (instance, _) = CreateVisualStudioWaitIndicator();
            Assert.Throws<Exception>(() =>
            {
                instance.WaitForAsyncOperation("", "", false, async _
                    => await Task.FromException(new Exception()));
            });

        }

        [Fact]
        public static void WaitForOperation_Wrapped_Canceled()
        {
            var (instance, _) = CreateVisualStudioWaitIndicator();
            instance.WaitForAsyncOperation("", "", false, async _
                => await Task.FromException(new OperationCanceledException()));
        }


        [Fact]
        public static void WaitForOperation_DoNotReturnTask()
        {
            var (instance, _) = CreateVisualStudioWaitIndicator();
            Assert.Throws<ArgumentException>(() =>
            {
                instance.WaitForOperation("", "", false, async _ =>
                {
                    await Task.FromException(new OperationCanceledException());
                });
            });
        }

        [Fact]
        public static void WaitForOperation_Wrapped_Canceled2()
        {
            var (instance, _) = CreateVisualStudioWaitIndicator();
            instance.WaitForAsyncOperation("", "", false, async _ =>
            {
                await Task.WhenAll(
                    Task.Run(() => throw new OperationCanceledException()),
                    Task.Run(() => throw new OperationCanceledException()));
            });
        }

        [Fact]
        public static void WaitForOperation_Wrapped_Canceled3()
        {
            var (instance, _) = CreateVisualStudioWaitIndicator();
            instance.WaitForOperation("", "", false, _ =>
            {
                throw new AggregateException(new[] { new OperationCanceledException(), new OperationCanceledException() });
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void WaitForOperation(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            instance.WaitForOperation(title, message, isCancelable, _ =>
            {
                wasCalled = true;
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void WaitForOperationCanceled(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
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
        public static void WaitForOperationWithResult(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test02";
            string message = "Testing02";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            instance.WaitForOperationWithResult(title, message, isCancelable, _ =>
            {
                wasCalled = true;
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void WaitForOperationWithResultCanceled(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test02";
            string message = "Testing02";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
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
        public static void WaitForAsyncOperation(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test03";
            string message = "Testing03";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
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
        public static void WaitForAsyncOperationCanceled(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test03";
            string message = "Testing03";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
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
        public static void WaitForAsyncOperationWithResult(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test04";
            string message = "Testing04";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
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
        public static void WaitForAsyncOperationWithResultCanceled(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test04";
            string message = "Testing04";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
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
        public static void WaitForOperationReturns(bool isCancelable)
        {
            string title = "Test05";
            string message = "Testing05";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            var result = instance.WaitForOperation(title, message, isCancelable, _ =>
            {
                return 42;
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static void WaitForOperationReturnsCanceled(bool isCancelable)
        {
            string title = "Test05";
            string message = "Testing05";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
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
        public static void WaitForOperationWithResultReturns(bool isCancelable)
        {
            string title = "Test06";
            string message = "Testing06";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            var (cancelled, result) = instance.WaitForOperationWithResult(title, message, isCancelable, _ =>
            {
                return 42;
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static void WaitForOperationWithResultReturnsCanceled(bool isCancelable)
        {
            string title = "Test06";
            string message = "Testing06";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
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
        public static void WaitForAsyncOperationReturns(bool isCancelable)
        {
            string title = "Test07";
            string message = "Testing07";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            var result = instance.WaitForAsyncOperation(title, message, isCancelable, _ =>
            {
                return Task.FromResult(42);
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static void WaitForAsyncOperationReturnsCanceled(bool isCancelable)
        {
            string title = "Test07";
            string message = "Testing07";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
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
        public static void WaitForAsyncOperationWithResultReturns(bool isCancelable)
        {
            string title = "Test08";
            string message = "Testing08";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            var (_, result) = instance.WaitForAsyncOperationWithResult(title, message, isCancelable, _ =>
            {
                return Task.FromResult(42);
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static void WaitForAsyncOperationWithResultReturnsCanceled(bool isCancelable)
        {
            string title = "Test08";
            string message = "Testing08";
            var (instance, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
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
        public static void WaitForOperation_Cancellation(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (instance, cancel) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            instance.WaitForOperation(title, message, isCancelable, token =>
            {
                cancel();
                Assert.Equal(isCancelable, token.IsCancellationRequested);
                wasCalled = true;
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static void WaitForOperation_ArgumentNullException(string title, string message)
        {
            bool isCancelable = false;
            var (waitIndicator, cancel) = CreateVisualStudioWaitIndicator(title, message, isCancelable);

            Assert.Throws<ArgumentNullException>(() =>
            {
                waitIndicator.WaitForAsyncOperation(title, message, isCancelable, _ =>
                {
                    throw new Exception();
                });
            });
        }

        [Fact]
        public static void WaitForOperation_ArgumentNullException_Delegate()
        {
            var (waitIndicator, cancel) = CreateVisualStudioWaitIndicator();

            Assert.Throws<ArgumentNullException>(() =>
            {
                waitIndicator.WaitForAsyncOperation("", "", false, null);
            });
        }

        [Fact]
        public static void WaitForOperation_Wrapped_Canceled2Async()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            waitIndicator.WaitForAsyncOperation("", "", false, async _ =>
            {
                await Task.WhenAll(
                    Task.Run(() => throw new OperationCanceledException()),
                    Task.Run(() => throw new OperationCanceledException()));
            });
        }

        [Fact]
        public static void WaitForOperation_Wrapped_Canceled3Async()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            waitIndicator.WaitForOperation("", "", false, _ =>
            {
                throw new AggregateException(new[] { new OperationCanceledException(), new OperationCanceledException() });
            });
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static void WaitForOperationWithResult_ArgumentNullException(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);

            Assert.Throws<ArgumentNullException>(() =>
            {
                waitIndicator.WaitForOperationWithResult(title, message, false, _ =>
                {
                    throw new Exception();
                });
            });
        }

        [Fact]
        public static void WaitForOperationWithResult_ArgumentNullException_Delegate()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();

            Assert.Throws<ArgumentNullException>(() =>
            {
                waitIndicator.WaitForOperationWithResult("", "", false, null);
            });
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static void WaitForAsyncOperation_ArgumentNullException(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);

            Assert.Throws<ArgumentNullException>(() =>
            {
                waitIndicator.WaitForAsyncOperation(title, message, false, _ =>
                {
                    throw new Exception();
                });
            });
        }

        [Fact]
        public static void WaitForAsyncOperation_ArgumentNullException_Delegate()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();

            Assert.Throws<ArgumentNullException>(() =>
            {
                waitIndicator.WaitForAsyncOperation("", "", false, null);
            });
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static void WaitForAsyncOperationWithResult_ArgumentNullException(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForAsyncOperationWithResult(title, message, false, _ =>
                {
                    throw new Exception();
                });
            });
        }

        [Fact]
        public static void WaitForAsyncOperationWithResult_ArgumentNullException_Delegate()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForAsyncOperationWithResult("", "", false, null);
            });
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static void WaitForOperationReturns_ArgumentNullException(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForOperation(title, message, false, _ =>
                {
                    return 42;
                });
            });
        }

        [Fact]
        public static void WaitForOperationReturns_ArgumentNullException_Delegate()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForOperation("", "", false, (Func<CancellationToken, int>)null);
            });
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static void WaitForOperationWithResultReturns_ArgumentNullException(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);

            Assert.Throws<ArgumentNullException>(() =>
            {
                var (canceled, result) = waitIndicator.WaitForOperationWithResult(title, message, false, _ =>
                {
                    return 42;
                });
            });
        }

        [Fact]
        public static void WaitForOperationWithResultReturns_ArgumentNullException_Delegate()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var (canceled, result) = waitIndicator.WaitForOperationWithResult("", "", false, (Func<CancellationToken, int>)null);
            });
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static void WaitForAsyncOperationReturns_ArgumentNullException(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForAsyncOperation(title, message, false, _ =>
                {
                    return Task.FromResult(42);
                });
            });
        }

        [Fact]
        public static void WaitForAsyncOperationReturns_ArgumentNullException_Delegate()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForAsyncOperation("", "", false, (Func<CancellationToken, Task<int>>)null);
            });
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static void WaitForAsyncOperationWithResultReturns_ArgumentNullException(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            Assert.Throws<ArgumentNullException>(() =>
            {
                var (_, result) = waitIndicator.WaitForAsyncOperationWithResult(title, message, false, _ =>
                {
                    return Task.FromResult(42);
                });
            });
        }

        [Fact]
        public static void WaitForAsyncOperationWithResultReturns_ArgumentNullException_Delegate()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            Assert.Throws<ArgumentNullException>(() =>
            {
                var (_, result) = waitIndicator.WaitForAsyncOperationWithResult("", "", false, (Func<CancellationToken, Task<int>>)null);
            });
        }

        private static (VisualStudioOperationWaitIndicator, Action cancel) CreateVisualStudioWaitIndicator(string title = "", string message = "", bool isCancelable = false)
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var threadedWaitDialogFactoryServiceMock = new Mock<IVsUIService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>>();
            var (threadedWaitDialogFactory, cancel) = IVsThreadedWaitDialogFactoryFactory.Create(title, message, isCancelable);
            threadedWaitDialogFactoryServiceMock.Setup(m => m.Value).Returns(threadedWaitDialogFactory);

            var waitIndicator = new VisualStudioOperationWaitIndicator(threadedWaitDialogFactoryServiceMock.Object, threadingService.JoinableTaskFactory.Context);
            return (waitIndicator, cancel);
        }
    }
}
