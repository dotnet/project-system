// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell;

// Register ourselves as a CPS project type
[assembly: ProjectTypeRegistration(
    projectTypeGuid: ProjectType.VisualBasic,
    displayName: "#1",                      // "Visual Basic"
    displayProjectFileExtensions: "#2",     // "Visual Basic Project Files (*.vbproj);*.vbproj"
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
        public const string PackageGuid = "D15F5C78-D04F-45FD-AEA2-D7982D8FA429";

        public VisualBasicProjectSystemPackage()
        {
        }
    }
}
