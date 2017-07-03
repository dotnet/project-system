// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    [Guid(BuildLoggingToolWindowGuidString)]
    public sealed class BuildLoggingToolWindow : ToolWindowPane, IOleCommandTarget, IVsUpdateSolutionEvents4
    {
        public const string BuildLoggingToolWindowGuidString = "391238ea-dad7-488c-94d1-e2b6b5172bf3";

        public const string BuildLoggingToolWindowCaption = "Build Logging";

        private readonly IBuildManager _buildLogger;
        private IVsSolutionBuildManager5 _updateSolutionEventsService;
        private readonly uint _updateSolutionEventsCookie;

        public BuildLoggingToolWindow() : base(ProjectSystemToolsPackage.Instance)
        {
            Caption = BuildLoggingToolWindowCaption;

            ToolBar = new CommandID(ProjectSystemToolsPackage.UIGroupGuid, ProjectSystemToolsPackage.BuildLoggingToolbarMenuId);
            ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;

            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            _buildLogger = componentModel.GetService<IBuildManager>();

            _updateSolutionEventsService = ((System.IServiceProvider)ProjectSystemToolsPackage.Instance).GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager5;
            _updateSolutionEventsService?.AdviseUpdateSolutionEvents4(this, out _updateSolutionEventsCookie);

            Content = new ToolWindowControl(_buildLogger);
        }

        int IOleCommandTarget.QueryStatus(ref Guid commandGroupGuid, uint commandCount, OLECMD[] commands, IntPtr commandText)
        {
            if (commandGroupGuid != ProjectSystemToolsPackage.CommandSetGuid)
            {
                return (int)Constants.MSOCMDERR_E_UNKNOWNGROUP;
            }

            if (commandCount != 1)
            {
                return (int)Constants.OLECMDERR_E_NOTSUPPORTED;
            }

            var cmd = commands[0];

            var handled = true;
            var enabled = false;
            var visible = false;
            var latched = false;

            switch (cmd.cmdID)
            {
                case ProjectSystemToolsPackage.StartLoggingCommandId:
                    visible = true;
                    enabled = !_buildLogger.IsLogging;
                    break;

                case ProjectSystemToolsPackage.StopLoggingCommandId:
                    visible = true;
                    enabled = _buildLogger.IsLogging;
                    break;

                default:
                    handled = false;
                    break;
            }

            if (handled)
            {
                commands[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;

                if (enabled)
                {
                    commands[0].cmdf |= (uint)OLECMDF.OLECMDF_ENABLED;
                }

                if (latched)
                {
                    commands[0].cmdf |= (uint)OLECMDF.OLECMDF_LATCHED;
                }

                if (!visible)
                {
                    commands[0].cmdf |= (uint)OLECMDF.OLECMDF_INVISIBLE;
                }
            }

            return handled ? VSConstants.S_OK : (int)Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        int IOleCommandTarget.Exec(ref Guid commandGroupGuid, uint commandId, uint commandExecutionOptions, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (commandGroupGuid != ProjectSystemToolsPackage.CommandSetGuid)
            {
                return (int)Constants.MSOCMDERR_E_UNKNOWNGROUP;
            }

            var handled = true;

            switch (commandId)
            {
                case ProjectSystemToolsPackage.StartLoggingCommandId:
                    _buildLogger.Start();
                    break;

                case ProjectSystemToolsPackage.StopLoggingCommandId:
                    _buildLogger.Stop();
                    break;

                default:
                    handled = false;
                    break;
            }

            return handled ? VSConstants.S_OK : (int)Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        protected override void Dispose(bool disposing)
        {
            if (_updateSolutionEventsService == null)
            {
                return;
            }

            _updateSolutionEventsService.UnadviseUpdateSolutionEvents4(_updateSolutionEventsCookie);
            _updateSolutionEventsService = null;
        }

        private static BuildOperation ActionToOperation(uint dwAction)
        {
            var action = (VSSOLNBUILDUPDATEFLAGS)dwAction;

            switch (action & VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_MASK)
            {
                case VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_CLEAN:
                    return BuildOperation.Clean;
                case VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD:
                    return BuildOperation.Build;
                case VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD | VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_FORCE_UPDATE:
                    return BuildOperation.Rebuild;
                case VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_DEPLOY:
                    return BuildOperation.Deploy;
            }

            var action2 = (VSSOLNBUILDUPDATEFLAGS2)dwAction;

            switch (action2 & (VSSOLNBUILDUPDATEFLAGS2)VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_MASK)
            {
                case VSSOLNBUILDUPDATEFLAGS2.SBF_OPERATION_PUBLISH:
                    return BuildOperation.Publish;
                case VSSOLNBUILDUPDATEFLAGS2.SBF_OPERATION_PUBLISHUI:
                    return BuildOperation.PublishUI;
                default:
                    return BuildOperation.Unknown;
            }
        }

        void IVsUpdateSolutionEvents4.UpdateSolution_QueryDelayFirstUpdateAction(out int pfDelay) => pfDelay = 0;

        void IVsUpdateSolutionEvents4.UpdateSolution_BeginFirstUpdateAction()
        {
        }

        void IVsUpdateSolutionEvents4.UpdateSolution_EndLastUpdateAction()
        {
        }

        void IVsUpdateSolutionEvents4.UpdateSolution_BeginUpdateAction(uint dwAction) => _buildLogger.NotifyBuildOperationStarted(ActionToOperation(dwAction));

        void IVsUpdateSolutionEvents4.UpdateSolution_EndUpdateAction(uint dwAction) => _buildLogger.NotifyBuildOperationEnded(ActionToOperation(dwAction));

        void IVsUpdateSolutionEvents4.OnActiveProjectCfgChangeBatchBegin()
        {
        }

        void IVsUpdateSolutionEvents4.OnActiveProjectCfgChangeBatchEnd()
        {
        }
    }
}
