// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    [Guid(BuildLoggingToolWindowGuidString)]
    public sealed class BuildLoggingToolWindow : ToolWindowPane, IOleCommandTarget
    {
        public const string BuildLoggingToolWindowGuidString = "391238ea-dad7-488c-94d1-e2b6b5172bf3";

        public const string BuildLoggingToolWindowCaption = "Build Logging";

        private readonly BuildLogger _buildLogger;

        public BuildLoggingToolWindow() : base(ProjectSystemToolsPackage.Instance)
        {
            Caption = BuildLoggingToolWindowCaption;

            ToolBar = new CommandID(ProjectSystemToolsPackage.UIGroupGuid, ProjectSystemToolsPackage.BuildLoggingToolbarMenuId);
            ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;

            _buildLogger = new BuildLogger(ProjectSystemToolsPackage.Instance);
            Content = new BuildLoggingToolWindowControl(_buildLogger);
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

        int IOleCommandTarget.Exec(ref Guid commandGroupGuid, uint commandID, uint commandExecutionOptions, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (commandGroupGuid != ProjectSystemToolsPackage.CommandSetGuid)
            {
                return (int)Constants.MSOCMDERR_E_UNKNOWNGROUP;
            }

            var handled = true;

            switch (commandID)
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
    }
}
