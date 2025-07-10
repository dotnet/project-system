// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

public class ProjectHotReloadBuildManagerTests
{
    [Fact]
    public async Task BuildProjectAsync_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        var mockVsSolutionBuildManager = new Mock<IVsSolutionBuildManager2>();
        var mockVsHierarchy = new Mock<IVsHierarchy>();
        var mockProject = UnconfiguredProjectFactory.Create(hostObject: mockVsHierarchy.Object);
        var mockThreadingService = IProjectThreadingServiceFactory.Create();
        var mockVsService = IVsServiceFactory.Create<SVsSolutionBuildManager, IVsSolutionBuildManager2>(mockVsSolutionBuildManager.Object);

        // Set up the solution build manager to simulate successful build
        mockVsSolutionBuildManager
            .Setup(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny))
            .Callback((IVsUpdateSolutionEvents events, out uint cookie) =>
            {
                cookie = 1;
                // Simulate successful build
                Task.Run(async () =>
                {
                    await Task.Delay(10); // Small delay to simulate build time
                    events.UpdateSolution_Done(fSucceeded: 1, fModified: 0, fCancelCommand: 0);
                });
            })
            .Returns(HResult.OK);

        mockVsSolutionBuildManager
            .Setup(x => x.StartSimpleUpdateProjectConfiguration(
                It.IsAny<IVsHierarchy>(),
                It.IsAny<IVsHierarchy>(),
                It.IsAny<string>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<int>()))
            .Returns(HResult.OK);

        mockVsSolutionBuildManager
            .Setup(x => x.UnadviseUpdateSolutionEvents(It.IsAny<uint>()))
            .Returns(HResult.OK);

        var buildManager = new ProjectHotReloadBuildManager(
            mockProject,
            mockThreadingService,
            mockVsService);

        // Act
        var result = await buildManager.BuildProjectAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        mockVsSolutionBuildManager.Verify(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny), Times.Once);
        mockVsSolutionBuildManager.Verify(x => x.StartSimpleUpdateProjectConfiguration(
            mockVsHierarchy.Object,
            null,
            null,
            (uint)VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD,
            (uint)VSSOLNBUILDQUERYRESULTS.VSSBQR_SAVEBEFOREBUILD_QUERY_YES,
            0), Times.Once);
        mockVsSolutionBuildManager.Verify(x => x.UnadviseUpdateSolutionEvents(It.IsAny<uint>()), Times.Once);
    }

    [Fact]
    public async Task BuildProjectAsync_WhenBuildFails_ReturnsFalse()
    {
        // Arrange
        var mockVsSolutionBuildManager = new Mock<IVsSolutionBuildManager2>();
        var mockVsHierarchy = new Mock<IVsHierarchy>();
        var mockProject = UnconfiguredProjectFactory.Create(hostObject: mockVsHierarchy.Object);
        var mockThreadingService = IProjectThreadingServiceFactory.Create();
        var mockVsService = IVsServiceFactory.Create<SVsSolutionBuildManager, IVsSolutionBuildManager2>(mockVsSolutionBuildManager.Object);

        // Set up the solution build manager to simulate build failure
        mockVsSolutionBuildManager
            .Setup(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny))
            .Callback((IVsUpdateSolutionEvents events, out uint cookie) =>
            {
                cookie = 1;
                // Simulate build failure by calling UpdateSolution_Done with fSucceeded = 0
                Task.Run(async () =>
                {
                    await Task.Delay(50); // Small delay to simulate build time
                    events.UpdateSolution_Done(fSucceeded: 0, fModified: 0, fCancelCommand: 0);
                });
            })
            .Returns(HResult.OK);

        mockVsSolutionBuildManager
            .Setup(x => x.StartSimpleUpdateProjectConfiguration(
                It.IsAny<IVsHierarchy>(),
                It.IsAny<IVsHierarchy>(),
                It.IsAny<string>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<int>()))
            .Returns(HResult.OK);

        mockVsSolutionBuildManager
            .Setup(x => x.UnadviseUpdateSolutionEvents(It.IsAny<uint>()))
            .Returns(HResult.OK);

        var buildManager = new ProjectHotReloadBuildManager(
            mockProject,
            mockThreadingService,
            mockVsService);

        // Act
        var result = await buildManager.BuildProjectAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task BuildProjectAsync_WhenCanceled_ThrowsTaskCanceledException()
    {
        // Arrange
        var mockVsSolutionBuildManager = new Mock<IVsSolutionBuildManager2>();
        var mockVsHierarchy = new Mock<IVsHierarchy>();
        var mockProject = UnconfiguredProjectFactory.Create(hostObject: mockVsHierarchy.Object);
        var mockThreadingService = IProjectThreadingServiceFactory.Create();
        var mockVsService = IVsServiceFactory.Create<SVsSolutionBuildManager, IVsSolutionBuildManager2>(mockVsSolutionBuildManager.Object);

        var buildManager = new ProjectHotReloadBuildManager(
            mockProject,
            mockThreadingService,
            mockVsService);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => buildManager.BuildProjectAsync(cts.Token));
    }

    [Fact]
    public async Task BuildProjectAsync_WhenStartSimpleUpdateProjectConfigurationFails_ThrowsComException()
    {
        // Arrange
        var mockVsSolutionBuildManager = new Mock<IVsSolutionBuildManager2>();
        var mockVsHierarchy = new Mock<IVsHierarchy>();
        var mockProject = UnconfiguredProjectFactory.Create(hostObject: mockVsHierarchy.Object);
        var mockThreadingService = IProjectThreadingServiceFactory.Create();
        var mockVsService = IVsServiceFactory.Create<SVsSolutionBuildManager, IVsSolutionBuildManager2>(mockVsSolutionBuildManager.Object);

        // Set up the solution build manager to return success for registration but fail for build
        mockVsSolutionBuildManager
            .Setup(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny))
            .Returns(HResult.OK);

        mockVsSolutionBuildManager
            .Setup(x => x.StartSimpleUpdateProjectConfiguration(
                It.IsAny<IVsHierarchy>(),
                It.IsAny<IVsHierarchy>(),
                It.IsAny<string>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<int>()))
            .Returns(HResult.Fail);

        mockVsSolutionBuildManager
            .Setup(x => x.UnadviseUpdateSolutionEvents(It.IsAny<uint>()))
            .Returns(HResult.OK);

        var buildManager = new ProjectHotReloadBuildManager(
            mockProject,
            mockThreadingService,
            mockVsService);

        // Act & Assert
        await Assert.ThrowsAsync<System.Runtime.InteropServices.COMException>(() => buildManager.BuildProjectAsync(CancellationToken.None));
    }

    [Fact]
    public async Task BuildProjectAsync_WhenBuildCanceled_ReturnsFalse()
    {
        // Arrange
        var mockVsSolutionBuildManager = new Mock<IVsSolutionBuildManager2>();
        var mockVsHierarchy = new Mock<IVsHierarchy>();
        var mockProject = UnconfiguredProjectFactory.Create(hostObject: mockVsHierarchy.Object);
        var mockThreadingService = IProjectThreadingServiceFactory.Create();
        var mockVsService = IVsServiceFactory.Create<SVsSolutionBuildManager, IVsSolutionBuildManager2>(mockVsSolutionBuildManager.Object);

        // Set up the solution build manager to simulate build cancellation
        mockVsSolutionBuildManager
            .Setup(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny))
            .Callback((IVsUpdateSolutionEvents events, out uint cookie) =>
            {
                cookie = 1;
                // Simulate build cancellation
                Task.Run(async () =>
                {
                    await Task.Delay(50); // Small delay to simulate build time
                    events.UpdateSolution_Cancel();
                });
            })
            .Returns(HResult.OK);

        mockVsSolutionBuildManager
            .Setup(x => x.StartSimpleUpdateProjectConfiguration(
                It.IsAny<IVsHierarchy>(),
                It.IsAny<IVsHierarchy>(),
                It.IsAny<string>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<int>()))
            .Returns(HResult.OK);

        mockVsSolutionBuildManager
            .Setup(x => x.UnadviseUpdateSolutionEvents(It.IsAny<uint>()))
            .Returns(HResult.OK);

        var buildManager = new ProjectHotReloadBuildManager(
            mockProject,
            mockThreadingService,
            mockVsService);

        // Act
        var result = await buildManager.BuildProjectAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task BuildProjectAsync_CallsCorrectVsApis()
    {
        // Arrange
        var mockVsSolutionBuildManager = new Mock<IVsSolutionBuildManager2>();
        var mockVsHierarchy = new Mock<IVsHierarchy>();
        var mockProject = UnconfiguredProjectFactory.Create(hostObject: mockVsHierarchy.Object);
        var mockThreadingService = IProjectThreadingServiceFactory.Create();
        var mockVsService = IVsServiceFactory.Create<SVsSolutionBuildManager, IVsSolutionBuildManager2>(mockVsSolutionBuildManager.Object);

        // Set up successful build
        mockVsSolutionBuildManager
            .Setup(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny))
            .Callback((IVsUpdateSolutionEvents events, out uint cookie) =>
            {
                cookie = 1;
                // Simulate successful build
                Task.Run(async () =>
                {
                    await Task.Delay(10); // Small delay to simulate build time
                    events.UpdateSolution_Done(fSucceeded: 1, fModified: 0, fCancelCommand: 0);
                });
            })
            .Returns(HResult.OK);

        mockVsSolutionBuildManager
            .Setup(x => x.StartSimpleUpdateProjectConfiguration(
                It.IsAny<IVsHierarchy>(),
                It.IsAny<IVsHierarchy>(),
                It.IsAny<string>(),
                It.IsAny<uint>(),
                It.IsAny<uint>(),
                It.IsAny<int>()))
            .Returns(HResult.OK);

        mockVsSolutionBuildManager
            .Setup(x => x.UnadviseUpdateSolutionEvents(It.IsAny<uint>()))
            .Returns(HResult.OK);

        var buildManager = new ProjectHotReloadBuildManager(
            mockProject,
            mockThreadingService,
            mockVsService);

        // Act
        await buildManager.BuildProjectAsync(CancellationToken.None);

        // Assert
        mockVsSolutionBuildManager.Verify(x => x.AdviseUpdateSolutionEvents(It.IsAny<IVsUpdateSolutionEvents>(), out It.Ref<uint>.IsAny), Times.Once);
        mockVsSolutionBuildManager.Verify(x => x.StartSimpleUpdateProjectConfiguration(
            mockVsHierarchy.Object,
            null,
            null,
            (uint)VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD,
            (uint)VSSOLNBUILDQUERYRESULTS.VSSBQR_SAVEBEFOREBUILD_QUERY_YES,
            0), Times.Once);
        mockVsSolutionBuildManager.Verify(x => x.UnadviseUpdateSolutionEvents(It.IsAny<uint>()), Times.Once);
    }
}
