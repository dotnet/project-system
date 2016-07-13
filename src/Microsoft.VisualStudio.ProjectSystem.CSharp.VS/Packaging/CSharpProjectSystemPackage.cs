// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Generators;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

// We register ourselves as a new CPS "project type", as well as setting ourselves as the prefered project type for the legacy C# project type.
[assembly: ProjectTypeRegistration(projectTypeGuid: CSharpProjectSystemPackage.ProjectTypeGuid, displayName: "#1", displayProjectFileExtensions: "#2", defaultProjectExtension: "csproj", language: "CSharp", resourcePackageGuid: CSharpProjectSystemPackage.PackageGuid)]
[assembly: PreferedProjectFactoryRegistration(originalProjectTypeGuid: CSharpProjectSystemPackage.LegacyProjectTypeGuid, preferedProjectTypeGuid: CSharpProjectSystemPackage.ProjectTypeGuid)]

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    [RemoteCodeGeneratorRegistration(SingleFileGenerators.ResXGuid, SingleFileGenerators.ResXGeneratorName, @"{" + ProjectTypeGuid + @"}", GeneratesDesignTimeSource = true)]
    [RemoteCodeGeneratorRegistration(SingleFileGenerators.PublicResXGuid, SingleFileGenerators.PublicResXGeneratorName, @"{" + ProjectTypeGuid + @"}", GeneratesDesignTimeSource = true)]
    [RemoteCodeGeneratorRegistration(SingleFileGenerators.TextTemplatingFileGeneratorGuid, SingleFileGenerators.TextTemplatingFileGenerator,
        ProjectTypeGuid, GeneratesDesignTimeSource = true)]
    [RemoteCodeGeneratorRegistration(SingleFileGenerators.TextTemplatingFilePreprocessorGuid, SingleFileGenerators.TextTemplatingFilePreprocessor,
        ProjectTypeGuid, GeneratesDesignTimeSource = true)]
    [GeneratorExtensionRegistration(SingleFileGenerators.TextTemplatingFileGeneratorExtension,
        SingleFileGenerators.TextTemplatingFileGenerator, ProjectTypeGuid)]
    internal class CSharpProjectSystemPackage : AsyncPackage
    {
        public const string ProjectTypeGuid = "9A19103F-16F7-4668-BE54-9A1E7A4F7556";
        public const string LegacyProjectTypeGuid = "FAE04EC0-301F-11d3-BF4B-00C04F79EFBC";
        public const string PackageGuid = "860A27C0-B665-47F3-BC12-637E16A1050A";

        public CSharpProjectSystemPackage()
        {
        }
    }
}
