// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

// Visual Basic
[assembly: ProjectTypeRegistration(
    projectTypeGuid: ProjectType.VisualBasic,
    displayName: "#1",                      // "Visual Basic"
    displayProjectFileExtensions: "#2",     // "Visual Basic Project Files (*.vbproj);*.vbproj"
    defaultProjectExtension: "vbproj",
    language: "VisualBasic",
    resourcePackageGuid: ManagedProjectSystemPackage.PackageGuid,
    Capabilities = ManagedProjectSystemPackage.DefaultCapabilities + "; " + ProjectCapability.VisualBasic,
    DisableAsynchronousProjectTreeLoad = true)]
