// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer
{
    [Guid(BuildLogExplorerToolWindowGuidString)]
    public sealed class BuildLogExplorerToolWindow : ToolWindowPane
    {
        public const string BuildLogExplorerToolWindowGuidString = "3A3D8BAD-16D8-4C83-9F0E-CA55521E0E6B";

        public const string BuildLogExplorerToolWindowCaption = "Build Log Explorer";

        public BuildLogExplorerToolWindow() : base(ProjectSystemToolsPackage.Instance)
        {
            Caption = BuildLogExplorerToolWindowCaption;

            Content = new TextBox();
        }
    }
}
