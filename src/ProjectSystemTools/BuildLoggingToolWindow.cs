using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.ProjectSystem.Tools
{
    [Guid(BuildLoggingToolWindowGuidString)]
    public class BuildLoggingToolWindow : ToolWindowPane
    {
        public const string BuildLoggingToolWindowGuidString = "391238ea-dad7-488c-94d1-e2b6b5172bf3";

        public const string BuildLoggingToolWindowCaption = "Build Logging";

        public BuildLoggingToolWindow() : base(null)
        {
            Caption = BuildLoggingToolWindowCaption;
            Content = new BuildLoggingToolWindowControl();
        }
    }
}
