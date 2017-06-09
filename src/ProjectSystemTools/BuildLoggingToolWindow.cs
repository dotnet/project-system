using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Constants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace Microsoft.VisualStudio.ProjectSystem.Tools
{
    [Guid(BuildLoggingToolWindowGuidString)]
    public class BuildLoggingToolWindow : ToolWindowPane, IOleCommandTarget
    {
        public const string BuildLoggingToolWindowGuidString = "391238ea-dad7-488c-94d1-e2b6b5172bf3";

        public const string BuildLoggingToolWindowCaption = "Build Logging";

        private bool _logging = false;

        public BuildLoggingToolWindow() : base(null)
        {
            Caption = BuildLoggingToolWindowCaption;

            ToolBar = new CommandID(ProjectSystemToolsPackage.UIGroupGuid, ProjectSystemToolsPackage.BuildLoggingToolbarMenuId);
            ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;

            Content = new BuildLoggingToolWindowControl();
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

            bool handled = true;
            bool enabled = false;
            bool visible = false;
            bool latched = false;

            switch (cmd.cmdID)
            {
                case ProjectSystemToolsPackage.StartLoggingCommandId:
                    visible = true;
                    enabled = !_logging;
                    break;

                case ProjectSystemToolsPackage.StopLoggingCommandId:
                    visible = true;
                    enabled = _logging;
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

            bool handled = true;

            switch (commandID)
            {
                case ProjectSystemToolsPackage.StartLoggingCommandId:
                    _logging = true;
                    break;

                case ProjectSystemToolsPackage.StopLoggingCommandId:
                    _logging = false;
                    break;

                default:
                    handled = false;
                    break;
            }

            return handled ? VSConstants.S_OK : (int)Constants.OLECMDERR_E_NOTSUPPORTED;
        }
    }
}
