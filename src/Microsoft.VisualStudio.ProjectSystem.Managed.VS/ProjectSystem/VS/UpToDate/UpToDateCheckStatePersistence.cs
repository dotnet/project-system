// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    internal sealed partial class UpToDateCheckStatePersistence : OnceInitializedOnceDisposedAsync, IUpToDateCheckStatePersistence, IVsSolutionEvents
    {
        private const string ProjectItemCacheFileName = ".futdcache.v1";

        private readonly object _lock = new();

        private Dictionary<(string ProjectPath, IImmutableDictionary<string, string> ConfigurationDimensions), (int ItemHash, DateTime ItemsChangedAtUtc)>? _dataByConfiguredProject;

        private readonly IVsUIService<SVsSolution, IVsSolution> _solution;

        private bool _hasUnsavedChange;
        private uint _cookie = VSConstants.VSCOOKIE_NIL;
        private string? _cacheFilePath;

        [ImportingConstructor]
        public UpToDateCheckStatePersistence(
            IVsUIService<SVsSolution, IVsSolution> solution,
            JoinableTaskContext joinableTaskContext)
            : base(new JoinableTaskContextNode(joinableTaskContext))
        {
            _solution = solution;
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await JoinableFactory.SwitchToMainThreadAsync(cancellationToken);

            Verify.HResult(_solution.Value.AdviseSolutionEvents(this, out _cookie));
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                if (_cookie != VSConstants.VSCOOKIE_NIL)
                {
                    await JoinableFactory.SwitchToMainThreadAsync();

                    Verify.HResult(_solution.Value.UnadviseSolutionEvents(_cookie));

                    _cookie = VSConstants.VSCOOKIE_NIL;
                }
            }
        }

        public async Task<(int ItemHash, DateTime ItemsChangedAtUtc)?> RestoreStateAsync(string projectPath, IImmutableDictionary<string, string> configurationDimensions)
        {
            await InitializeAsync();
            await InitializeDataAsync();

            lock (_lock)
            {
                Assumes.NotNull(_dataByConfiguredProject);

                if (_dataByConfiguredProject.TryGetValue((projectPath, configurationDimensions), out (int ItemHash, DateTime ItemsChangedAtUtc) storedData))
                    return storedData;

                return null;
            }

            async Task InitializeDataAsync()
            {
                if (_cacheFilePath is null || _dataByConfiguredProject is null)
                {
                    string filePath = await GetCacheFilePathAsync(CancellationToken.None);

                    // Switch to a background thread before doing file I/O
                    await TaskScheduler.Default;

                    lock (_lock)
                    {
                        if (_cacheFilePath is null || _dataByConfiguredProject is null)
                        {
                            _cacheFilePath = filePath;
                            _dataByConfiguredProject = Deserialize(_cacheFilePath);
                        }
                    }
                }

                return;

                async Task<string> GetCacheFilePathAsync(CancellationToken cancellationToken)
                {
                    await JoinableFactory.SwitchToMainThreadAsync(cancellationToken);

                    var solutionWorkingFolder = _solution.Value as IVsSolutionWorkingFolders;

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
        }

        public void StoreState(string projectPath, IImmutableDictionary<string, string> configurationDimensions, int itemHash, DateTime itemsChangedAtUtc)
        {
            lock (_lock)
            {
                Assumes.NotNull(_dataByConfiguredProject);

                (string ProjectPath, IImmutableDictionary<string, string> ConfigurationDimensions) key = (ProjectPath: projectPath, ConfigurationDimensions: configurationDimensions);

                if (!_dataByConfiguredProject.TryGetValue(key, out (int ItemHash, DateTime ItemsChangedAtUtc) storedData) ||
                    storedData.ItemHash != itemHash ||
                    storedData.ItemsChangedAtUtc != itemsChangedAtUtc)
                {
                    _dataByConfiguredProject[key] = (itemHash, itemsChangedAtUtc);
                    _hasUnsavedChange = true;
                }
            }
        }

        #region Serialization

        private static void Serialize(string cacheFilePath, Dictionary<(string ProjectPath, IImmutableDictionary<string, string> ConfigurationDimensions), (int ItemHash, DateTime ItemsChangedAtUtc)> dataByConfiguredProject)
        {
            using var stream = new FileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            writer.Write(dataByConfiguredProject.Count);

            foreach (((string path, IImmutableDictionary<string, string> dimensions), (int ItemHash, DateTime ItemsChangedAtUtc) data) in dataByConfiguredProject)
            {
                writer.Write(path);

                writer.Write(dimensions.Count);

                foreach ((string name, string value) in dimensions)
                {
                    writer.Write(name);
                    writer.Write(value);
                }

                writer.Write(data.ItemHash);
                writer.Write(data.ItemsChangedAtUtc.Ticks);
            }
        }

        private static Dictionary<(string ProjectPath, IImmutableDictionary<string, string> ConfigurationDimensions), (int ItemHash, DateTime ItemsChangedAtUtc)> Deserialize(string cacheFilePath)
        {
            var data = new Dictionary<(string ProjectPath, IImmutableDictionary<string, string> ConfigurationDimensions), (int ItemHash, DateTime ItemsChangedAtUtc)>(ConfiguredProjectComparer.Instance);

            if (!File.Exists(cacheFilePath))
            {
                return data;
            }

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
                long ticks = reader.ReadInt64();
                var itemsChangedAtUtc = new DateTime(ticks, DateTimeKind.Utc);

                data[(path, dimensions.ToImmutable())] = (hash, itemsChangedAtUtc);
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
        public int OnBeforeCloseSolution(object pUnkReserved) => HResult.NotImplemented;

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            lock (_lock)
            {
                if (_hasUnsavedChange && _cacheFilePath is not null && _dataByConfiguredProject is not null)
                {
                    Serialize(_cacheFilePath, _dataByConfiguredProject);
                }

                _cacheFilePath = null;
                _dataByConfiguredProject = null;
            }

            return HResult.OK;
        }

        #endregion
    }
}
