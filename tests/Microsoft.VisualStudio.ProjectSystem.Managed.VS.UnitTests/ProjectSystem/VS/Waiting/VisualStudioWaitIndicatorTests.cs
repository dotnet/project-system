// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable VSSDK005 // Avoid instantiating JoinableTaskContext

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiting
{
    public static class VisualStudioWaitIndicatorTests
    {
        [Fact]
        public static void Run_WhenAsyncMethodThrows_Throws()
        {
            var (instance, _) = CreateInstance();
            Assert.Throws<Exception>(() =>
            {
                instance.Run<string>("", "", false, _
                    => throw new Exception());
            });
        }

        [Fact]
        public static void Run_WhenAsyncMethodThrowsWrapped_Throws()
        {
            var (instance, _) = CreateInstance();
            Assert.Throws<Exception>(() =>
            {
                instance.Run("", "", false, async _
                    => await Task.FromException<string>(new Exception()));
            });
        }

        [Fact]
        public static void Run_WhenAsyncMethodThrowsOperationCanceled_SetsIsCancelledToTrue()
        {
            var (instance, _) = CreateInstance();

            var result = instance.Run("", "", false, async _
                    => await Task.FromException<string>(new OperationCanceledException()));

            Assert.True(result.IsCancelled);
        }

        [Fact]
        public static void Run_WhenAsyncMethodThrowsAggregrateContainedOperationCanceled_SetsIsCancelledToTrue()
        {
            var (instance, _) = CreateInstance();

            var result = instance.Run("", "", false, async _ =>
            {
                await Task.WhenAll(
                    Task.Run(() => throw new OperationCanceledException()),
                    Task.Run(() => throw new OperationCanceledException()));

                return "";
            });

            Assert.True(result.IsCancelled);
        }

        [Fact]
        public static void Run_WhenUserCancels_CancellationTokenIsCancelled()
        {
            var (instance, userCancel) = CreateInstance(isCancelable: true);

            CancellationToken? result = default;
            instance.Run("", "", true, context =>
            {
                userCancel();

                result = context.CancellationToken;

                return TaskResult.EmptyString;
            });

            Assert.NotNull(result);
            Assert.True(result.Value.IsCancellationRequested);
        }

        [Fact]
        public static void Run_ReturnsResultOfAsyncMethod()
        {
            var (instance, _) = CreateInstance();

            var result = instance.Run("", "", false, _ =>
            {
                return Task.FromResult("Hello");
            });

            Assert.Equal("Hello", result.Result);
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
