// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Input
{
    /// <summary>
    ///     Provides common well-known project-system command IDs.
    /// </summary>
    internal static class ManagedProjectSystemCommandId
    {
        public const long GenerateNuGetPackageProjectContextMenu = 0x2000;
        public const long GenerateNuGetPackageTopLevelBuild = 0x2001;
        public const long NavigateToProject = 0x2002;
        public const int DebugTargetMenuDebugFrameworkMenu = 0x3000;
        public const int DebugFrameworks = 0x3050;
    }
}
