// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Build;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
#pragma warning disable VSSDK005

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

        IVsUpdateSolutionEvents? capturedEventListener = null;
        uint capturedCookie = 1;

        // Setup the VS solution build manager to capture the event listener
        mockVsSolutionBuildManager2
            .Setup(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny))
            .Callback((IVsUpdateSolutionEvents events, out uint cookie) =>
            {
                capturedEventListener = events;
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

        // Simulate build completion event after a short delay
        await Task.Delay(50);
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
        var eventListeners = new List<IVsUpdateSolutionEvents>();
        uint cookieCounter = 1;

        // Setup the VS solution build manager to capture event listeners and track build start times
        mockVsSolutionBuildManager2
            .Setup(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny))
            .Callback((IVsUpdateSolutionEvents events, out uint cookie) =>
            {
                eventListeners.Add(events);
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

        // Wait for both builds to start and register their event listeners
        await Task.Yield();
        await Task.Delay(1000);

        // Complete the first build
        Assert.True(eventListeners.Count == 1);
        eventListeners[0].UpdateSolution_Done(fSucceeded: 1, fModified: 0, fCancelCommand: 0);

        // Wait for the first build to complete and the second to start
        await Task.Delay(100);

        // Complete the second build
        Assert.True(eventListeners.Count == 2);
        eventListeners[1].UpdateSolution_Done(fSucceeded: 1, fModified: 0, fCancelCommand: 0);

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

        IVsUpdateSolutionEvents? capturedEventListener = null;

        mockVsSolutionBuildManager2
            .Setup(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny))
            .Callback((IVsUpdateSolutionEvents events, out uint cookie) =>
            {
                capturedEventListener = events;
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

        // Simulate build failure event
        await Task.Delay(50);
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

        IVsUpdateSolutionEvents? capturedEventListener = null;

        mockVsSolutionBuildManager2
            .Setup(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny))
            .Callback((IVsUpdateSolutionEvents events, out uint cookie) =>
            {
                capturedEventListener = events;
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

        // Simulate build cancellation event
        await Task.Delay(50);
        Assert.NotNull(capturedEventListener);
        capturedEventListener.UpdateSolution_Cancel();

        var result = await buildTask;

        // Assert
        Assert.False(result);
    }
}
#pragma warning restore VSSDK005
