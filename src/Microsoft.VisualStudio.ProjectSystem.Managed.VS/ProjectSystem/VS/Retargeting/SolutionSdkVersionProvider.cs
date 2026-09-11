// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.Json;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IFileSystem = Microsoft.VisualStudio.IO.IFileSystem;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

/// <summary>
///     Resolves the SDK version pinned by the solution's <c>global.json</c> once, and keeps it until a
///     <c>global.json</c> that could change the answer is added, changed or deleted.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="IProjectRetargetHandler"/> is exported per project, yet the <c>global.json</c> that pins
///         the SDK version is a property of the solution. Resolving it here means the probe walk from the
///         solution directory to the drive root happens once per solution, rather than once per project per
///         retarget check. Retargeting checks run on the main thread, so on a large solution the repeated walk
///         costs thousands of main thread file operations during solution load.
///     </para>
///     <para>
///         Every path the walk could probe is watched, so adding, changing or deleting any <c>global.json</c>
///         that could change the answer discards the cached value. Callers see the same values they would have
///         seen had each of them walked the file system themselves.
///     </para>
/// </remarks>
[Export(typeof(ISolutionSdkVersionProvider))]
internal sealed class SolutionSdkVersionProvider : OnceInitializedOnceDisposedUnderLockAsync, ISolutionSdkVersionProvider, IVsFreeThreadedFileChangeEvents2
{
    private const string GlobalJsonFileName = "global.json";

    /// <summary>
    ///     Indicates that <see cref="_sdkVersion"/> does not hold a cached value.
    /// </summary>
    private const int NoCachedValue = -1;

    private const _VSFILECHANGEFLAGS FileChangeFlags = _VSFILECHANGEFLAGS.VSFILECHG_Add | _VSFILECHANGEFLAGS.VSFILECHG_Del | _VSFILECHANGEFLAGS.VSFILECHG_Size | _VSFILECHANGEFLAGS.VSFILECHG_Time;

    private readonly ISolutionService _solutionService;
    private readonly IFileSystem _fileSystem;
    private readonly IVsService<IVsAsyncFileChangeEx> _fileChangeService;
    private readonly IProjectFaultHandlerService _faultHandlerService;

    /// <summary>
    ///     Cookies of the watches covering the candidate paths of <see cref="_watchedSolutionDirectory"/>.
    /// </summary>
    private readonly List<uint> _fileWatcherCookies = [];

    /// <summary>
    ///     Incremented whenever a watched <c>global.json</c> path changes. Written from the file watcher's
    ///     free threaded callback, so is only ever accessed via <see cref="Interlocked"/>/<see cref="Volatile"/>.
    /// </summary>
    private int _changeCount;

    private IVsAsyncFileChangeEx? _vsFileChange;
    private string? _watchedSolutionDirectory;
    private string? _sdkVersion;
    private int _cachedChangeCount = NoCachedValue;
    private bool _cacheEnabled;
    private int _fileChangeUnavailable;

