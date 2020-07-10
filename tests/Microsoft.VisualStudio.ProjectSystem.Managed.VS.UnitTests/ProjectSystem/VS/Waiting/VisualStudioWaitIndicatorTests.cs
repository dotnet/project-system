// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable VSSDK005 // Avoid instantiating JoinableTaskContext

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiting
{
    public class VisualStudioWaitIndicatorTests
    {
        [Fact]
        public static void Wait_Exception_Test()
        {
            var (instance, _) = CreateInstance();
            Assert.Throws<Exception>(() =>
            {
                instance.Wait("", "", false, _
                    => throw new Exception());
            });
        }

        [Fact]
        public static void WaitForAsyncFunction_Wrapped_Exception_Test()
        {
            var (instance, _) = CreateInstance();
            Assert.Throws<Exception>(() =>
            {
                instance.WaitForAsyncFunction("", "", false, async _
                    => await Task.FromException(new Exception()));
            });
        }

        [Fact]
        public static void WaitForAsyncFunction_Wrapped_Canceled_Test()
        {
            var (instance, _) = CreateInstance();
            instance.WaitForAsyncFunction("", "", false, async _
                => await Task.FromException(new OperationCanceledException()));
        }


        [Fact]
        public static void Wait_DoNotReturnTask_Test()
        {
            var (instance, _) = CreateInstance();
            Assert.Throws<ArgumentException>(() =>
            {
                instance.Wait("", "", false, async _ =>
                {
                    await Task.FromException(new OperationCanceledException());
                });
            });
        }

        [Fact]
        public static void WaitForAsyncFunction_Wrapped_Canceled_Test2()
        {
            var (instance, _) = CreateInstance();
            instance.WaitForAsyncFunction("", "", false, async _ =>
            {
                await Task.WhenAll(
                    Task.Run(() => throw new OperationCanceledException()),
                    Task.Run(() => throw new OperationCanceledException()));
            });

        }

        [Fact]
        public static void WaitForAsyncFunction_Wrapped_Canceled_Test3()
        {
            var (instance, _) = CreateInstance();
            instance.WaitForAsyncFunction("", "", false, _ =>
            {
                throw new AggregateException(new[] { new OperationCanceledException(), new OperationCanceledException() });
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void WaitForAsyncFunction_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (instance, _) = CreateInstance(title, message, isCancelable);
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
        public static void Wait_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (instance, _) = CreateInstance(title, message, isCancelable);
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
        public static void WaitWithResult_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test02";
            string message = "Testing02";
            var (instance, _) = CreateInstance(title, message, isCancelable);
            instance.WaitWithResult(title, message, isCancelable, _ =>
            {
                wasCalled = true;
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void WaitWithResult_Canceled_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test02";
            string message = "Testing02";
            var (instance, _) = CreateInstance(title, message, isCancelable);
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
        public static void WaitForAsyncFunction_Test2(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test03";
            string message = "Testing03";
            var (instance, _) = CreateInstance(title, message, isCancelable);
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
        public static void WaitForAsyncFunction_Canceled_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test03";
            string message = "Testing03";
            var (instance, _) = CreateInstance(title, message, isCancelable);
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
        public static void WaitForAsyncFunctionWithResult_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test04";
            string message = "Testing04";
            var (instance, _) = CreateInstance(title, message, isCancelable);
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
        public static void WaitForAsyncFunctionWithResult_Canceled_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test04";
            string message = "Testing04";
            var (instance, _) = CreateInstance(title, message, isCancelable);
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
        public static void Wait_Returns_Test(bool isCancelable)
        {
            string title = "Test05";
            string message = "Testing05";
            var (instance, _) = CreateInstance(title, message, isCancelable);
            var result = instance.Wait(title, message, isCancelable, _ =>
            {
                return 42;
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static void WaitReturns_Canceled_Test(bool isCancelable)
        {
            string title = "Test05";
            string message = "Testing05";
            var (instance, _) = CreateInstance(title, message, isCancelable);
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
        public static void WaitWithResult_Returns_Test(bool isCancelable)
        {
            string title = "Test06";
            string message = "Testing06";
            var (instance, _) = CreateInstance(title, message, isCancelable);
            var (cancelled, result) = instance.WaitWithResult(title, message, isCancelable, _ =>
            {
                return 42;
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static void WaitWithResult_Canceled_Test2(bool isCancelable)
        {
            string title = "Test06";
            string message = "Testing06";
            var (instance, _) = CreateInstance(title, message, isCancelable);
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
        public static void WaitForAsyncOperation_Returns_Test(bool isCancelable)
        {
            string title = "Test07";
            string message = "Testing07";
            var (instance, _) = CreateInstance(title, message, isCancelable);
            var result = instance.WaitForAsyncFunction(title, message, isCancelable, _ =>
            {
                return Task.FromResult(42);
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static void WaitForAsyncFunctionReturns_Canceled_Test(bool isCancelable)
        {
            string title = "Test07";
            string message = "Testing07";
            var (instance, _) = CreateInstance(title, message, isCancelable);
            object? result = instance.WaitForAsyncFunction(title, message, isCancelable, _ =>
            {
                if (isCancelable)
                {
                    throw new OperationCanceledException();
                }

                return Task.FromResult((object?)null);
            });
            Assert.Null(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void WaitForAsyncFunctionWithResult_Returns_Test(bool isCancelable)
        {
            string title = "Test08";
            string message = "Testing08";
            var (instance, _) = CreateInstance(title, message, isCancelable);
            var (_, result) = instance.WaitForAsyncFunctionWithResult(title, message, isCancelable, _ =>
            {
                return Task.FromResult(42);
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static void WaitForAsyncFunctionWithResult_Returns_Canceled_Test(bool isCancelable)
        {
            string title = "Test08";
            string message = "Testing08";
            var (instance, _) = CreateInstance(title, message, isCancelable);
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
        public static void Wait_Cancellation_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (instance, cancel) = CreateInstance(title, message, isCancelable);
            instance.Wait(title, message, isCancelable, token =>
            {
                cancel();
                Assert.Equal(isCancelable, token.IsCancellationRequested);
                wasCalled = true;
            });
            Assert.True(wasCalled);
        }

        private static (VisualStudioWaitIndicator, Action cancel) CreateInstance(string title = "", string message = "", bool isCancelable = false)
        {
            var joinableTaskContext = new JoinableTaskContext();
            var (threadedWaitDialogFactory, cancel) = IVsThreadedWaitDialogFactoryFactory.Create(title, message, isCancelable);
            var threadedWaitDialogFactoryService = IVsUIServiceFactory.Create<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>(threadedWaitDialogFactory);

            var instance = new VisualStudioWaitIndicator(joinableTaskContext, threadedWaitDialogFactoryService);
            return (instance, cancel);
        }
    }
}
