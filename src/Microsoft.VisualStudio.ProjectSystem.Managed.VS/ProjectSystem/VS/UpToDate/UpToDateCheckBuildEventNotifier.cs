// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.UpToDate;
using Microsoft.VisualStudio.ProjectSystem.VS.Build;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UpToDate
{
    /// <summary>
    /// Listens for build events and notifies the fast up-to-date check of them
    /// via <see cref="IBuildUpToDateCheckProviderInternal"/>.
    /// </summary>
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
    internal sealed class UpToDateCheckBuildEventNotifier : OnceInitializedOnceDisposedAsync, IVsUpdateSolutionEvents2, IProjectDynamicLoadComponent
    {
        private readonly ISolutionBuildManager _solutionBuildManager;

        private IAsyncDisposable? _solutionBuildEventsSubscription;

        [ImportingConstructor]
        public UpToDateCheckBuildEventNotifier(
            JoinableTaskContext joinableTaskContext,
            ISolutionBuildManager solutionBuildManager)
            : base(new(joinableTaskContext))
        {
            _solutionBuildManager = solutionBuildManager;
        }

        public Task LoadAsync() => InitializeAsync();
        public Task UnloadAsync() => DisposeAsync();

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            _solutionBuildEventsSubscription = await _solutionBuildManager.SubscribeSolutionEventsAsync(this);
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                if (_solutionBuildEventsSubscription is not null)
                {
                    return _solutionBuildEventsSubscription.DisposeAsync().AsTask();
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Called right before a project configuration starts building.
        /// </summary>
        int IVsUpdateSolutionEvents2.UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            if (IsBuild(dwAction))
            {
                foreach (IBuildUpToDateCheckProviderInternal provider in FindActiveConfiguredProviders(pHierProj))
                {
                    provider.NotifyBuildStarting(DateTime.UtcNow);
                }
            }

            return HResult.OK;
        }

        /// <summary>
        /// Called right after a project configuration is finished building.
        /// </summary>
        int IVsUpdateSolutionEvents2.UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            if (fCancel == 0 && IsBuild(dwAction))
            {
                foreach (IBuildUpToDateCheckProviderInternal provider in FindActiveConfiguredProviders(pHierProj))
                {
                    provider.NotifyBuildCompleted(wasSuccessful: fSuccess != 0);
                }
            }

            return HResult.OK;
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="options"/> indicates either a build or rebuild.
        /// </summary>
        private static bool IsBuild(uint options)
        {
            const VSSOLNBUILDUPDATEFLAGS flags = VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD;

            var operation = (VSSOLNBUILDUPDATEFLAGS)options;

            if ((operation & flags) == flags)
            {
                return true;
            }

            return false;
        }

        private static IEnumerable<IBuildUpToDateCheckProviderInternal> FindActiveConfiguredProviders(IVsHierarchy vsHierarchy)
        {
            UnconfiguredProject? unconfiguredProject = vsHierarchy.AsUnconfiguredProject();

            if (unconfiguredProject is not null)
            {
                IActiveConfiguredProjectProvider activeConfiguredProjectProvider = unconfiguredProject.Services.ExportProvider.GetExportedValue<IActiveConfiguredProjectProvider>();

                ConfiguredProject? configuredProject = activeConfiguredProjectProvider.ActiveConfiguredProject;

                if (configuredProject is not null)
                {
                    return configuredProject.Services.ExportProvider.GetExportedValues<IBuildUpToDateCheckProviderInternal>();
                }
            }

            return Enumerable.Empty<IBuildUpToDateCheckProviderInternal>();
        }

        #region IVsUpdateSolutionEvents stubs

        int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate) => HResult.OK;
        int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate) => HResult.OK;
        int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand) => HResult.OK;
        int IVsUpdateSolutionEvents.UpdateSolution_Cancel() => HResult.OK;
        int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => HResult.OK;

        int IVsUpdateSolutionEvents2.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => HResult.OK;
        int IVsUpdateSolutionEvents2.UpdateSolution_Begin(ref int pfCancelUpdate) => HResult.OK;
        int IVsUpdateSolutionEvents2.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand) => HResult.OK;
        int IVsUpdateSolutionEvents2.UpdateSolution_StartUpdate(ref int pfCancelUpdate) => HResult.OK;
        int IVsUpdateSolutionEvents2.UpdateSolution_Cancel() => HResult.OK;

        #endregion
    }
}
