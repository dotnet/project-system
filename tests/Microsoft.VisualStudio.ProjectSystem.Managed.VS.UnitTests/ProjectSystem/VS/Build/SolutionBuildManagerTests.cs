// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
#pragma warning disable VSSDK005  // Use the JoinableTaskContext singleton

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build;
public class SolutionBuildManagerTests
{
    [Fact]
    public async Task BuildProjectAndWaitForCompletionAsync_WaitsForSolutionBuildCompleteEvent()
    {
        // Arrange
        var mockVsSolutionBuildManager2 = new Mock<IVsSolutionBuildManager2>();
        mockVsSolutionBuildManager2.As<IVsSolutionBuildManager3>();
        var mockVsService = IVsServiceFactory.Create<SVsSolutionBuildManager, IVsSolutionBuildManager2>(mockVsSolutionBuildManager2.Object);
        var mockVsHierarchy = new Mock<IVsHierarchy>();
        var joinableTaskContext = JoinableTaskContext.CreateNoOpContext();

        var tcs = new TaskCompletionSource<IVsUpdateSolutionEvents>();
        uint capturedCookie = 1;

        // Setup the VS solution build manager to capture the event listener
        mockVsSolutionBuildManager2
            .Setup(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny))
            .Callback((IVsUpdateSolutionEvents events, out uint cookie) =>
            {
                tcs.SetResult(events);
                cookie = capturedCookie;
            })
            .Returns(HResult.OK);

        mockVsSolutionBuildManager2
            .Setup(x => x.StartSimpleUpdateProjectConfiguration(
                It.IsAny<IVsHierarchy>(),
                It.IsAny<IVsHierarchy>(),
                It.IsAny<string>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<int>()))
            .Returns(HResult.OK);

        mockVsSolutionBuildManager2
            .Setup(x => x.UnadviseUpdateSolutionEvents(It.IsAny<uint>()))
            .Returns(HResult.OK);

        var buildManager = new SolutionBuildManager(mockVsService, joinableTaskContext);
        // Act
        var buildTask = buildManager.BuildProjectAndWaitForCompletionAsync(mockVsHierarchy.Object);

        // Wait for the build to start and the event listener to be captured
        var capturedEventListener = await tcs.Task;
        Assert.NotNull(capturedEventListener);
        capturedEventListener.UpdateSolution_Done(fSucceeded: 1, fModified: 0, fCancelCommand: 0);
        var result = await buildTask;

