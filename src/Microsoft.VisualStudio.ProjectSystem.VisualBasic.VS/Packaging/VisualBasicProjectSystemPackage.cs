// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell;

// Register ourselves as a CPS project type
[assembly: ProjectTypeRegistration(
    projectTypeGuid: VisualBasicProjectSystemPackage.ProjectTypeGuid,
    displayName: "#1",
    displayProjectFileExtensions: "#2",
    defaultProjectExtension: "vbproj",
    language: "VisualBasic",
    resourcePackageGuid: VisualBasicProjectSystemPackage.PackageGuid,
    Capabilities = ManagedProjectSystemPackage.DefaultCapabilities + "; " + ProjectCapability.VisualBasic,
    DisableAsynchronousProjectTreeLoad = true)]

namespace Microsoft.VisualStudio.Packaging
{
    [Guid("D15F5C78-D04F-45FD-AEA2-D7982D8FA429")]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    internal class VisualBasicProjectSystemPackage : AsyncPackage
    {
        public const string ProjectTypeGuid = "778DAE3C-4631-46EA-AA77-85C1314464D9";
        public const string LegacyProjectTypeGuid = "F184B08F-C81C-45F6-A57F-5ABD9991F28F";
        public const string PackageGuid = "D15F5C78-D04F-45FD-AEA2-D7982D8FA429";

        public VisualBasicProjectSystemPackage()
        {
        }
    }
}