    [ImportingConstructor]
    public SolutionSdkVersionProvider(
        ISolutionService solutionService,
        IFileSystem fileSystem,
        IVsService<SVsFileChangeEx, IVsAsyncFileChangeEx> fileChangeService,
        IProjectFaultHandlerService faultHandlerService,
        JoinableTaskContext joinableTaskContext)
        : base(new JoinableTaskContextNode(joinableTaskContext))
    {
        _solutionService = solutionService;
        _fileSystem = fileSystem;
        _fileChangeService = fileChangeService;
        _faultHandlerService = faultHandlerService;
    }

    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        _vsFileChange = await _fileChangeService.GetValueAsync(cancellationToken);
    }

    protected override async Task DisposeCoreUnderLockAsync(bool initialized)
    {
        Exception? error = await StopWatchingAsync();

        if (error is not null)
        {
            throw error;
        }
    }

    public async Task<string?> GetSdkVersionAsync(CancellationToken cancellationToken = default)
    {
        string? solutionDirectory = await _solutionService.GetSolutionDirectoryAsync(cancellationToken);

        if (Strings.IsNullOrEmpty(solutionDirectory))
        {
            return null;
        }

        if (Volatile.Read(ref _fileChangeUnavailable) != 0)
        {
            return (await ReadSdkVersionAsync(solutionDirectory)).SdkVersion;
        }

        try
        {
            await InitializeAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex.IsCatchable())
        {
            if (Interlocked.Exchange(ref _fileChangeUnavailable, 1) == 0)
            {
                ReportWatcherFailure(ex);
            }

            return (await ReadSdkVersionAsync(solutionDirectory)).SdkVersion;
        }

        return await ExecuteUnderLockAsync(token => GetSdkVersionUnderLockAsync(solutionDirectory, token), cancellationToken);
    }

    private async Task<string?> GetSdkVersionUnderLockAsync(string solutionDirectory, CancellationToken cancellationToken)
    {
        if (!string.Equals(_watchedSolutionDirectory, solutionDirectory, StringComparisons.Paths))
        {
            // A different solution is open. Give up the previous solution's cached value before moving the
            // watches. If watching fails, retargeting continues with uncached reads.
            _watchedSolutionDirectory = null;
            _sdkVersion = null;
            _cachedChangeCount = NoCachedValue;
            _cacheEnabled = false;

            ReportWatcherFailure(await StopWatchingAsync());

            _watchedSolutionDirectory = solutionDirectory;

            try
            {
                await StartWatchingAsync(solutionDirectory, cancellationToken);
                _cacheEnabled = true;
            }
            catch (OperationCanceledException)
            {
                ReportWatcherFailure(await StopWatchingAsync());
                _watchedSolutionDirectory = null;
                throw;
            }
            catch (Exception ex) when (ex.IsCatchable() && ex is not OperationCanceledException)
            {
                ReportWatcherFailure(ex);
                ReportWatcherFailure(await StopWatchingAsync());
            }
        }

        // Sample the change count before touching the file system. If a change arrives while we read, the
        // two no longer match, so we hand back what we read but the next caller reads the file system again.
        int changeCount = Volatile.Read(ref _changeCount);

        if (_cachedChangeCount == changeCount)
        {
            return _sdkVersion;
        }

        (string? sdkVersion, bool cacheable) = await ReadSdkVersionAsync(solutionDirectory);

        if (cacheable && _cacheEnabled)
        {
            _sdkVersion = sdkVersion;
            _cachedChangeCount = changeCount;
        }

        return sdkVersion;
    }

    private async Task<(string? SdkVersion, bool Cacheable)> ReadSdkVersionAsync(string solutionDirectory)
    {
        string? globalJsonPath = FindGlobalJsonPath(solutionDirectory);

        if (globalJsonPath is null)
        {
            return (null, true);
        }

        try
        {
            using Stream stream = _fileSystem.OpenTextStream(globalJsonPath);
            using JsonDocument doc = await JsonDocument.ParseAsync(stream);

            if (doc.RootElement.TryGetProperty("sdk", out JsonElement sdkProp) &&
                sdkProp.TryGetProperty("version", out JsonElement versionProp))
            {
                return (versionProp.GetString(), true);
            }

            return (null, true);
        }
        catch (Exception ex) when (ex.IsCatchable())
        {
            // Preserve the previous behavior of ignoring transient reads and malformed content, but do not
            // retain that result. A file-change notification can precede completion of the corresponding write.
            return (null, false);
        }
    }

    private string? FindGlobalJsonPath(string solutionDirectory)
    {
        foreach (string globalJsonPath in EnumerateCandidatePaths(solutionDirectory))
        {
            if (_fileSystem.FileExists(globalJsonPath))
            {
                return globalJsonPath;
            }
        }

        return null;
    }

    /// <summary>
    ///     Enumerates the <c>global.json</c> paths that apply to <paramref name="solutionDirectory"/>, from the
    ///     solution directory up to the root of its drive.
    /// </summary>
    private static IEnumerable<string> EnumerateCandidatePaths(string solutionDirectory)
    {
        string? directory = solutionDirectory;
        string? previousPath = null;

        while (!Strings.IsNullOrEmpty(directory))
        {
            string globalJsonPath = Path.Join(directory, GlobalJsonFileName);

            // The solution directory arrives with a trailing separator, which Path.GetDirectoryName trims
            // rather than treating as a level to walk up. Skipping the repeated path avoids probing and
            // watching the solution directory twice.
            if (!string.Equals(globalJsonPath, previousPath, StringComparisons.Paths))
            {
                yield return globalJsonPath;

                previousPath = globalJsonPath;
            }

            directory = Path.GetDirectoryName(directory);
        }
    }

    private async Task StartWatchingAsync(string solutionDirectory, CancellationToken cancellationToken)
    {
        Assumes.NotNull(_vsFileChange);

        // Watch every path the walk could probe, not just the file we end up reading. A global.json added
        // closer to the solution directory takes precedence over the one we found, and one added anywhere
        // in the chain changes the answer when we found nothing at all.
        foreach (string globalJsonPath in EnumerateCandidatePaths(solutionDirectory))
        {
            uint cookie = await _vsFileChange.AdviseFileChangeAsync(globalJsonPath, FileChangeFlags, sink: this, cancellationToken);

            _fileWatcherCookies.Add(cookie);
        }
    }

    private async Task<Exception?> StopWatchingAsync()
    {
        if (_fileWatcherCookies.Count == 0)
        {
            return null;
        }

        Assumes.NotNull(_vsFileChange);

        List<Exception>? errors = null;

        // Once cleanup starts, attempt every watch even if one removal fails. Successful cookies are forgotten;
        // failed cookies remain available for a later solution transition or disposal attempt.
        for (int i = _fileWatcherCookies.Count - 1; i >= 0; i--)
        {
            uint cookie = _fileWatcherCookies[i];

            try
            {
                await _vsFileChange.UnadviseFileChangeAsync(cookie, CancellationToken.None);
                _fileWatcherCookies.RemoveAt(i);
            }
            catch (Exception ex) when (ex.IsCatchable())
            {
                (errors ??= []).Add(ex);
            }
        }

        return errors is null ? null : new AggregateException(errors);
    }

    private void ReportWatcherFailure(Exception? error)
    {
        if (error is not null)
        {
            _faultHandlerService.RegisterFaultHandler(Task.FromException(error));
        }
    }

    public int FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
    {
        // Called free threaded. Any change to a candidate path can change the version we would resolve,
        // so discard what we cached. The next caller reads the file system again.
        Interlocked.Increment(ref _changeCount);

        return HResult.OK;
    }

    public int DirectoryChanged(string pszDirectory)
    {
        return HResult.NotImplemented;
    }

    public int DirectoryChangedEx(string pszDirectory, string pszFile)
    {
        return HResult.NotImplemented;
    }

    public int DirectoryChangedEx2(string pszDirectory, uint cChanges, string[] rgpszFile, uint[] rggrfChange)
    {
        return HResult.NotImplemented;
    }
}
