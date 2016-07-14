// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Generators;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

// We register ourselves as a new CPS "project type", as well as setting ourselves as the prefered project type for the legacy VB project type.
[assembly: ProjectTypeRegistration(projectTypeGuid: VisualBasicProjectSystemPackage.ProjectTypeGuid, displayName: "#1", displayProjectFileExtensions: "#2", defaultProjectExtension: "vbproj", language: "VisualBasic", resourcePackageGuid: VisualBasicProjectSystemPackage.PackageGuid)]
[assembly: PreferedProjectFactoryRegistration(originalProjectTypeGuid: VisualBasicProjectSystemPackage.LegacyProjectTypeGuid, preferedProjectTypeGuid: VisualBasicProjectSystemPackage.ProjectTypeGuid)]

namespace Microsoft.VisualStudio.Packaging
{
    [Guid("D15F5C78-D04F-45FD-AEA2-D7982D8FA429")]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    [RemoteCodeGeneratorRegistration(SingleFileGenerators.ResXGuid, SingleFileGenerators.ResXGeneratorName,
        SingleFileGenerators.ResXDescription, ProjectTypeGuidFormatted, GeneratesDesignTimeSource = true)]
    [RemoteCodeGeneratorRegistration(SingleFileGenerators.PublicResXGuid, SingleFileGenerators.PublicResXGeneratorName,
        SingleFileGenerators.PublicResXDescription, ProjectTypeGuidFormatted, GeneratesDesignTimeSource = true)]
    [RemoteCodeGeneratorRegistration(SingleFileGenerators.VbResXGeneratorGuid, SingleFileGenerators.VbResXGeneratorName,
        SingleFileGenerators.VbResXGeneratorDescription, ProjectTypeGuidFormatted, GeneratesDesignTimeSource = true)]
    [RemoteCodeGeneratorRegistration(SingleFileGenerators.PublicVbResXGeneratorGuid, SingleFileGenerators.PublicVbResXGeneratorName,
        SingleFileGenerators.PublicVbResXGeneratorDescription, ProjectTypeGuidFormatted, GeneratesDesignTimeSource = true)]
    [RemoteCodeGeneratorRegistration(SingleFileGenerators.TextTemplatingFileGeneratorGuid, SingleFileGenerators.TextTemplatingFileGenerator,
        SingleFileGenerators.TextTemplatingFileGeneratorDescription, ProjectTypeGuidFormatted, GeneratesDesignTimeSource = true)]
    [RemoteCodeGeneratorRegistration(SingleFileGenerators.TextTemplatingFilePreprocessorGuid, SingleFileGenerators.TextTemplatingFilePreprocessor,
        SingleFileGenerators.TextTemplatingFilePreprocessorDescription, ProjectTypeGuidFormatted, GeneratesDesignTimeSource = true)]
    [GeneratorExtensionRegistration(SingleFileGenerators.TextTemplatingFileGeneratorExtension,
        SingleFileGenerators.TextTemplatingFileGenerator, ProjectTypeGuidFormatted)]
    internal class VisualBasicProjectSystemPackage : AsyncPackage
    {
        public const string ProjectTypeGuid = "9A19103F-16F7-4668-BE54-9A1E7A4F7556";
        private const string ProjectTypeGuidFormatted = "{" + ProjectTypeGuid + "}";
        public const string LegacyProjectTypeGuid = "F184B08F-C81C-45F6-A57F-5ABD9991F28F";
        public const string PackageGuid = "D15F5C78-D04F-45FD-AEA2-D7982D8FA429";

        public VisualBasicProjectSystemPackage()
        {
        }
    }
}
