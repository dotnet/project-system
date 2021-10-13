// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    /// <summary>
    /// Tracks implicitly builds triggered as part of F5/Shift+F5 debugging commands and
    /// skips analyzer execution for these builds by invoking into <see cref="IImplicitlyTriggeredBuildManager"/>.
    /// </summary>
    [Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class ImplicitlyTriggeredDebugBuildManager : OnceInitializedOnceDisposedAsync, IProjectDynamicLoadComponent, IVsUpdateSolutionEvents2, IVsUpdateSolutionEvents3
    {
        private readonly IProjectSystemOptionsWithChanges _options;
        private readonly IVsService<IVsSolutionBuildManager3> _solutionBuildManagerService;

#pragma warning disable CS0618 // Type or member is obsolete - IImplicitlyTriggeredBuildManager is marked obsolete as it may eventually be replaced with a different API.
        private readonly IImplicitlyTriggeredBuildManager _implicitlyTriggeredBuildManager;
#pragma warning restore CS0618 // Type or member is obsolete

        private IVsSolutionBuildManager3? _solutionBuildManager;
        private uint _cookie, _cookie3;
        private bool _skipAnalyzersForImplicitlyTriggeredBuild;

        [ImportingConstructor]
        public ImplicitlyTriggeredDebugBuildManager(
            IProjectThreadingService threadingService,
            IProjectSystemOptionsWithChanges options,
            IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager3> solutionBuildManagerService,
#pragma warning disable CS0618 // Type or member is obsolete - IImplicitlyTriggeredBuildManager is marked obsolete as it may eventually be replaced with a different API.
            IImplicitlyTriggeredBuildManager implicitlyTriggeredBuildManager)
#pragma warning restore CS0618 // Type or member is obsolete
            : base(threadingService.JoinableTaskContext)
        {
            _options = options;
            _solutionBuildManagerService = solutionBuildManagerService;
            _implicitlyTriggeredBuildManager = implicitlyTriggeredBuildManager;
        }

        public Task LoadAsync() => InitializeAsync();
        public Task UnloadAsync() => DisposeAsync();

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            // AdviseUpdateSolutionEvents call needs UI thread.
            await JoinableFactory.SwitchToMainThreadAsync(cancellationToken);

            _solutionBuildManager = await _solutionBuildManagerService.GetValueAsync(cancellationToken);
            ErrorHandler.ThrowOnFailure(((IVsSolutionBuildManager2)_solutionBuildManager).AdviseUpdateSolutionEvents(this, out _cookie));
            ErrorHandler.ThrowOnFailure(_solutionBuildManager.AdviseUpdateSolutionEvents3(this, out _cookie3));

            _skipAnalyzersForImplicitlyTriggeredBuild = await _options.GetSkipAnalyzersForImplicitlyTriggeredBuildAsync(cancellationToken);
            _options.RegisterOptionChangedEventHandler(OnOptionChangedAsync);
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                ((IVsSolutionBuildManager2)_solutionBuildManager!).UnadviseUpdateSolutionEvents(_cookie);
                _solutionBuildManager.UnadviseUpdateSolutionEvents3(_cookie3);

                _options.UnregisterOptionChangedEventHandler(OnOptionChangedAsync);
            }

            return Task.CompletedTask;
        }

        private async Task OnOptionChangedAsync(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == ProjectSystemOptions.SkipAnalyzersForImplicitlyTriggeredBuildSettingKey)
            {
                _skipAnalyzersForImplicitlyTriggeredBuild = await _options.GetSkipAnalyzersForImplicitlyTriggeredBuildAsync();
            }
        }

        private bool IsDebugBuildThatNeedsToSkipAnalyzers()
        {
            if (!_skipAnalyzersForImplicitlyTriggeredBuild)
            {
                return false;
            }

            ErrorHandler.ThrowOnFailure(_solutionBuildManager!.QueryBuildManagerBusyEx(out uint flags));
            var buildFlags = (VSSOLNBUILDUPDATEFLAGS)flags;
            return (buildFlags & (VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_LAUNCH | VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_LAUNCHDEBUG)) != 0;
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            if (IsDebugBuildThatNeedsToSkipAnalyzers())
            {
                _implicitlyTriggeredBuildManager.OnBuildStart();
            }

            return HResult.OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            if (IsDebugBuildThatNeedsToSkipAnalyzers())
            {
                _implicitlyTriggeredBuildManager.OnBuildEndOrCancel();
            }

            return HResult.OK;
        }

        public int UpdateSolution_Cancel()
        {
            if (IsDebugBuildThatNeedsToSkipAnalyzers())
            {
                _implicitlyTriggeredBuildManager.OnBuildEndOrCancel();
            }

            return HResult.OK;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return HResult.OK;
        }

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return HResult.OK;
        }

        public int UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            return HResult.OK;
        }

        public int UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            return HResult.OK;
        }

        public int OnBeforeActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            return HResult.OK;
        }

        public int OnAfterActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            return HResult.OK;
        }
    }
}
