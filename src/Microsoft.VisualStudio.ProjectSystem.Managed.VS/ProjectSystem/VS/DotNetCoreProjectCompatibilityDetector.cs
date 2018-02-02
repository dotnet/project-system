// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
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
        // The version's below this are compatible, versions above it are unsupported (stronger warning message and no don't show again option), equal
        // to are "partial" so tooling should generally work, new features may not have tooling.
        static Version s_partialSupportedVersion = new Version(2, 1);

        const string LearnMoreFwlink = "https://go.microsoft.com/fwlink/?linkid=867264";
        private const string SuppressDotNewCoreWarningKey = @"ManagedProjectSystem\SuppressDotNewCoreWarning";

        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { }

        enum CompatibilityLevel
        {
            Supported = 0,
            Partial = 1,
            NotSupported =2
        }
        [ImportingConstructor]
        public DotNetCoreProjectCompatibilityDetector([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, Lazy<IProjectServiceAccessor> projectAccessor, 
                                                      Lazy<IDialogServices> dialogServices, Lazy<IProjectThreadingService> threadHandling)
        {
            _serviceProvider = serviceProvider;
            ProjectServiceAccessor = projectAccessor;
            DialogServices = dialogServices;
            ThreadHandling = threadHandling;
        }

        public async Task InitializeAsync()
        {
            await ThreadHandling.Value.SwitchToUIThread();

            // Do nothing, don't hook into events if not the release channel
            if(IsReleaseChannel())
            {
                var solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
                Verify.HResult(solution.AdviseSolutionEvents(this, out _solutionCookie));
            }
        }

        private Lazy<IProjectServiceAccessor> ProjectServiceAccessor { get; set; }
        private Lazy<IDialogServices> DialogServices { get; set; }
        private Lazy<IProjectThreadingService> ThreadHandling { get; set; }

        private readonly IServiceProvider _serviceProvider;
        private uint _solutionCookie = VSConstants.VSCOOKIE_NIL;
        private bool _solutionOpened;
        private CompatibilityLevel _compatibilityLevelWarnedForThisSolution = CompatibilityLevel.Supported;

        public void Dispose()
        {
            ThreadHandling.Value.VerifyOnUIThread();

            if(_solutionCookie != VSConstants.VSCOOKIE_NIL)
            {
                var solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
                if(solution != null)
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
            if(_solutionOpened && fAdded == 1 && _compatibilityLevelWarnedForThisSolution != CompatibilityLevel.NotSupported)
            {
                UnconfiguredProject project = pHierarchy.AsUnconfiguredProject();
                if (project != null)
                {
                    ThreadHandling.Value.JoinableTaskFactory.StartOnIdle(async () =>
                    {
                        // Run on the background
                        await TaskScheduler.Default;

                        // We need to check if this project has been newly created. Our projects will implement IProjectCreationState -we can 
                        // skip any that don't
                        var projectCreationState = project.Services.ExportProvider.GetExportedValueOrDefault<IProjectCreationState>();
                        if(projectCreationState != null && !projectCreationState.WasNewlyCreated)
                        {
                            CompatibilityLevel compatLevel = await GetProjectCompatibilityAsync(project).ConfigureAwait(false);
                            if(compatLevel != CompatibilityLevel.Supported)
                            {
                                await WarnUserOfiIncompatibleProjectAsync(compatLevel).ConfigureAwait(false);
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
            ThreadHandling.Value.JoinableTaskFactory.StartOnIdle(async () =>
            {
                // Run on the background
                await TaskScheduler.Default;

                CompatibilityLevel finalCompatLevel = CompatibilityLevel.Supported;
                IProjectService projectService = ProjectServiceAccessor.Value.GetProjectService();
                IEnumerable<UnconfiguredProject> projects = projectService.LoadedUnconfiguredProjects;
                foreach (var project in projects)
                {
                    // Track the most severe compatibility level
                    CompatibilityLevel compatLevel = await GetProjectCompatibilityAsync(project).ConfigureAwait(false);
                    if(compatLevel != CompatibilityLevel.Supported && compatLevel > finalCompatLevel)
                    {
                        finalCompatLevel = compatLevel;
                    }
                }
            
                if (finalCompatLevel != CompatibilityLevel.Supported)
                {
                    // Warn the user.
                    await WarnUserOfiIncompatibleProjectAsync(finalCompatLevel).ConfigureAwait(false);
               }

               // Used to know when to process newly added projects
               _solutionOpened = true;
            });

            return VSConstants.S_OK;
        }

        private async Task WarnUserOfiIncompatibleProjectAsync(CompatibilityLevel compatLevel)
        {
            // Warn the user.
            await ThreadHandling.Value.SwitchToUIThread();

            // Check if already warned - this could happen in the off chance two projects are added very quickly since the detection work is 
            // scheduled on idle.
            if(_compatibilityLevelWarnedForThisSolution < compatLevel)
            {
                // Only want to warn once per solution
                _compatibilityLevelWarnedForThisSolution = compatLevel;
                
                IVsUIShell uiShell = _serviceProvider.GetService<IVsUIShell, SVsUIShell>();
                uiShell.GetAppName(out string caption);

                if(compatLevel == CompatibilityLevel.Partial)
                {
                    // Get current dontShowAgain value
                    var settingsManager = (ISettingsManager)_serviceProvider.GetService(typeof(SVsSettingsPersistenceManager));
                    bool suppressPrompt = false;
                    if( settingsManager != null)
                    {
                        suppressPrompt = settingsManager.GetValueOrDefault(SuppressDotNewCoreWarningKey, defaultValue:false);
                    }
                    
                    if(!suppressPrompt)
                    {
                        suppressPrompt= DialogServices.Value.DontShowAgainMessageBox(caption, VSResources.PartialSupportedDotNetCoreProject, VSResources.DontShowAgain, false, VSResources.LearnMore, LearnMoreFwlink);
                        if(suppressPrompt && settingsManager != null)
                        {
                            await settingsManager.SetValueAsync(SuppressDotNewCoreWarningKey, true, isMachineLocal: true).ConfigureAwait(true);
                        }
                    }
                }
                else
                {
                    DialogServices.Value.DontShowAgainMessageBox(caption, string.Format(VSResources.NotSupportedDotNetCoreProject, s_partialSupportedVersion.Major, s_partialSupportedVersion.Minor), 
                                                                 null, false, VSResources.LearnMore, LearnMoreFwlink);
                }
            }
        }

        private async Task<CompatibilityLevel> GetProjectCompatibilityAsync(UnconfiguredProject project)
        {
            if (project.Capabilities.AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp))
            {
                IProjectProperties properties = project.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject.Services.ProjectPropertiesProvider.GetCommonProperties();
                string tfm = await properties.GetEvaluatedPropertyValueAsync("TargetFrameworkMoniker").ConfigureAwait(false);
                var fw = new FrameworkName(tfm);
                if ((fw.Identifier.Equals(".NETCore", StringComparison.OrdinalIgnoreCase) || fw.Identifier.Equals(".NETCoreApp", StringComparison.OrdinalIgnoreCase)))
                {
                    if(fw.Version.Major == s_partialSupportedVersion.Major && fw.Version.Minor == s_partialSupportedVersion.Minor)
                    {
                        return CompatibilityLevel.Partial;
                    }
                    else if(fw.Version.Major > s_partialSupportedVersion.Major || (fw.Version.Major == s_partialSupportedVersion.Major && fw.Version.Minor > s_partialSupportedVersion.Minor))
                    {
                        return CompatibilityLevel.NotSupported;
                    }
                }
            }

            return CompatibilityLevel.Supported;
        }

        /// <summary>
        /// Query the ChannelSuffix property. Will be empty if this is the release channel
        /// </summary>
        private bool IsReleaseChannel()
        {
            IVsAppId vsAppId = _serviceProvider.GetService<IVsAppId, SVsAppId>();
            int hr = vsAppId.GetProperty((int)VSAPropID.VSAPROPID_ChannelSuffix, out object value);
            return ErrorHandler.Succeeded(hr) && value is string && string.IsNullOrEmpty((string)value);
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
