// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.VS.Setup;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

public class ProjectRetargetHandlerTests
{
    private const string GlobalJsonWithSdk = """
        {
          "sdk": {
            "version": "8.0.100"
          }
        }
        """;

    [Fact]
    public async Task CheckForRetargetAsync_WhenNoValidOptions_ReturnsNull()
    {
        var handler = CreateInstance();

        var result = await handler.CheckForRetargetAsync(RetargetCheckOptions.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForRetargetAsync_WhenRetargetingServiceIsNull_ReturnsNull()
    {
        var handler = CreateInstance(trackProjectRetargeting: null);

        var result = await handler.CheckForRetargetAsync(RetargetCheckOptions.ProjectLoad);

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForRetargetAsync_WhenNoGlobalJson_ReturnsNull()
    {
        var fileSystem = new IFileSystemMock();
        var solution = CreateSolutionWithDirectory(@"C:\Solution");

        var handler = CreateInstance(fileSystem: fileSystem, solution: solution);

        var result = await handler.CheckForRetargetAsync(RetargetCheckOptions.ProjectLoad);

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForRetargetAsync_WhenGlobalJsonHasNoSdkVersion_ReturnsNull()
    {
        var fileSystem = new IFileSystemMock();
        var solution = CreateSolutionWithDirectory(@"C:\Solution");

        // Create global.json without sdk.version
        string globalJsonPath = @"C:\Solution\global.json";
        await fileSystem.WriteAllTextAsync(globalJsonPath, "{}");

        var handler = CreateInstance(fileSystem: fileSystem, solution: solution);

        var result = await handler.CheckForRetargetAsync(RetargetCheckOptions.ProjectLoad);

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForRetargetAsync_WhenNoRetargetVersionAvailable_ReturnsNull()
    {
        var fileSystem = new IFileSystemMock();
        var solution = CreateSolutionWithDirectory(@"C:\Solution");

        // Create global.json with sdk version
        string globalJsonPath = @"C:\Solution\global.json";
        await fileSystem.WriteAllTextAsync(globalJsonPath, GlobalJsonWithSdk);

        var releasesProvider = Mock.Of<IDotNetReleasesProvider>(
            p => p.GetSupportedOrLatestSdkVersionAsync("8.0.100", true, default) == Task.FromResult<string?>(null));

        var handler = CreateInstance(
            fileSystem: fileSystem,
            solution: solution,
            releasesProvider: releasesProvider);

        var result = await handler.CheckForRetargetAsync(RetargetCheckOptions.ProjectLoad);

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForRetargetAsync_WhenRetargetVersionSameAsCurrent_ReturnsNull()
    {
        var fileSystem = new IFileSystemMock();
        var solution = CreateSolutionWithDirectory(@"C:\Solution");

        string globalJsonPath = @"C:\Solution\global.json";
        await fileSystem.WriteAllTextAsync(globalJsonPath, GlobalJsonWithSdk);

        // Releases provider returns same version
        var releasesProvider = Mock.Of<IDotNetReleasesProvider>(
            p => p.GetSupportedOrLatestSdkVersionAsync("8.0.100", true, default) == Task.FromResult<string?>("8.0.100"));

        var handler = CreateInstance(
            fileSystem: fileSystem,
            solution: solution,
            releasesProvider: releasesProvider);

        var result = await handler.CheckForRetargetAsync(RetargetCheckOptions.ProjectLoad);

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForRetargetAsync_WhenRetargetVersionIsInstalled_ReturnsNull()
    {
        var fileSystem = new IFileSystemMock();
        var solution = CreateSolutionWithDirectory(@"C:\Solution");

        string globalJsonPath = @"C:\Solution\global.json";
        await fileSystem.WriteAllTextAsync(globalJsonPath, GlobalJsonWithSdk);

        var releasesProvider = Mock.Of<IDotNetReleasesProvider>(
            p => p.GetSupportedOrLatestSdkVersionAsync("8.0.100", true, default) == Task.FromResult<string?>("8.0.200"));

        // SDK is already installed
        var dotnetEnvironment = Mock.Of<IDotNetEnvironment>(
            s => s.IsSdkInstalledAsync("8.0.200") == Task.FromResult(true));

        var handler = CreateInstance(
            fileSystem: fileSystem,
            solution: solution,
            releasesProvider: releasesProvider,
            dotnetEnvironment: dotnetEnvironment);

        var result = await handler.CheckForRetargetAsync(RetargetCheckOptions.ProjectLoad);

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForRetargetAsync_WhenRetargetVersionNotInstalled_ReturnsTargetChange()
    {
        var fileSystem = new IFileSystemMock();
        var solution = CreateSolutionWithDirectory(@"C:\Solution");

        string globalJsonPath = @"C:\Solution\global.json";
        await fileSystem.WriteAllTextAsync(globalJsonPath, GlobalJsonWithSdk);

        var releasesProvider = Mock.Of<IDotNetReleasesProvider>(
            p => p.GetSupportedOrLatestSdkVersionAsync("8.0.100", true, default) == Task.FromResult<string?>("8.0.200"));

        // SDK is NOT installed
        var dotnetEnvironment = Mock.Of<IDotNetEnvironment>(
            s => s.IsSdkInstalledAsync("8.0.200") == Task.FromResult(false));

        var retargetingService = new Mock<IVsTrackProjectRetargeting2>();
        retargetingService.Setup(r => r.RegisterProjectTarget(It.IsAny<IVsProjectTargetDescription>()))
            .Returns(HResult.OK);

        var handler = CreateInstance(
            fileSystem: fileSystem,
            solution: solution,
            releasesProvider: releasesProvider,
            dotnetEnvironment: dotnetEnvironment,
            trackProjectRetargeting: retargetingService.Object);

        var result = await handler.CheckForRetargetAsync(RetargetCheckOptions.ProjectLoad);

        Assert.NotNull(result);
        Assert.IsType<ProjectRetargetHandler.TargetChange>(result);
    }

    [Theory]
    [InlineData(RetargetCheckOptions.ProjectRetarget)]
    [InlineData(RetargetCheckOptions.SolutionRetarget)]
    [InlineData(RetargetCheckOptions.ProjectLoad)]
    [InlineData(RetargetCheckOptions.ProjectRetarget | RetargetCheckOptions.SolutionRetarget)]
    public async Task CheckForRetargetAsync_WithValidOptions_CallsGetTargetChange(RetargetCheckOptions options)
    {
        var fileSystem = new IFileSystemMock();
        var solution = CreateSolutionWithDirectory(@"C:\Solution");

        string globalJsonPath = @"C:\Solution\global.json";
        await fileSystem.WriteAllTextAsync(globalJsonPath, GlobalJsonWithSdk);

        var releasesProvider = Mock.Of<IDotNetReleasesProvider>(
            p => p.GetSupportedOrLatestSdkVersionAsync("8.0.100", true, default) == Task.FromResult<string?>("8.0.200"));

        var dotnetEnvironment = Mock.Of<IDotNetEnvironment>(
            s => s.IsSdkInstalledAsync("8.0.200") == Task.FromResult(false));

        var retargetingService = new Mock<IVsTrackProjectRetargeting2>();
        retargetingService.Setup(r => r.RegisterProjectTarget(It.IsAny<IVsProjectTargetDescription>()))
            .Returns(HResult.OK);

        var handler = CreateInstance(
            fileSystem: fileSystem,
            solution: solution,
            releasesProvider: releasesProvider,
            dotnetEnvironment: dotnetEnvironment,
            trackProjectRetargeting: retargetingService.Object);

        var result = await handler.CheckForRetargetAsync(options);

        // Should get a result for valid options
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CheckForRetargetAsync_FindsGlobalJsonInParentDirectory()
    {
        var fileSystem = new IFileSystemMock();
        var solution = CreateSolutionWithDirectory(@"C:\Solution\SubFolder");

        // Create global.json in parent directory
        string globalJsonPath = @"C:\Solution\global.json";
        await fileSystem.WriteAllTextAsync(globalJsonPath, GlobalJsonWithSdk);

        var releasesProvider = Mock.Of<IDotNetReleasesProvider>(
            p => p.GetSupportedOrLatestSdkVersionAsync("8.0.100", true, default) == Task.FromResult<string?>("8.0.200"));

        var dotnetEnvironment = Mock.Of<IDotNetEnvironment>(
            s => s.IsSdkInstalledAsync("8.0.200") == Task.FromResult(false));

        var retargetingService = new Mock<IVsTrackProjectRetargeting2>();
        retargetingService.Setup(r => r.RegisterProjectTarget(It.IsAny<IVsProjectTargetDescription>()))
            .Returns(HResult.OK);

        var handler = CreateInstance(
            fileSystem: fileSystem,
            solution: solution,
            releasesProvider: releasesProvider,
            dotnetEnvironment: dotnetEnvironment,
            trackProjectRetargeting: retargetingService.Object);

        var result = await handler.CheckForRetargetAsync(RetargetCheckOptions.ProjectLoad);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task CheckForRetargetAsync_RegistersTargetDescriptions()
    {
        var fileSystem = new IFileSystemMock();
        var solution = CreateSolutionWithDirectory(@"C:\Solution");

        string globalJsonPath = @"C:\Solution\global.json";
        await fileSystem.WriteAllTextAsync(globalJsonPath, GlobalJsonWithSdk);

        var releasesProvider = Mock.Of<IDotNetReleasesProvider>(
            p => p.GetSupportedOrLatestSdkVersionAsync("8.0.100", true, default) == Task.FromResult<string?>("8.0.200"));

        var dotnetEnvironment = Mock.Of<IDotNetEnvironment>(
            s => s.IsSdkInstalledAsync("8.0.200") == Task.FromResult(false));

        var retargetingService = new Mock<IVsTrackProjectRetargeting2>();
        retargetingService.Setup(r => r.RegisterProjectTarget(It.IsAny<IVsProjectTargetDescription>()))
            .Returns(HResult.OK);

        var handler = CreateInstance(
            fileSystem: fileSystem,
            solution: solution,
            releasesProvider: releasesProvider,
            dotnetEnvironment: dotnetEnvironment,
            trackProjectRetargeting: retargetingService.Object);

        var result = await handler.CheckForRetargetAsync(RetargetCheckOptions.ProjectLoad);

        Assert.NotNull(result);
        // Verify RegisterProjectTarget was called twice (workaround for bug)
        retargetingService.Verify(r => r.RegisterProjectTarget(It.IsAny<IVsProjectTargetDescription>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetAffectedFilesAsync_ReturnsEmptyList()
    {
        var handler = CreateInstance();

        var result = await handler.GetAffectedFilesAsync(null!);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task RetargetAsync_ReturnsCompletedTask()
    {
        var handler = CreateInstance();

        await handler.RetargetAsync(TextWriter.Null, RetargetOptions.None, null!, string.Empty);

        // Should complete without throwing
    }

    [Fact]
    public void Dispose_WhenNoTargetsRegistered_DoesNotThrow()
    {
        var handler = CreateInstance();

        handler.Dispose();

        // Should complete without throwing
    }

    [Fact]
    public async Task Dispose_WhenTargetsRegistered_UnregistersTargets()
    {
        var fileSystem = new IFileSystemMock();
        var solution = CreateSolutionWithDirectory(@"C:\Solution");

        string globalJsonPath = @"C:\Solution\global.json";
        await fileSystem.WriteAllTextAsync(globalJsonPath, GlobalJsonWithSdk);

        var releasesProvider = Mock.Of<IDotNetReleasesProvider>(
            p => p.GetSupportedOrLatestSdkVersionAsync("8.0.100", true, default) == Task.FromResult<string?>("8.0.200"));

        var dotnetEnvironment = Mock.Of<IDotNetEnvironment>(
            s => s.IsSdkInstalledAsync("8.0.200") == Task.FromResult(false));

        var retargetingService = new Mock<IVsTrackProjectRetargeting2>();
        retargetingService.Setup(r => r.RegisterProjectTarget(It.IsAny<IVsProjectTargetDescription>()))
            .Returns(HResult.OK);
        retargetingService.Setup(r => r.UnregisterProjectTarget(It.IsAny<Guid>()))
            .Returns(HResult.OK);

        var handler = CreateInstance(
            fileSystem: fileSystem,
            solution: solution,
            releasesProvider: releasesProvider,
            dotnetEnvironment: dotnetEnvironment,
            trackProjectRetargeting: retargetingService.Object);

        // Register targets
        await handler.CheckForRetargetAsync(RetargetCheckOptions.ProjectLoad);

        // Dispose
        handler.Dispose();

        // Verify UnregisterProjectTarget was called twice (once for each registered target)
        retargetingService.Verify(r => r.UnregisterProjectTarget(It.IsAny<Guid>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CheckForRetargetAsync_WithInvalidGlobalJson_ReturnsNull()
    {
        var fileSystem = new IFileSystemMock();
        var solution = CreateSolutionWithDirectory(@"C:\Solution");

        // Create invalid JSON
        string globalJsonPath = @"C:\Solution\global.json";
        await fileSystem.WriteAllTextAsync(globalJsonPath, "{ invalid json }");

        var handler = CreateInstance(fileSystem: fileSystem, solution: solution);

        var result = await handler.CheckForRetargetAsync(RetargetCheckOptions.ProjectLoad);

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForRetargetAsync_CallsReleasesProviderWithCorrectParameters()
    {
        var fileSystem = new IFileSystemMock();
        var solution = CreateSolutionWithDirectory(@"C:\Solution");

        string globalJsonPath = @"C:\Solution\global.json";
        await fileSystem.WriteAllTextAsync(globalJsonPath, GlobalJsonWithSdk);

        var mockReleasesProvider = new Mock<IDotNetReleasesProvider>();
        mockReleasesProvider
            .Setup(p => p.GetSupportedOrLatestSdkVersionAsync("8.0.100", true, default))
            .ReturnsAsync((string?)null);

        var retargetingService = new Mock<IVsTrackProjectRetargeting2>();

        var handler = CreateInstance(
            fileSystem: fileSystem,
            solution: solution,
            releasesProvider: mockReleasesProvider.Object,
            trackProjectRetargeting: retargetingService.Object);

        var result = await handler.CheckForRetargetAsync(RetargetCheckOptions.ProjectLoad);

        // result should be null since releases provider returns null
        Assert.Null(result);
        
        // Verify the method was called with includePreview: true
        mockReleasesProvider.Verify(
            p => p.GetSupportedOrLatestSdkVersionAsync("8.0.100", true, default),
            Times.Once);
    }

    private static ProjectRetargetHandler CreateInstance(
        IDotNetReleasesProvider? releasesProvider = null,
        IFileSystem? fileSystem = null,
        IProjectThreadingService? threadingService = null,
        IVsTrackProjectRetargeting2? trackProjectRetargeting = null,
        IVsSolution? solution = null,
        IDotNetEnvironment? dotnetEnvironment = null)
    {
        releasesProvider ??= Mock.Of<IDotNetReleasesProvider>();
        fileSystem ??= new IFileSystemMock();
        threadingService ??= IProjectThreadingServiceFactory.Create();

        var retargetingService = IVsServiceFactory.Create<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2>(trackProjectRetargeting);
        var solutionService = IVsServiceFactory.Create<SVsSolution, IVsSolution>(solution);

        dotnetEnvironment ??= Mock.Of<IDotNetEnvironment>();

        return new ProjectRetargetHandler(
            new Lazy<IDotNetReleasesProvider>(() => releasesProvider),
            fileSystem,
            threadingService,
            retargetingService,
            solutionService,
            dotnetEnvironment);
    }

    private static IVsSolution CreateSolutionWithDirectory(string directory)
    {
        return IVsSolutionFactory.CreateWithSolutionDirectory(
            (out string solutionDirectory, out string solutionFile, out string userSettings) =>
            {
                solutionDirectory = directory;
                solutionFile = Path.Combine(directory, "Solution.sln");
                userSettings = string.Empty;
                return HResult.OK;
            });
    }
}
