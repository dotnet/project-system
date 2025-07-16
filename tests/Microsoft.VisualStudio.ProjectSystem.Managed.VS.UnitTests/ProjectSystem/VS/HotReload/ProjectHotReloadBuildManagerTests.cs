// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.HotReload;
using Microsoft.VisualStudio.ProjectSystem.VS.Build;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

public class ProjectHotReloadBuildManagerTests
{
    [Fact]
    public async Task BuildProjectAsync_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        var mockVsSolutionBuildManager = new Mock<ISolutionBuildManager>();
        var mockVsHierarchy = new Mock<IVsHierarchy>();
        var mockProject = UnconfiguredProjectFactory.Create(hostObject: mockVsHierarchy.Object);
        var mockThreadingService = IProjectThreadingServiceFactory.Create();
        var mockLogger = new Mock<Lazy<IHotReloadDiagnosticOutputService>>();
        var mockDisposable = new Mock<IAsyncDisposable>();

        mockVsSolutionBuildManager
            .Setup(x => x.BuildProjectAndWaitForCompletionAsync(
                It.IsAny<IVsHierarchy>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var buildManager = new ProjectHotReloadBuildManager(
            mockProject,
            mockVsSolutionBuildManager.Object,
            mockThreadingService,
            mockLogger.Object);

        // Act
        var result = await buildManager.BuildProjectAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        mockVsSolutionBuildManager.Verify(x => x.BuildProjectAndWaitForCompletionAsync(
            mockVsHierarchy.Object,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BuildProjectAsync_WhenBuildFails_ReturnsFalse()
    {
        // Arrange
        var mockVsSolutionBuildManager = new Mock<ISolutionBuildManager>();
        var mockVsHierarchy = new Mock<IVsHierarchy>();
        var mockProject = UnconfiguredProjectFactory.Create(hostObject: mockVsHierarchy.Object);
        var mockThreadingService = IProjectThreadingServiceFactory.Create();
        var mockDisposable = new Mock<IAsyncDisposable>();
        var mockLogger = new Mock<Lazy<IHotReloadDiagnosticOutputService>>();

        // Set up the solution build manager to simulate build failure
        mockVsSolutionBuildManager
            .Setup(x => x.SubscribeSolutionEventsAsync(It.IsAny<IVsUpdateSolutionEvents>()))
            .Callback((IVsUpdateSolutionEvents events) =>
            {
                // Simulate build failure by calling UpdateSolution_Done with fSucceeded = 0
                Task.Run(async () =>
                {
                    await Task.Delay(50); // Small delay to simulate build time
                    events.UpdateSolution_Done(fSucceeded: 0, fModified: 0, fCancelCommand: 0);
                });
            })
            .ReturnsAsync(mockDisposable.Object);

        mockVsSolutionBuildManager
            .Setup(x => x.BuildProjectAndWaitForCompletionAsync(
                It.IsAny<IVsHierarchy>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var buildManager = new ProjectHotReloadBuildManager(
            mockProject,
            mockVsSolutionBuildManager.Object,
            mockThreadingService,
            mockLogger.Object);

        // Act
        var result = await buildManager.BuildProjectAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task BuildProjectAsync_WhenBuildCanceled_ReturnsFalse()
    {
        // Arrange
        var mockVsHierarchy = new Mock<IVsHierarchy>();
        var mockProject = UnconfiguredProjectFactory.Create(hostObject: mockVsHierarchy.Object);
        var mockThreadingService = IProjectThreadingServiceFactory.Create();
        var mockSolutionBuildManager = new Mock<ISolutionBuildManager>();
        var mockDisposable = new Mock<IAsyncDisposable>();
        var mockLogger = new Mock<Lazy<IHotReloadDiagnosticOutputService>>();
        // Set up the solution build manager to simulate build cancellation
        mockSolutionBuildManager
            .Setup(x => x.SubscribeSolutionEventsAsync(It.IsAny<IVsUpdateSolutionEvents>()))
            .Callback((IVsUpdateSolutionEvents events) =>
            {
                // Simulate build cancellation
                Task.Run(async () =>
                {
                    await Task.Delay(50); // Small delay to simulate build time
                    events.UpdateSolution_Cancel();
                });
            })
            .ReturnsAsync(mockDisposable.Object);

        mockSolutionBuildManager
            .Setup(x => x.BuildProjectAndWaitForCompletionAsync(
                It.IsAny<IVsHierarchy>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var buildManager = new ProjectHotReloadBuildManager(
            mockProject,
            mockSolutionBuildManager.Object,
            mockThreadingService,
            mockLogger.Object);

        // Act
        var result = await buildManager.BuildProjectAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
    }
}
