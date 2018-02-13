// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.References;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
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
        // The version's above this are not supported
        static Version s_lastSupportedVersion = new Version(2, 0); 

        public const string NotSupportedLearnMoreFwlink = "https://go.microsoft.com/fwlink/?linkid=866848";

        private enum CompatibilityLevel
        {
            Supported = 0,
            NotSupported = 1
        }

        [ImportingConstructor]
        public DotNetCoreProjectCompatibilityDetector([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, Lazy<IProjectServiceAccessor> projectAccessor,
                                                      Lazy<IDialogServices> dialogServices, Lazy<IProjectThreadingService> threadHandling, Lazy<IVsShellUtilitiesHelper> vsShellUtilitiesHelper,
                                                      Lazy<IFileSystem> fileSystem)
        {
            _serviceProvider = serviceProvider;
            _projectServiceAccessor = projectAccessor;
            _dialogServices = dialogServices;
            _threadHandling = threadHandling;
            _shellUtilitiesHelper = vsShellUtilitiesHelper;
            _fileSystem = fileSystem;
        }

        private readonly IServiceProvider _serviceProvider;
        private readonly Lazy<IProjectServiceAccessor> _projectServiceAccessor;
        private readonly Lazy<IDialogServices> _dialogServices;
        private readonly Lazy<IProjectThreadingService> _threadHandling;
        private readonly Lazy<IVsShellUtilitiesHelper> _shellUtilitiesHelper;
        private readonly Lazy<IFileSystem> _fileSystem;

        private uint _solutionCookie = VSConstants.VSCOOKIE_NIL;
        private bool _solutionOpened;
        private CompatibilityLevel _compatibilityLevelWarnedForThisSolution = CompatibilityLevel.Supported;

        public async Task InitializeAsync()
        {
            await _threadHandling.Value.SwitchToUIThread();

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

                        CompatibilityLevel compatLevel = await GetProjectCompatibilityAsync(project).ConfigureAwait(false);
                        if (compatLevel != CompatibilityLevel.Supported)
                        {
                            await WarnUserOfIncompatibleProjectAsync(compatLevel).ConfigureAwait(false);
                        }
                    });
                }
            }

            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            // Clear state flags
            _compatibilityLevelWarnedForThisSolution = CompatibilityLevel.Supported;
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

                CompatibilityLevel finalCompatLevel = CompatibilityLevel.Supported;
                IProjectService projectService = _projectServiceAccessor.Value.GetProjectService();
                IEnumerable<UnconfiguredProject> projects = projectService.LoadedUnconfiguredProjects;
                foreach (var project in projects)
                {
                    // Track the most severe compatibility level
                    CompatibilityLevel compatLevel = await GetProjectCompatibilityAsync(project).ConfigureAwait(false);
                    if (compatLevel != CompatibilityLevel.Supported && compatLevel > finalCompatLevel)
                    {
                        finalCompatLevel = compatLevel;
                    }
                }

                if (finalCompatLevel != CompatibilityLevel.Supported)
                {

                    // Warn the user.
                    await WarnUserOfIncompatibleProjectAsync(finalCompatLevel).ConfigureAwait(false);
                }

                // Used so we know when to process newly added projects
                _solutionOpened = true;
            });

            return VSConstants.S_OK;
        }

        private async Task WarnUserOfIncompatibleProjectAsync(CompatibilityLevel compatLevel)
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

                string msg = string.Format(VSResources.NotSupportedDotNetCoreProject, s_lastSupportedVersion.Major, s_lastSupportedVersion.Minor);
                _dialogServices.Value.DontShowAgainMessageBox(caption, msg, null, false, VSResources.LearnMore, NotSupportedLearnMoreFwlink);
            }
        }

        private async Task<CompatibilityLevel> GetProjectCompatibilityAsync(UnconfiguredProject project)
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
                        return GetCompatibilityLevelFromVersion(fw.Version);
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
                                        return GetCompatibilityLevelFromVersion(aspnetVersion);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return CompatibilityLevel.Supported;
        }

        /// <summary>
        /// Compares the passed in version to our known last supported version to determine the 
        /// compatibility level
        /// </summary>
        private CompatibilityLevel GetCompatibilityLevelFromVersion(Version version)
        {
            // Only compare major, minor. The presence of build with change the comparison. ie: 2.0 != 2.0.0
            if (version.Build != -1)
            {
                version = new Version(version.Major, version.Minor);
            }

            return version <= s_lastSupportedVersion? CompatibilityLevel.Supported : CompatibilityLevel.NotSupported;
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
