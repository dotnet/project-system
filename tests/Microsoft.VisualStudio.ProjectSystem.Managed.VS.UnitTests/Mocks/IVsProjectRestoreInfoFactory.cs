// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.PackageRestore;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal static class ProjectRestoreInfoFactory
    {
        public static ProjectRestoreInfo Create()
        {
            return new ProjectRestoreInfo(string.Empty, string.Empty, string.Empty,
                                          RestoreBuilder.EmptyTargetFrameworks,
                                          RestoreBuilder.EmptyReferences);
        }
    }
}
