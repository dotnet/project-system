// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.VS;

// Visual Basic
[assembly: ProjectTypeRegistration(
    projectTypeGuid: ProjectType.VisualBasic,
    displayName: "#21",                      // "Visual Basic"
    displayProjectFileExtensions: "#22",     // "Visual Basic Project Files (*.vbproj);*.vbproj"
    defaultProjectExtension: "vbproj",
    language: "VisualBasic",
    resourcePackageGuid: ManagedProjectSystemPackage.PackageGuid,
    Capabilities = ProjectTypeCapabilities.VisualBasic,
    DisableAsynchronousProjectTreeLoad = true,
    PossibleProjectExtensions = "vbproj",
    NewProjectRequireNewFolderVsTemplate = true,
    SupportsCodespaces = true,
    SupportsSolutionChangeWithoutReload = true)]
[assembly: ProvideDiffSupportedContentType(".vbproj", "")]   // Empty string because content type is not important, we just want to tell the diff that the file type is supported
[assembly: ProvideEditorFactoryMapping("{f6819a78-a205-47b5-be1c-675b3c7f0b8e}", ".vbproj")] // Use the XML editor

// F#
[assembly: ProjectTypeRegistration(
    projectTypeGuid: ProjectType.FSharp,
    displayName: "#23",                      // "F#"
    displayProjectFileExtensions: "#24",     // "F# Project Files (*.fsproj);*.fsproj"
    defaultProjectExtension: "fsproj",
    language: "FSharp",
    resourcePackageGuid: ManagedProjectSystemPackage.PackageGuid,
    Capabilities = ProjectTypeCapabilities.FSharp,
    DisableAsynchronousProjectTreeLoad = true,
    PossibleProjectExtensions = "fsproj",
    NewProjectRequireNewFolderVsTemplate = true,
    SupportsCodespaces = true,
    SupportsSolutionChangeWithoutReload = true)]
[assembly: ProvideDiffSupportedContentType(".fsproj", "")]   // Empty string because content type is not important, we just want to tell the diff that the file type is supported
[assembly: ProvideEditorFactoryMapping("{f6819a78-a205-47b5-be1c-675b3c7f0b8e}", ".fsproj")] // Use the XML editor

// C#
[assembly: ProjectTypeRegistration(
    projectTypeGuid: ProjectType.CSharp,
    displayName: "#25",                      // "C#"
    displayProjectFileExtensions: "#26",     // "C# Project Files (*.csproj);*.csproj"
    defaultProjectExtension: "csproj",
    language: "CSharp",
    resourcePackageGuid: ManagedProjectSystemPackage.PackageGuid,
    Capabilities = ProjectTypeCapabilities.CSharp,
    DisableAsynchronousProjectTreeLoad = true,
    PossibleProjectExtensions = "csproj",
    NewProjectRequireNewFolderVsTemplate = true,
    SupportsCodespaces = true,
    SupportsSolutionChangeWithoutReload = true)]
[assembly: ProvideDiffSupportedContentType(".csproj", "")]   // Empty string because content type is not important, we just want to tell the diff that the file type is supported
[assembly: ProvideEditorFactoryMapping("{f6819a78-a205-47b5-be1c-675b3c7f0b8e}", ".csproj")] // Use the XML editor