        // Assert
        Assert.True(result);
        mockVsSolutionBuildManager2.Verify(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny), Times.Once);
        mockVsSolutionBuildManager2.Verify(x => x.StartSimpleUpdateProjectConfiguration(
            mockVsHierarchy.Object,
            null,
            null,
            (uint)VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD,
            (uint)VSSOLNBUILDQUERYRESULTS.VSSBQR_SAVEBEFOREBUILD_QUERY_YES,
            0), Times.Once);
        mockVsSolutionBuildManager2.Verify(x => x.UnadviseUpdateSolutionEvents(capturedCookie), Times.Once);
    }

    [Fact]
    public async Task BuildProjectAndWaitForCompletionAsync_BuildsSequentiallyWhenCalledInParallel()
    {
        // Arrange
        var mockVsSolutionBuildManager2 = new Mock<IVsSolutionBuildManager2>();
        mockVsSolutionBuildManager2.As<IVsSolutionBuildManager3>();
        var mockVsService = IVsServiceFactory.Create<SVsSolutionBuildManager, IVsSolutionBuildManager2>(mockVsSolutionBuildManager2.Object);
        var mockVsHierarchy1 = new Mock<IVsHierarchy>();
        var mockVsHierarchy2 = new Mock<IVsHierarchy>();
        var joinableTaskContext = JoinableTaskContext.CreateNoOpContext();

        var buildStartTimes = new List<DateTime>();
        var tcsForFirstListener = new TaskCompletionSource<IVsUpdateSolutionEvents>();
        var tcsForSecondListener = new TaskCompletionSource<IVsUpdateSolutionEvents>();
        var eventListeners = new List<IVsUpdateSolutionEvents>();
        uint cookieCounter = 1;

        // Setup the VS solution build manager to capture event listeners and track build start times
        mockVsSolutionBuildManager2
            .Setup(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny))
            .Callback((IVsUpdateSolutionEvents events, out uint cookie) =>
            {
                eventListeners.Add(events);
                if (eventListeners.Count == 1)
                    tcsForFirstListener.SetResult(events);
                else if (eventListeners.Count == 2)
                    tcsForSecondListener.SetResult(events);
                cookie = cookieCounter++;
            })
            .Returns(HResult.OK);

        mockVsSolutionBuildManager2
            .Setup(x => x.StartSimpleUpdateProjectConfiguration(
                It.IsAny<IVsHierarchy>(),
                It.IsAny<IVsHierarchy>(),
                It.IsAny<string>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<int>()))
            .Callback(() =>
            {
                buildStartTimes.Add(DateTime.UtcNow);
            })
            .Returns(HResult.OK);

        mockVsSolutionBuildManager2
            .Setup(x => x.UnadviseUpdateSolutionEvents(It.IsAny<uint>()))
            .Returns(HResult.OK);

        var buildManager = new SolutionBuildManager(mockVsService, joinableTaskContext);

        // Act - Start two builds in parallel
        var buildTask1 = buildManager.BuildProjectAndWaitForCompletionAsync(mockVsHierarchy1.Object);
        var buildTask2 = buildManager.BuildProjectAndWaitForCompletionAsync(mockVsHierarchy2.Object);

        // Wait for the first build to start and register its event listener
        var firstEventListener = await tcsForFirstListener.Task;
        Assert.NotNull(firstEventListener);

        // Complete the first build
        firstEventListener.UpdateSolution_Done(fSucceeded: 1, fModified: 0, fCancelCommand: 0);

        // Wait for the second build to start and register its event listener
        var secondEventListener = await tcsForSecondListener.Task;
        Assert.NotNull(secondEventListener);

        // Complete the second build
        secondEventListener.UpdateSolution_Done(fSucceeded: 1, fModified: 0, fCancelCommand: 0);

        var result1 = await buildTask1;
        var result2 = await buildTask2;
        // Assert
        Assert.True(result1);
        Assert.True(result2);

        // Verify that builds were started sequentially (second build started after first build started)
        Assert.Equal(2, buildStartTimes.Count);
        Assert.True(buildStartTimes[1] > buildStartTimes[0], "Second build should start after the first build");

        // Verify that StartSimpleUpdateProjectConfiguration was called twice (once for each build)
        mockVsSolutionBuildManager2.Verify(x => x.StartSimpleUpdateProjectConfiguration(
            It.IsAny<IVsHierarchy>(),
            It.IsAny<IVsHierarchy>(),
            It.IsAny<string>(),
            It.IsAny<uint>(),
            It.IsAny<uint>(),
            It.IsAny<int>()), Times.Exactly(2));

        // Verify that event subscription and unsubscription happened for both builds
        mockVsSolutionBuildManager2.Verify(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny), Times.Exactly(2));
        mockVsSolutionBuildManager2.Verify(x => x.UnadviseUpdateSolutionEvents(It.IsAny<uint>()), Times.Exactly(2));
    }

    [Fact]
    public async Task BuildProjectAndWaitForCompletionAsync_WhenBuildFails_ReturnsFalse()
    {
        // Arrange
        var mockVsSolutionBuildManager2 = new Mock<IVsSolutionBuildManager2>();
        mockVsSolutionBuildManager2.As<IVsSolutionBuildManager3>();
        var mockVsService = IVsServiceFactory.Create<SVsSolutionBuildManager, IVsSolutionBuildManager2>(mockVsSolutionBuildManager2.Object);
        var mockVsHierarchy = new Mock<IVsHierarchy>();
        var joinableTaskContext = JoinableTaskContext.CreateNoOpContext();

        var tcs = new TaskCompletionSource<IVsUpdateSolutionEvents>();

        mockVsSolutionBuildManager2
            .Setup(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny))
            .Callback((IVsUpdateSolutionEvents events, out uint cookie) =>
            {
                tcs.SetResult(events);
                cookie = 1;
            })
            .Returns(HResult.OK);

        mockVsSolutionBuildManager2
            .Setup(x => x.StartSimpleUpdateProjectConfiguration(
                It.IsAny<IVsHierarchy>(),
                It.IsAny<IVsHierarchy>(),
                It.IsAny<string>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<int>()))
            .Returns(HResult.OK);

        mockVsSolutionBuildManager2
            .Setup(x => x.UnadviseUpdateSolutionEvents(It.IsAny<uint>()))
            .Returns(HResult.OK);

        var buildManager = new SolutionBuildManager(mockVsService, joinableTaskContext);
        // Act
        var buildTask = buildManager.BuildProjectAndWaitForCompletionAsync(mockVsHierarchy.Object);

        // Wait for the build to start and the event listener to be captured
        var capturedEventListener = await tcs.Task;
        Assert.NotNull(capturedEventListener);
        capturedEventListener.UpdateSolution_Done(fSucceeded: 0, fModified: 0, fCancelCommand: 0);

        var result = await buildTask;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task BuildProjectAndWaitForCompletionAsync_WhenBuildCanceled_ReturnsFalse()
    {
        using var _ = SynchronizationContextUtil.Suppress();

        // Arrange
        var mockVsSolutionBuildManager2 = new Mock<IVsSolutionBuildManager2>();
        mockVsSolutionBuildManager2.As<IVsSolutionBuildManager3>();
        var mockVsService = IVsServiceFactory.Create<SVsSolutionBuildManager, IVsSolutionBuildManager2>(mockVsSolutionBuildManager2.Object);
        var mockVsHierarchy = new Mock<IVsHierarchy>();
        var joinableTaskContext = new JoinableTaskContext();

        var tcs = new TaskCompletionSource<IVsUpdateSolutionEvents>();

        mockVsSolutionBuildManager2
            .Setup(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny))
            .Callback((IVsUpdateSolutionEvents events, out uint cookie) =>
            {
                tcs.SetResult(events);
                cookie = 1;
            })
            .Returns(HResult.OK);

        mockVsSolutionBuildManager2
            .Setup(x => x.StartSimpleUpdateProjectConfiguration(
                It.IsAny<IVsHierarchy>(),
                It.IsAny<IVsHierarchy>(),
                It.IsAny<string>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<int>()))
            .Returns(HResult.OK);

        mockVsSolutionBuildManager2
            .Setup(x => x.UnadviseUpdateSolutionEvents(It.IsAny<uint>()))
            .Returns(HResult.OK);

        var buildManager = new SolutionBuildManager(mockVsService, joinableTaskContext);

        // Act
        var buildTask = buildManager.BuildProjectAndWaitForCompletionAsync(mockVsHierarchy.Object);

        // Wait for the build to start and the event listener to be captured
        var capturedEventListener = await tcs.Task;
        Assert.NotNull(capturedEventListener);
        capturedEventListener.UpdateSolution_Cancel();

        var result = await buildTask;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task BuildProjectAndWaitForCompletionAsync_WhenUpdateSolutionBeginCalledAfterWaiting_CompletesCorrectly()
    {
        using var _ = SynchronizationContextUtil.Suppress();

        // Arrange
        var mockVsSolutionBuildManager2 = new Mock<IVsSolutionBuildManager2>();
        mockVsSolutionBuildManager2.As<IVsSolutionBuildManager3>();
        var mockVsService = IVsServiceFactory.Create<SVsSolutionBuildManager, IVsSolutionBuildManager2>(mockVsSolutionBuildManager2.Object);
        var mockVsHierarchy = new Mock<IVsHierarchy>();
        var joinableTaskContext = new JoinableTaskContext();
        var tcs = new TaskCompletionSource<IVsUpdateSolutionEvents>();

        mockVsSolutionBuildManager2
            .Setup(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny))
            .Callback((IVsUpdateSolutionEvents events, out uint cookie) =>
            {
                tcs.SetResult(events);
                cookie = 1;
            })
            .Returns(HResult.OK);

        mockVsSolutionBuildManager2
            .Setup(x => x.StartSimpleUpdateProjectConfiguration(
                It.IsAny<IVsHierarchy>(),
                It.IsAny<IVsHierarchy>(),
                It.IsAny<string>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<int>()))
            .Returns(HResult.OK);

        mockVsSolutionBuildManager2
            .Setup(x => x.UnadviseUpdateSolutionEvents(It.IsAny<uint>()))
            .Returns(HResult.OK);

        var buildManager = new SolutionBuildManager(mockVsService, joinableTaskContext);

        // Act
        var buildTask = buildManager.BuildProjectAndWaitForCompletionAsync(mockVsHierarchy.Object);

        // Wait for the build to start and the event listener to be captured
        var capturedEventListener = await tcs.Task;
        Assert.NotNull(capturedEventListener);

        // Simulate the problematic sequence:
        // 1. UpdateSolution_Begin is called, in real world, this could happen when user starts another build
        // 2. UpdateSolution_Done is called (which should complete the build)
        int pfCancelUpdate = 0;
        capturedEventListener.UpdateSolution_Begin(ref pfCancelUpdate);
        capturedEventListener.UpdateSolution_Done(fSucceeded: 1, fModified: 0, fCancelCommand: 0);

        // The build should complete successfully despite UpdateSolution_Begin being called
        var result = await buildTask;

        // Assert
        Assert.True(result);
    }
}
#pragma warning restore VSSDK005
