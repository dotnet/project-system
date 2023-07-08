// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ProjectSystem.UpToDate;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UpToDate
{
    /// <summary>
    /// Implementation of <see cref="IUpToDateCheckStatePersistence" /> for use in Visual Studio.
    /// </summary>
    /// <remarks>
    /// Stores the required data to disk in the <c>.vs</c> folder.
    /// </remarks>
    [Export(typeof(IUpToDateCheckStatePersistence))]
    [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
    internal sealed partial class UpToDateCheckStatePersistence : OnceInitializedOnceDisposedUnderLockAsync, IUpToDateCheckStatePersistence, IVsSolutionEvents
    {
        // The name of the file within the .vs folder that we use to store data for the up-to-date check.
        // Note the suffix that indicates the file format's version. If a change is made to the format,
        // this number must be bumped.
        private const string ProjectItemCacheFileName = ".futdcache.v2";

        private Dictionary<(string ProjectPath, IImmutableDictionary<string, string> ConfigurationDimensions), (int ItemHash, DateTime? ItemsChangedAtUtc, DateTime? LastSuccessfulBuildStartedAtUtc)>? _dataByConfiguredProject;

        private readonly ISolutionService _solutionService;

        private bool _hasUnsavedChange;
        private IAsyncDisposable? _solutionEventsSubscription;
        private string? _cacheFilePath;
        private JoinableTask? _cleanupTask;

        [ImportingConstructor]
        public UpToDateCheckStatePersistence(
            ISolutionService solutionService,
            JoinableTaskContext joinableTaskContext)
            : base(new JoinableTaskContextNode(joinableTaskContext))
        {
            _solutionService = solutionService;
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _solutionEventsSubscription = await _solutionService.SubscribeAsync(this, cancellationToken);
        }

        protected override async Task DisposeCoreUnderLockAsync(bool initialized)
        {
            if (initialized)
            {
                Assumes.NotNull(_solutionEventsSubscription);

                await _solutionEventsSubscription.DisposeAsync();
            }
        }

        public async Task<(int ItemHash, DateTime? ItemsChangedAtUtc)?> RestoreItemStateAsync(string projectPath, IImmutableDictionary<string, string> configurationDimensions, CancellationToken cancellationToken)
        {
            await InitializeAsync(cancellationToken);

            return await ExecuteUnderLockAsync<(int ItemHash, DateTime? ItemsChangedAtUtc)?>(
                async token =>
                {
                    await EnsureDataInitializedAsync(token);

                    Assumes.NotNull(_dataByConfiguredProject);

                    if (_dataByConfiguredProject.TryGetValue((projectPath, configurationDimensions), out (int ItemHash, DateTime? ItemsChangedAtUtc, DateTime? LastSuccessfulBuildStartedAtUtc) storedData))
                    {
                        return (storedData.ItemHash, storedData.ItemsChangedAtUtc);
                    }

                    return null;
                },
                cancellationToken);
        }

        public Task StoreItemStateAsync(string projectPath, IImmutableDictionary<string, string> configurationDimensions, int itemHash, DateTime? itemsChangedAtUtc, CancellationToken cancellationToken)
        {
            Requires.Argument(itemsChangedAtUtc != DateTime.MinValue, nameof(itemsChangedAtUtc), "Must not be DateTime.MinValue.");

            return ExecuteUnderLockAsync(
                token =>
                {
                    if (_dataByConfiguredProject is not null)
                    {
                        (string ProjectPath, IImmutableDictionary<string, string> ConfigurationDimensions) key = (ProjectPath: projectPath, ConfigurationDimensions: configurationDimensions);

                        if (!_dataByConfiguredProject.TryGetValue(key, out (int ItemHash, DateTime? ItemsChangedAtUtc, DateTime? LastSuccessfulBuildStartedAtUtc) storedData)
                            || storedData.ItemHash != itemHash
                            || storedData.ItemsChangedAtUtc != itemsChangedAtUtc)
                        {
                            _dataByConfiguredProject[key] = (itemHash, itemsChangedAtUtc, storedData.LastSuccessfulBuildStartedAtUtc);
                            _hasUnsavedChange = true;
                        }
                    }

                    return Task.CompletedTask;
                },
                cancellationToken);
        }

        public async Task<DateTime?> RestoreLastSuccessfulBuildStateAsync(string projectPath, IImmutableDictionary<string, string> configurationDimensions, CancellationToken cancellationToken)
        {
            await InitializeAsync(cancellationToken);

            return await ExecuteUnderLockAsync(
                async token =>
                {
                    await EnsureDataInitializedAsync(token);

                    Assumes.NotNull(_dataByConfiguredProject);

                    if (_dataByConfiguredProject.TryGetValue((projectPath, configurationDimensions), out (int ItemHash, DateTime? ItemsChangedAtUtc, DateTime? LastSuccessfulBuildStartedAtUtc) storedData)
                        && storedData.LastSuccessfulBuildStartedAtUtc is not null and { Ticks: > 0 })
                    {
                        return storedData.LastSuccessfulBuildStartedAtUtc;
                    }

                    return null;
                },
                cancellationToken);
        }

        public Task StoreLastSuccessfulBuildStateAsync(string projectPath, IImmutableDictionary<string, string> configurationDimensions, DateTime lastSuccessfulBuildStartedAtUtc, CancellationToken cancellationToken)
        {
            Requires.Argument(lastSuccessfulBuildStartedAtUtc != DateTime.MinValue, nameof(lastSuccessfulBuildStartedAtUtc), "Must not be DateTime.MinValue.");

            return ExecuteUnderLockAsync(
                token =>
                {
                    if (_dataByConfiguredProject is not null)
                    {
                        (string ProjectPath, IImmutableDictionary<string, string> ConfigurationDimensions) key = (ProjectPath: projectPath, ConfigurationDimensions: configurationDimensions);

                        if (!_dataByConfiguredProject.TryGetValue(key, out (int ItemHash, DateTime? ItemsChangedAtUtc, DateTime? LastSuccessfulBuildStartedAtUtc) storedData)
                            || storedData.LastSuccessfulBuildStartedAtUtc != lastSuccessfulBuildStartedAtUtc)
                        {
                            _dataByConfiguredProject[key] = (storedData.ItemHash, storedData.ItemsChangedAtUtc, lastSuccessfulBuildStartedAtUtc);
                            _hasUnsavedChange = true;
                        }
                    }

                    return Task.CompletedTask;
                },
                cancellationToken);
        }

        private async Task EnsureDataInitializedAsync(CancellationToken cancellationToken)
        {
            if (_cacheFilePath is null || _dataByConfiguredProject is null)
            {
                string filePath = await GetCacheFilePathAsync(cancellationToken);

                // Switch to a background thread before doing file I/O
                await TaskScheduler.Default;

                if (_cacheFilePath is null || _dataByConfiguredProject is null)
                {
                    _cacheFilePath = filePath;
                    _dataByConfiguredProject = Deserialize(_cacheFilePath);
                }
            }

            return;

            async Task<string> GetCacheFilePathAsync(CancellationToken cancellationToken)
            {
                await JoinableFactory.SwitchToMainThreadAsync(cancellationToken);

                var solutionWorkingFolder = _solutionService.Solution as IVsSolutionWorkingFolders;

                Assumes.Present(solutionWorkingFolder);

                solutionWorkingFolder.GetFolder(
                    (uint)__SolutionWorkingFolder.SlnWF_StatePersistence,
                    guidProject: Guid.Empty,
                    fVersionSpecific: true,
                    fEnsureCreated: true,
                    out bool isTemporary,
                    out string workingFolderPath);

                return Path.Combine(workingFolderPath, ProjectItemCacheFileName);
            }
        }

        #region Serialization

        private static void Serialize(string cacheFilePath, Dictionary<(string ProjectPath, IImmutableDictionary<string, string> ConfigurationDimensions), (int ItemHash, DateTime? ItemsChangedAtUtc, DateTime? LastSuccessfulBuildStartedAtUtc)> dataByConfiguredProject)
        {
            using var stream = new FileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            writer.Write(dataByConfiguredProject.Count);

            foreach (((string path, IImmutableDictionary<string, string> dimensions), (int ItemHash, DateTime? ItemsChangedAtUtc, DateTime? LastSuccessfulBuildStartedAtUtc) data) in dataByConfiguredProject)
            {
                writer.Write(path);

                writer.Write(dimensions.Count);

                foreach ((string name, string value) in dimensions)
                {
                    writer.Write(name);
                    writer.Write(value);
                }

                writer.Write(data.ItemHash);
                writer.Write(data.ItemsChangedAtUtc?.Ticks ?? 0L);
                writer.Write(data.LastSuccessfulBuildStartedAtUtc?.Ticks ?? 0L);
            }
        }

        private static Dictionary<(string ProjectPath, IImmutableDictionary<string, string> ConfigurationDimensions), (int ItemHash, DateTime? ItemsChangedAtUtc, DateTime? LastSuccessfulBuildStartedAtUtc)> Deserialize(string cacheFilePath)
        {
            var data = new Dictionary<(string ProjectPath, IImmutableDictionary<string, string> ConfigurationDimensions), (int ItemHash, DateTime? ItemsChangedAtUtc, DateTime? LastSuccessfulBuildStartedAtUtc)>(ConfiguredProjectComparer.Instance);

            if (!File.Exists(cacheFilePath))
            {
                return data;
            }

            try
            {
                using var stream = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

                int configuredProjectCount = reader.ReadInt32();

                while (configuredProjectCount-- != 0)
                {
                    string path = reader.ReadString();

                    int dimensionCount = reader.ReadInt32();
                    var dimensions = ImmutableStringDictionary<string>.EmptyOrdinal.ToBuilder();

                    while (dimensionCount-- != 0)
                    {
                        string name = reader.ReadString();
                        string value = reader.ReadString();
                        dimensions[name] = value;
                    }

                    int hash = reader.ReadInt32();
                    var itemsChangedAtUtc = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);
                    var lastSuccessfulBuildStartedAtUtc = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);

                    data[(path, dimensions.ToImmutable())] = (hash, itemsChangedAtUtc, lastSuccessfulBuildStartedAtUtc);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
                // Return empty data in case of failure. Assume the whole file is corrupted.
                return new();
            }

            return data;
        }

        #endregion

        #region IVsSolutionEvents

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) => HResult.NotImplemented;
        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => HResult.NotImplemented;
        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => HResult.NotImplemented;
        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => HResult.NotImplemented;
        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => HResult.NotImplemented;
        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => HResult.NotImplemented;
        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution) => HResult.NotImplemented;
        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => HResult.NotImplemented;
        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            // Kick off clean up work now. We will join on it after solution close.
            _cleanupTask = JoinableFactory.RunAsync(async () =>
            {
                await TaskScheduler.Default;

                await ExecuteUnderLockAsync(
                    _ =>
                    {
                        if (_hasUnsavedChange && _cacheFilePath is not null && _dataByConfiguredProject is not null)
                        {
                            Serialize(_cacheFilePath, _dataByConfiguredProject);
                        }

                        _cacheFilePath = null;
                        _dataByConfiguredProject = null;
                        _hasUnsavedChange = false;

                        return Task.CompletedTask;
                    });
            });

            return HResult.OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            // Wait for any async clean up to complete. We need to ensure this occurs before we close
            // the solution so that if we are immediately re-opening the solution (e.g. during branch
            // switching where the .sln file changed) we will restore the persisted state correctly.
            _cleanupTask?.Join();
            _cleanupTask = null;

            return HResult.OK;
        }

        #endregion
    }
}
