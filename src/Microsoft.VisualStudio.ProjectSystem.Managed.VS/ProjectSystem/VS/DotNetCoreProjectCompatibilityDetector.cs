// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// <see cref="IDotNetCoreProjectCompatibilityDetector"/>
    /// </summary>
    [Export(typeof(IDotNetCoreProjectCompatibilityDetector))]
    internal sealed class DotNetCoreProjectCompatibilityDetector : IDotNetCoreProjectCompatibilityDetector, IVsSolutionEvents, IVsSolutionLoadEvents, IDisposable
    {
        private const string SupportedLearnMoreFwlink = "https://go.microsoft.com/fwlink/?linkid=866848";
        private const string UnsupportedLearnMoreFwlink = "https://go.microsoft.com/fwlink/?linkid=866796";
        private const string SuppressDotNewCoreWarningKey = @"ManagedProjectSystem\SuppressDotNewCoreWarning";
        private const string VersionCompatibilityDownloadFwlink = "https://go.microsoft.com/fwlink/?linkid=866798";
        private const string VersionDataFilename = "DotNetVersionCompatibility.json";
        private const int CacheFileValidHours = 24;

        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { }

        enum CompatibilityLevel
        {
            Recommended = 0,
            Supported = 1,
            NotSupported = 2
        }
        [ImportingConstructor]
        public DotNetCoreProjectCompatibilityDetector([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, Lazy<IProjectServiceAccessor> projectAccessor,
                                                      Lazy<IDialogServices> dialogServices, Lazy<IProjectThreadingService> threadHandling, Lazy<IVsShellUtilitiesHelper> vsShellUtilitiesHelper,
                                                      Lazy<IFileSystem> fileSystem, Lazy<IHttpClient> httpClient)
        {
            _serviceProvider = serviceProvider;
            _projectServiceAccessor = projectAccessor;
            _dialogServices = dialogServices;
            _threadHandling = threadHandling;
            _shellUtilitiesHelper = vsShellUtilitiesHelper;
            _fileSystem = fileSystem;
            _httpClient = httpClient;
        }

        private readonly IServiceProvider _serviceProvider;
        private readonly Lazy<IProjectServiceAccessor> _projectServiceAccessor;
        private readonly Lazy<IDialogServices> _dialogServices;
        private readonly Lazy<IProjectThreadingService> _threadHandling;
        private readonly Lazy<IVsShellUtilitiesHelper> _shellUtilitiesHelper;
        private readonly Lazy<IFileSystem> _fileSystem;
        private readonly Lazy<IHttpClient> _httpClient;

        private RemoteCacheFile _versionDataCacheFile;
        private Version _ourVSVersion;

        private uint _solutionCookie = VSConstants.VSCOOKIE_NIL;
        private bool _solutionOpened;
        private CompatibilityLevel _compatibilityLevelWarnedForThisSolution = CompatibilityLevel.Recommended;

        // Tracks how often we meed to look for new data
        private DateTime _timeCurVersionDataLastUpdatedUtc = DateTime.MinValue;
        private VersionCompatibilityData _curVersionCompatibilityData;

        public async Task InitializeAsync()
        {
            await _threadHandling.Value.SwitchToUIThread();

            // Initialize our cache file
            string appDataFolder = await _shellUtilitiesHelper.Value.GetLocalAppDataFolderAsync(_serviceProvider).ConfigureAwait(true);
            if (appDataFolder != null)
            {
                _versionDataCacheFile = new RemoteCacheFile(Path.Combine(appDataFolder, VersionDataFilename), VersionCompatibilityDownloadFwlink,
                                                            TimeSpan.FromHours(CacheFileValidHours), _fileSystem.Value, _httpClient);
            }

            _ourVSVersion = await _shellUtilitiesHelper.Value.GetVSVersionAsync(_serviceProvider).ConfigureAwait(true);

            IVsSolution solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
            Verify.HResult(solution.AdviseSolutionEvents(this, out _solutionCookie));

            // Check to see if a solution is already open. If so we set _solutionOpened to true so that subsequent projects added to 
            // this solution are processed.
            if (ErrorHandler.Succeeded(solution.GetProperty((int)__VSPROPID4.VSPROPID_IsSolutionFullyLoaded, out object isFullyLoaded)) && isFullyLoaded is bool && (bool)isFullyLoaded)
            {
                _solutionOpened = true;
            }
        }

        public void Dispose()
        {
            _threadHandling.Value.VerifyOnUIThread();

            if (_solutionCookie != VSConstants.VSCOOKIE_NIL)
            {
                var solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
                if (solution != null)
                {
                    Verify.HResult(solution.UnadviseSolutionEvents(_solutionCookie));
                    _solutionCookie = VSConstants.VSCOOKIE_NIL;
                }
            }
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            // Only check this project if the solution is opened and we haven't already warned at the maximum level. Note that fAdded
            // is true for both add and a reload of an unloaded project
            if (_solutionOpened && fAdded == 1 && _compatibilityLevelWarnedForThisSolution != CompatibilityLevel.NotSupported)
            {
                UnconfiguredProject project = pHierarchy.AsUnconfiguredProject();
                if (project != null)
                {
                    _threadHandling.Value.JoinableTaskFactory.RunAsync(async () =>
                    {
                        // Run on the background
                        await TaskScheduler.Default;

                        VersionCompatibilityData compatData = GetVersionCmpatibilityData();

                        // We need to check if this project has been newly created. Our projects will implement IProjectCreationState -we can 
                        // skip any that don't
                        var projectCreationState = project.Services.ExportProvider.GetExportedValueOrDefault<IProjectCreationState>();
                        if (projectCreationState != null && !projectCreationState.WasNewlyCreated)
                        {
                            CompatibilityLevel compatLevel = await GetProjectCompatibilityAsync(project, compatData).ConfigureAwait(false);
                            if (compatLevel != CompatibilityLevel.Recommended)
                            {
                                await WarnUserOfIncompatibleProjectAsync(compatLevel, compatData).ConfigureAwait(false);
                            }
                        }
                    });
                }
            }

            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            // Clear state flags
            _compatibilityLevelWarnedForThisSolution = CompatibilityLevel.Recommended;
            _solutionOpened = false;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Fired when the solution load process is fully complete, including all background loading 
        /// of projects. This event always fires after the initial opening of a solution 
        /// </summary>
        public int OnAfterBackgroundSolutionLoadComplete()
        {
            // Schedule this to run on idle
            _threadHandling.Value.JoinableTaskFactory.RunAsync(async () =>
            {
                // Run on the background
                await TaskScheduler.Default;

                VersionCompatibilityData compatDataToUse = GetVersionCmpatibilityData();

                CompatibilityLevel finalCompatLevel = CompatibilityLevel.Recommended;
                IProjectService projectService = _projectServiceAccessor.Value.GetProjectService();
                IEnumerable<UnconfiguredProject> projects = projectService.LoadedUnconfiguredProjects;
                foreach (var project in projects)
                {
                    // Track the most severe compatibility level
                    CompatibilityLevel compatLevel = await GetProjectCompatibilityAsync(project, compatDataToUse).ConfigureAwait(false);
                    if (compatLevel != CompatibilityLevel.Recommended && compatLevel > finalCompatLevel)
                    {
                        finalCompatLevel = compatLevel;
                    }
                }

                if (finalCompatLevel != CompatibilityLevel.Recommended)
                {

                    // Warn the user.
                    await WarnUserOfIncompatibleProjectAsync(finalCompatLevel, compatDataToUse).ConfigureAwait(false);
                }

                // Used so we know when to process newly added projects
                _solutionOpened = true;
            });

            return VSConstants.S_OK;
        }

        private async Task WarnUserOfIncompatibleProjectAsync(CompatibilityLevel compatLevel, VersionCompatibilityData compatData)
        {
            // Warn the user.
            await _threadHandling.Value.SwitchToUIThread();

            // Check if already warned - this could happen in the off chance two projects are added very quickly since the detection work is 
            // scheduled on idle.
            if (_compatibilityLevelWarnedForThisSolution < compatLevel)
            {
                // Only want to warn once per solution
                _compatibilityLevelWarnedForThisSolution = compatLevel;

                IVsUIShell uiShell = _serviceProvider.GetService<IVsUIShell, SVsUIShell>();
                uiShell.GetAppName(out string caption);

                if (compatLevel == CompatibilityLevel.Supported)
                {
                    // Get current dontShowAgain value
                    var settingsManager = (ISettingsManager)_serviceProvider.GetService(typeof(SVsSettingsPersistenceManager));
                    bool suppressPrompt = false;
                    if (settingsManager != null)
                    {
                        suppressPrompt = settingsManager.GetValueOrDefault(SuppressDotNewCoreWarningKey, defaultValue: false);
                    }

                    if (!suppressPrompt)
                    {
                        var msg = string.Format(compatData.OpenSupportedMessage, compatData.SupportedVersion.Major, compatData.SupportedVersion.Minor);
                        suppressPrompt = _dialogServices.Value.DontShowAgainMessageBox(caption, msg, VSResources.DontShowAgain, false, VSResources.LearnMore, SupportedLearnMoreFwlink);
                        if (suppressPrompt && settingsManager != null)
                        {
                            await settingsManager.SetValueAsync(SuppressDotNewCoreWarningKey, suppressPrompt, isMachineLocal: true).ConfigureAwait(true);
                        }
                    }
                }
                else
                {
                    string msg;
                    if (compatData.UnsupportedVersion != null)
                    {
                        msg = string.Format(compatData.OpenUnsupportedMessage, compatData.UnsupportedVersion.Major, compatData.UnsupportedVersion.Minor);
                    }
                    else
                    {
                        msg = string.Format(compatData.OpenUnsupportedMessage, compatData.SupportedVersion.Major, compatData.SupportedVersion.Minor);
                    }

                    _dialogServices.Value.DontShowAgainMessageBox(caption, msg, null, false, VSResources.LearnMore, UnsupportedLearnMoreFwlink);
                }
            }
        }

        private async Task<CompatibilityLevel> GetProjectCompatibilityAsync(UnconfiguredProject project, VersionCompatibilityData compatData)
        {
            if (project.Capabilities.AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp))
            {
                IProjectProperties properties = project.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject.Services.ProjectPropertiesProvider.GetCommonProperties();
                string tfm = await properties.GetEvaluatedPropertyValueAsync("TargetFrameworkMoniker").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(tfm))
                {
                    var fw = new FrameworkName(tfm);
                    if (fw.Identifier.Equals(".NETCoreApp", StringComparison.OrdinalIgnoreCase))
                    {
                        return GetCompatibilityLevelFromVersion(fw.Version, compatData);
                    }
                    else if (fw.Identifier.Equals(".NETFramework", StringComparison.OrdinalIgnoreCase))
                    {
                        // The interesting case here is Asp.Net Core on full framework
                        IImmutableSet<IUnresolvedPackageReference> pkgReferences = await project.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject.Services.PackageReferences.GetUnresolvedReferencesAsync().ConfigureAwait(false);

                        // Look through the package references
                        foreach (var pkgRef in pkgReferences)
                        {
                            if (string.Equals(pkgRef.EvaluatedInclude, "Microsoft.AspNetCore.All", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(pkgRef.EvaluatedInclude, "Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase))
                            {
                                string verString = await pkgRef.Metadata.GetEvaluatedPropertyValueAsync("Version").ConfigureAwait(false);
                                if (!string.IsNullOrWhiteSpace(verString))
                                {
                                    // This is a semantic version string. We only care about the non-semantic version part
                                    int index = verString.IndexOfAny(new char[] { '-', '+' });
                                    if (index != -1)
                                    {
                                        verString = verString.Substring(0, index);
                                    }

                                    if (Version.TryParse(verString, out Version aspnetVersion))
                                    {
                                        return GetCompatibilityLevelFromVersion(aspnetVersion, compatData);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return CompatibilityLevel.Recommended;
        }

        /// <summary>
        /// Compares the passed in version to the compatibility data to determine the compat level
        /// </summary>
        private CompatibilityLevel GetCompatibilityLevelFromVersion(Version version, VersionCompatibilityData compatData)
        {
            // Omly compare major, minor. The presence of build with change the comparison. ie: 2.0 != 2.0.0
            if (version.Build != -1)
            {
                version = new Version(version.Major, version.Minor);
            }

            if (compatData.SupportedVersion != null)
            {
                if (version < compatData.SupportedVersion)
                {
                    return CompatibilityLevel.Recommended;
                }
                else if (version == compatData.SupportedVersion || (compatData.UnsupportedVersion != null && version < compatData.UnsupportedVersion))
                {
                    return CompatibilityLevel.Supported;
                }

                return CompatibilityLevel.NotSupported;
            }

            // Only has an unsupported version
            if (compatData.UnsupportedVersion != null)
            {
                if (version < compatData.UnsupportedVersion)
                {
                    return CompatibilityLevel.Recommended;
                }

                return CompatibilityLevel.NotSupported;
            }

            // No restrictions
            return CompatibilityLevel.Recommended;
        }

        /// <summary>
        /// Pings the server to download version compatibility information and stores this in a cached file in the users app data. If the cached file is
        /// less than 24 hours old, it uses that data. Otherwise it downloads from the server. If the download fails it will use the previously cached
        ///  file, or if that file doesn't not exist, it uses the data baked into this class
        /// </summary>
        private VersionCompatibilityData GetVersionCmpatibilityData()
        {
            // Do we need to update our cached data? Note that since the download could take a long time like tens of seconds we don't really want to
            // start showing messages to the user well after their project is opened and they are interacting with it. Thus we start a task to update the 
            // file, so that the next time we come here, we have updated data.
            if (_curVersionCompatibilityData != null && _timeCurVersionDataLastUpdatedUtc.AddHours(CacheFileValidHours).IsLaterThan(DateTime.UtcNow))
            {
                return _curVersionCompatibilityData;
            }

            try
            {
                // Try the cache file
                Dictionary<Version, VersionCompatibilityData> versionCompatData = GetCompabilityDataFromCacheFile();

                // See if the cache file needs refreshing and if so, kick off a task to do so
                if (_versionDataCacheFile != null && _versionDataCacheFile.CacheFileIsStale())
                {
                    Task noWait = _versionDataCacheFile.TryToUpdateCacheFileAsync(() =>
                    {
                        // Invalidate the in-memory cached data on success
                        _timeCurVersionDataLastUpdatedUtc = DateTime.MinValue;
                    });
                }

                if (versionCompatData != null)
                {
                    // First try to match exatly on our VS version and if that fails, match on just major, minor
                    if (versionCompatData.TryGetValue(_ourVSVersion, out VersionCompatibilityData compatData) || versionCompatData.TryGetValue(new Version(_ourVSVersion.Major, _ourVSVersion.Minor), out compatData))
                    {
                        // Now fix up missing data
                        if (string.IsNullOrEmpty(compatData.OpenSupportedMessage))
                        {
                            compatData.OpenSupportedMessage = VSResources.PartialSupportedDotNetCoreProject;
                        }

                        if (string.IsNullOrEmpty(compatData.OpenUnsupportedMessage))
                        {
                            compatData.OpenUnsupportedMessage = VSResources.NotSupportedDotNetCoreProject;
                        }

                        UpdateInMemoryCachedData(compatData);
                    }
                }
            }
            catch
            {

            }

            if (_curVersionCompatibilityData == null)
            {
                // Something failed or no remote file,  use the compatibility data we shipped with which does not have any warnings
                UpdateInMemoryCachedData(new VersionCompatibilityData()
                {
                    OpenSupportedMessage = VSResources.PartialSupportedDotNetCoreProject,
                    OpenUnsupportedMessage = VSResources.NotSupportedDotNetCoreProject
                });
            }

            return _curVersionCompatibilityData;
        }

        private void UpdateInMemoryCachedData(VersionCompatibilityData newData)
        {
            _curVersionCompatibilityData = newData;
            _timeCurVersionDataLastUpdatedUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// If the cached file exists reads the data and returns it
        /// </summary>
        private Dictionary<Version, VersionCompatibilityData> GetCompabilityDataFromCacheFile()
        {
            try
            {
                string data = _versionDataCacheFile?.ReadCacheFile();
                if (data != null)
                {
                    return VersionCompatibilityData.DeserializeVersionData(data);
                }
            }
            catch
            {

            }
            return null;
        }

        #region Unused

        /// <summary>
        /// Unused IVsSolutionEvents
        /// </summary>
        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Unused IVsSolutionLoadEvents
        /// </summary>
        public int OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeBackgroundSolutionLoadBegins()
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeOpenSolution(string pszSolutionFilename)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
        {
            pfShouldDelayLoadToNextIdle = false;
            return VSConstants.S_OK;
        }

        #endregion
    }
}
