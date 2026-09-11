// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

public class SolutionSdkVersionProviderTests
{
    private const string GlobalJsonWithSdk = """
        {
          "sdk": {
            "version": "8.0.100"
          }
        }
        """;

    private const string GlobalJsonWithNewerSdk = """
        {
          "sdk": {
            "version": "9.0.100"
          }
        }
        """;

    [Fact]
    public async Task GetSdkVersionAsync_WhenNoSolution_ReturnsNull()
    {
        var provider = CreateInstance(solutionDirectory: null);

        Assert.Null(await provider.GetSdkVersionAsync());
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenNoGlobalJson_ReturnsNull()
    {
        var provider = CreateInstance(new IFileSystemMock(), @"C:\Solution");

        Assert.Null(await provider.GetSdkVersionAsync());
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenGlobalJsonInSolutionDirectory_ReturnsVersion()
    {
        var fileSystem = new IFileSystemMock();
        await fileSystem.WriteAllTextAsync(@"C:\Solution\global.json", GlobalJsonWithSdk);

        var provider = CreateInstance(fileSystem, @"C:\Solution");

        Assert.Equal("8.0.100", await provider.GetSdkVersionAsync());
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenGlobalJsonInParentDirectory_ReturnsVersion()
    {
        var fileSystem = new IFileSystemMock();
        await fileSystem.WriteAllTextAsync(@"C:\Solution\global.json", GlobalJsonWithSdk);

        var provider = CreateInstance(fileSystem, @"C:\Solution\SubFolder");

        Assert.Equal("8.0.100", await provider.GetSdkVersionAsync());
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenGlobalJsonHasNoSdkVersion_ReturnsNull()
    {
        var fileSystem = new IFileSystemMock();
        await fileSystem.WriteAllTextAsync(@"C:\Solution\global.json", "{}");

        var provider = CreateInstance(fileSystem, @"C:\Solution");

        Assert.Null(await provider.GetSdkVersionAsync());
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenGlobalJsonIsInvalid_ReturnsNull()
    {
        var fileSystem = new IFileSystemMock();
        await fileSystem.WriteAllTextAsync(@"C:\Solution\global.json", "{ invalid json }");

        var provider = CreateInstance(fileSystem, @"C:\Solution");

        Assert.Null(await provider.GetSdkVersionAsync());
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenGlobalJsonReadFails_DoesNotCacheFailure()
    {
        const string GlobalJsonPath = @"C:\Solution\global.json";

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.FileExists(GlobalJsonPath)).Returns(true);
        fileSystem.SetupSequence(f => f.OpenTextStream(GlobalJsonPath))
            .Throws<IOException>()
            .Returns(new MemoryStream(Encoding.UTF8.GetBytes(GlobalJsonWithSdk)));

        var provider = CreateInstance(fileSystem.Object, @"C:\Solution");

        Assert.Null(await provider.GetSdkVersionAsync());
        Assert.Equal("8.0.100", await provider.GetSdkVersionAsync());
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenFileChangeServiceFails_ReadsWithoutCaching()
    {
        const string GlobalJsonPath = @"C:\Solution\global.json";

        var fileSystem = new IFileSystemMock();
        await fileSystem.WriteAllTextAsync(GlobalJsonPath, GlobalJsonWithSdk);

        var fileChangeService = new Mock<IVsService<SVsFileChangeEx, IVsAsyncFileChangeEx>>();
        fileChangeService.Setup(s => s.GetValueAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("The file change service is unavailable."));

        var provider = new SolutionSdkVersionProvider(
            ISolutionServiceFactory.Create(@"C:\Solution"),
            fileSystem,
            fileChangeService.Object,
            Mock.Of<IProjectFaultHandlerService>(),
            JoinableTaskContextNodeFactory.Create().Context);

        Assert.Equal("8.0.100", await provider.GetSdkVersionAsync());

        await fileSystem.WriteAllTextAsync(GlobalJsonPath, GlobalJsonWithNewerSdk);

        Assert.Equal("9.0.100", await provider.GetSdkVersionAsync());
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenCalledRepeatedly_ProbesFileSystemOnce()
    {
        var fileSystem = CreateProbeCountingFileSystem();

        var provider = CreateInstance(fileSystem.Object, @"C:\Solution\Nested");

        for (int i = 0; i < 10; i++)
        {
            Assert.Null(await provider.GetSdkVersionAsync());
        }

        // C:\Solution\Nested, C:\Solution and C:\ -- walked once, not once per call.
        fileSystem.Verify(f => f.FileExists(It.IsAny<string>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenSolutionDirectoryHasTrailingSeparator_DoesNotProbeItTwice()
    {
        var fileSystem = CreateProbeCountingFileSystem();

        // IVsSolution.GetSolutionInfo hands back the solution directory with a trailing separator.
        var provider = CreateInstance(fileSystem.Object, @"C:\Solution\Nested\");

        Assert.Null(await provider.GetSdkVersionAsync());

        fileSystem.Verify(f => f.FileExists(@"C:\Solution\Nested\global.json"), Times.Once);
        fileSystem.Verify(f => f.FileExists(It.IsAny<string>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GetSdkVersionAsync_WatchesEveryPathThatCouldBeProbed()
    {
        var fileChangeService = new IVsAsyncFileChangeExMock();

        var provider = CreateInstance(new IFileSystemMock(), @"C:\Solution\Nested", fileChangeService);

        Assert.Null(await provider.GetSdkVersionAsync());

        Assert.Equal(
            new[] { @"C:\Solution\Nested\global.json", @"C:\Solution\global.json", @"C:\global.json" }.OrderBy(path => path),
            fileChangeService.WatchedFiles.OrderBy(path => path));
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenSolutionDirectoryDiffers_ProbesEachSolution()
    {
        var fileSystem = new IFileSystemMock();
        await fileSystem.WriteAllTextAsync(@"C:\First\global.json", GlobalJsonWithSdk);
        await fileSystem.WriteAllTextAsync(@"C:\Second\global.json", GlobalJsonWithNewerSdk);

        string? solutionDirectory = @"C:\First";

        var provider = CreateInstance(fileSystem, CreateSolutionService(() => solutionDirectory));

        Assert.Equal("8.0.100", await provider.GetSdkVersionAsync());

        solutionDirectory = @"C:\Second";

        Assert.Equal("9.0.100", await provider.GetSdkVersionAsync());
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenSolutionDirectoryDiffers_StopsWatchingPreviousSolutionsPaths()
    {
        string? solutionDirectory = @"C:\First";

        var fileChangeService = new IVsAsyncFileChangeExMock();

        var provider = CreateInstance(new IFileSystemMock(), CreateSolutionService(() => solutionDirectory), fileChangeService);

        Assert.Null(await provider.GetSdkVersionAsync());

        solutionDirectory = @"C:\Second";

        Assert.Null(await provider.GetSdkVersionAsync());

        Assert.Equal(
            new[] { @"C:\Second\global.json", @"C:\global.json" }.OrderBy(path => path),
            fileChangeService.WatchedFiles.OrderBy(path => path));
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenGlobalJsonChanges_ReturnsNewVersion()
    {
        var fileSystem = new IFileSystemMock();
        await fileSystem.WriteAllTextAsync(@"C:\Solution\global.json", GlobalJsonWithSdk);

        var provider = CreateInstance(fileSystem, @"C:\Solution");

        Assert.Equal("8.0.100", await provider.GetSdkVersionAsync());

        await fileSystem.WriteAllTextAsync(@"C:\Solution\global.json", GlobalJsonWithNewerSdk);
        RaiseFileChanged(provider, @"C:\Solution\global.json");

        Assert.Equal("9.0.100", await provider.GetSdkVersionAsync());
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenGlobalJsonAdded_ReturnsVersion()
    {
        var fileSystem = new IFileSystemMock();

        var provider = CreateInstance(fileSystem, @"C:\Solution");

        Assert.Null(await provider.GetSdkVersionAsync());

        await fileSystem.WriteAllTextAsync(@"C:\Solution\global.json", GlobalJsonWithSdk);
        RaiseFileChanged(provider, @"C:\Solution\global.json");

        Assert.Equal("8.0.100", await provider.GetSdkVersionAsync());
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenGlobalJsonAddedCloserToSolution_ReturnsItsVersion()
    {
        var fileSystem = new IFileSystemMock();
        await fileSystem.WriteAllTextAsync(@"C:\global.json", GlobalJsonWithSdk);

        var provider = CreateInstance(fileSystem, @"C:\Solution");

        Assert.Equal("8.0.100", await provider.GetSdkVersionAsync());

        await fileSystem.WriteAllTextAsync(@"C:\Solution\global.json", GlobalJsonWithNewerSdk);
        RaiseFileChanged(provider, @"C:\Solution\global.json");

        Assert.Equal("9.0.100", await provider.GetSdkVersionAsync());
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenGlobalJsonRemoved_ReturnsVersionFromParentDirectory()
    {
        var fileSystem = new IFileSystemMock();
        await fileSystem.WriteAllTextAsync(@"C:\global.json", GlobalJsonWithSdk);
        await fileSystem.WriteAllTextAsync(@"C:\Solution\global.json", GlobalJsonWithNewerSdk);

        var provider = CreateInstance(fileSystem, @"C:\Solution");

        Assert.Equal("9.0.100", await provider.GetSdkVersionAsync());

        fileSystem.RemoveFile(@"C:\Solution\global.json");
        RaiseFileChanged(provider, @"C:\Solution\global.json");

        Assert.Equal("8.0.100", await provider.GetSdkVersionAsync());
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenSolutionClosedAndReopened_ObservesChangeMadeWhileClosed()
    {
        var fileSystem = new IFileSystemMock();
        await fileSystem.WriteAllTextAsync(@"C:\Solution\global.json", GlobalJsonWithSdk);

        string? solutionDirectory = @"C:\Solution";

        var provider = CreateInstance(fileSystem, CreateSolutionService(() => solutionDirectory));

        Assert.Equal("8.0.100", await provider.GetSdkVersionAsync());

        // Close the solution, change global.json, then reopen the same solution.
        solutionDirectory = null;

        Assert.Null(await provider.GetSdkVersionAsync());

        await fileSystem.WriteAllTextAsync(@"C:\Solution\global.json", GlobalJsonWithNewerSdk);
        RaiseFileChanged(provider, @"C:\Solution\global.json");

        solutionDirectory = @"C:\Solution";

        Assert.Equal("9.0.100", await provider.GetSdkVersionAsync());
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenGlobalJsonChangesWhileBeingRead_DoesNotCacheIt()
    {
        const string GlobalJsonPath = @"C:\Solution\global.json";

        string contents = GlobalJsonWithSdk;
        bool changeRaised = false;
        SolutionSdkVersionProvider? provider = null;

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.FileExists(GlobalJsonPath)).Returns(true);
        fileSystem.Setup(f => f.OpenTextStream(GlobalJsonPath))
            .Returns(() =>
            {
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(contents));

                if (!changeRaised)
                {
                    // The file changes after we opened it, but before we cache what we read from it.
                    changeRaised = true;
                    contents = GlobalJsonWithNewerSdk;

                    Assumes.NotNull(provider);
                    RaiseFileChanged(provider, GlobalJsonPath);
                }

                return stream;
            });

        provider = CreateInstance(fileSystem.Object, @"C:\Solution");

        Assert.Equal("8.0.100", await provider.GetSdkVersionAsync());

        Assert.Equal("9.0.100", await provider.GetSdkVersionAsync());
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenWatchingNewSolutionFails_ReadsWithoutCaching()
    {
        var fileSystem = new IFileSystemMock();
        await fileSystem.WriteAllTextAsync(@"C:\First\global.json", GlobalJsonWithSdk);

        string? solutionDirectory = @"C:\First";

        var fileChangeService = new InterruptedFileChangeServiceMock { AdviseFailurePath = @"C:\Second\global.json" };

        var provider = CreateInstance(fileSystem, CreateSolutionService(() => solutionDirectory), fileChangeService);

        Assert.Equal("8.0.100", await provider.GetSdkVersionAsync());

        // Moving our watches over to the second solution fails part way through.
        solutionDirectory = @"C:\Second";

        Assert.Null(await provider.GetSdkVersionAsync());

        fileChangeService.AdviseFailurePath = null;

        // The first solution's global.json changes while we are not watching it, so we are never told about it.
        await fileSystem.WriteAllTextAsync(@"C:\First\global.json", GlobalJsonWithNewerSdk);

        solutionDirectory = @"C:\First";

        Assert.Equal("9.0.100", await provider.GetSdkVersionAsync());

        Assert.Equal(
            new[] { @"C:\First\global.json", @"C:\global.json" }.OrderBy(path => path),
            fileChangeService.WatchedFiles.OrderBy(path => path));
    }

    [Fact]
    public async Task GetSdkVersionAsync_WhenWatchingIsCancelled_WatchesEachPathOnceOnNextCall()
    {
        var fileSystem = new IFileSystemMock();
        await fileSystem.WriteAllTextAsync(@"C:\Solution\Nested\global.json", GlobalJsonWithSdk);

        var fileChangeService = new InterruptedFileChangeServiceMock
        {
            AdviseFailurePath = @"C:\global.json",
            Error = new OperationCanceledException()
        };

        var provider = CreateInstance(fileSystem, @"C:\Solution\Nested", fileChangeService);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetSdkVersionAsync());

        fileChangeService.AdviseFailurePath = null;

        Assert.Equal("8.0.100", await provider.GetSdkVersionAsync());

        // The watches taken before the cancellation are replaced, not duplicated or leaked.
        Assert.Equal(
            new[] { @"C:\Solution\Nested\global.json", @"C:\Solution\global.json", @"C:\global.json" }.OrderBy(path => path),
            fileChangeService.WatchedFiles.OrderBy(path => path));
    }

    [Fact]
    public async Task DisposeAsync_WhenUnwatchingPreviouslyFailed_RetriesTheCookie()
    {
        string? solutionDirectory = @"C:\First";

        var fileChangeService = new InterruptedFileChangeServiceMock
        {
            UnadviseFailurePath = @"C:\First\global.json",
            Error = new InvalidOperationException("The file change service failed.")
        };

        var provider = CreateInstance(new IFileSystemMock(), CreateSolutionService(() => solutionDirectory), fileChangeService);

        Assert.Null(await provider.GetSdkVersionAsync());

        // Unwatching the first solution fails before one of its watches has been removed. Retargeting still
        // proceeds for the second solution because file watching is only a cache optimization.
        solutionDirectory = @"C:\Second";

        Assert.Null(await provider.GetSdkVersionAsync());

        fileChangeService.UnadviseFailurePath = null;

        await provider.DisposeAsync();

        // Disposal retries the watch whose earlier removal failed and removes all current-solution watches.
        Assert.Empty(fileChangeService.WatchedFiles);
    }
    private static void RaiseFileChanged(SolutionSdkVersionProvider provider, string path)
    {
        provider.FilesChanged(cChanges: 1, [path], [(uint)_VSFILECHANGEFLAGS.VSFILECHG_Time]);
    }

    private static Mock<IFileSystem> CreateProbeCountingFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
        return fileSystem;
    }

    private static ISolutionService CreateSolutionService(Func<string?> solutionDirectory)
    {
        var solutionService = new Mock<ISolutionService>();
        solutionService.Setup(s => s.GetSolutionDirectoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(solutionDirectory);

        return solutionService.Object;
    }

    private static SolutionSdkVersionProvider CreateInstance(
        IFileSystem? fileSystem = null,
        string? solutionDirectory = null,
        IVsAsyncFileChangeExMock? fileChangeService = null)
    {
        return CreateInstance(fileSystem, ISolutionServiceFactory.Create(solutionDirectory), fileChangeService);
    }

    private static SolutionSdkVersionProvider CreateInstance(
        IFileSystem? fileSystem,
        ISolutionService solutionService,
        IVsAsyncFileChangeExMock? fileChangeService = null)
    {
        return new SolutionSdkVersionProvider(
            solutionService,
            fileSystem ?? new IFileSystemMock(),
            IVsServiceFactory.Create<SVsFileChangeEx, IVsAsyncFileChangeEx>(fileChangeService ?? new IVsAsyncFileChangeExMock()),
            Mock.Of<IProjectFaultHandlerService>(),
            JoinableTaskContextNodeFactory.Create().Context);
    }

    /// <summary>
    ///     Fails a single advise or unadvise, modelling the file change service failing, or the operation being
    ///     cancelled, part way through moving our watches from one solution to another.
    /// </summary>
    private sealed class InterruptedFileChangeServiceMock : IVsAsyncFileChangeExMock
    {
        public string? AdviseFailurePath { get; set; }

        public string? UnadviseFailurePath { get; set; }

        public Exception Error { get; set; } = new InvalidOperationException("The file change service failed.");

        public override Task<uint> AdviseFileChangeAsync(string filename, _VSFILECHANGEFLAGS filter, IVsFreeThreadedFileChangeEvents2 sink, CancellationToken cancellationToken = default)
        {
            if (filename == AdviseFailurePath)
            {
                throw Error;
            }

            return base.AdviseFileChangeAsync(filename, filter, sink, cancellationToken);
        }

        public override async Task<string> UnadviseFileChangeAsync(uint cookie, CancellationToken cancellationToken = default)
        {
            string filename = GetWatchedFile(cookie);

            if (filename == UnadviseFailurePath)
            {
                throw Error;
            }

            return await base.UnadviseFileChangeAsync(cookie, cancellationToken);
        }
    }
}
