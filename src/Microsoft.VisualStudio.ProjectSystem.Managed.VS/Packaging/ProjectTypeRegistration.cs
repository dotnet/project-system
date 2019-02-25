// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

// Visual Basic
[assembly: ProjectTypeRegistration(
    projectTypeGuid: ProjectType.VisualBasic,
    displayName: "#21",                      // "Visual Basic"
    displayProjectFileExtensions: "#22",     // "Visual Basic Project Files (*.vbproj);*.vbproj"
    defaultProjectExtension: "vbproj",
    language: "VisualBasic",
    resourcePackageGuid: ManagedProjectSystemPackage.PackageGuid,
    Capabilities = ManagedProjectSystemPackage.DefaultCapabilities + "; " + ProjectCapability.VisualBasic,
    DisableAsynchronousProjectTreeLoad = true)]

// F#
[assembly: ProjectTypeRegistration(
    projectTypeGuid: ProjectType.FSharp,
    displayName: "#23",                      // "F#"
    displayProjectFileExtensions: "#24",     // "F# Project Files (*.fsproj);*.fsproj"
    defaultProjectExtension: "fsproj",
    language: "FSharp",
    resourcePackageGuid: ManagedProjectSystemPackage.PackageGuid,
    Capabilities = ManagedProjectSystemPackage.DefaultCapabilities + "; " + ProjectCapability.FSharp + "; " + ProjectCapability.SortByDisplayOrder,
    DisableAsynchronousProjectTreeLoad = true)]

// C#
[assembly: ProjectTypeRegistration(
    projectTypeGuid: ProjectType.CSharp,
    displayName: "#25",                      // "C#"
    displayProjectFileExtensions: "#26",     // "C# Project Files (*.csproj);*.csproj"
    defaultProjectExtension: "csproj",
    language: "CSharp",
    resourcePackageGuid: ManagedProjectSystemPackage.PackageGuid,
    Capabilities = ManagedProjectSystemPackage.DefaultCapabilities + "; " + ProjectCapability.CSharp,
    DisableAsynchronousProjectTreeLoad = true)]
