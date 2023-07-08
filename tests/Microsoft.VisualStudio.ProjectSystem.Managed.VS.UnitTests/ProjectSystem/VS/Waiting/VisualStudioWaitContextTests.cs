// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiting
{
    public static class VisualStudioWaitContextTests
    {
        [Fact]
        public static void CancellationToken()
        {
            var cancellable = Create("Title", "Message", allowCancel: true);

            Assert.True(cancellable.CancellationToken.CanBeCanceled);

            var nonCancellable = Create("Title", "Message", allowCancel: false);

            Assert.False(nonCancellable.CancellationToken.CanBeCanceled);
        }

        [Fact]
        public static void CreateWrongType_Test()
        {
            Assert.ThrowsAny<Exception>(() => _ = CreateWrongType(string.Empty, string.Empty, false));
        }

        private delegate void CreateInstanceCallback(out IVsThreadedWaitDialog2 ppIVsThreadedWaitDialog);

        private static VisualStudioWaitContext Create(string title, string message, bool allowCancel)
        {
            var threadedWaitDialogFactoryMock = new Mock<IVsThreadedWaitDialogFactory>();
            var threadedWaitDialogMock = new Mock<IVsThreadedWaitDialog3>();
            threadedWaitDialogMock.Setup(m => m.StartWaitDialogWithCallback(
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.Is<string>(s => s == null),
                It.Is<object>(s => s == null),
                It.Is<string>(s => s == null),
                It.IsAny<bool>(),
                It.IsInRange(0, int.MaxValue, Range.Inclusive),
                It.Is<bool>(v => !v),
                It.Is<int>(i => i == 0),
                It.Is<int>(i => i == 0),
                It.IsNotNull<IVsThreadedWaitDialogCallback>()))
                .Callback((string szWaitCaption,
                           string szWaitMessage,
                           string szProgressText,
                           object varStatusBmpAnim,
                           string szStatusBarText,
                           bool fIsCancelable,
                           int iDelayToShowDialog,
                           bool fShowProgress,
                           int iTotalSteps,
                           int iCurrentStep,
                           IVsThreadedWaitDialogCallback pCallback) =>
                {
                    Assert.Equal(title, szWaitCaption);
                    Assert.Equal(message, szWaitMessage);
                    Assert.Equal(allowCancel, fIsCancelable);
                });
            threadedWaitDialogMock.Setup(m => m.EndWaitDialog(out It.Ref<int>.IsAny));
            var threadedWaitDialog = threadedWaitDialogMock.Object;

            threadedWaitDialogFactoryMock
                .Setup(m => m.CreateInstance(out It.Ref<IVsThreadedWaitDialog2>.IsAny))
                .Callback(new CreateInstanceCallback((out IVsThreadedWaitDialog2 ppIVsThreadedWaitDialog) =>
                {
                    ppIVsThreadedWaitDialog = threadedWaitDialog;
                }))
                .Returns(HResult.OK);
            return new VisualStudioWaitContext(threadedWaitDialogFactoryMock.Object, title, message, allowCancel);
        }

        private static VisualStudioWaitContext CreateWrongType(string title, string message, bool allowCancel)
        {
            var threadedWaitDialogFactoryMock = new Mock<IVsThreadedWaitDialogFactory>();
            threadedWaitDialogFactoryMock
                .Setup(m => m.CreateInstance(out It.Ref<IVsThreadedWaitDialog2>.IsAny))
                .Callback(new CreateInstanceCallback((out IVsThreadedWaitDialog2 ppIVsThreadedWaitDialog) =>
                {
                    ppIVsThreadedWaitDialog = null!;
                }))
                .Returns(HResult.OK);
            return new VisualStudioWaitContext(threadedWaitDialogFactoryMock.Object, title, message, allowCancel);
        }
    }
}
