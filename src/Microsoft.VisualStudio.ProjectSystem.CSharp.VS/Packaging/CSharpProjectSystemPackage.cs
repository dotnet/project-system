// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Generators;
using Microsoft.VisualStudio.ProjectSystem.VS.Xproj;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

// We register ourselves as a new CPS "project type"
[assembly: ProjectTypeRegistration(projectTypeGuid: CSharpProjectSystemPackage.ProjectTypeGuid, displayName: "#1", displayProjectFileExtensions: "#2", defaultProjectExtension: "csproj", language: "CSharp", resourcePackageGuid: CSharpProjectSystemPackage.PackageGuid)]

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    [ProvideProjectFactory(typeof(MigrateXprojProjectFactory), null, "#8", "xproj", "xproj", null)]
    [RemoteCodeGeneratorRegistration(SingleFileGenerators.ResXGuid, SingleFileGenerators.ResXGeneratorName,
        SingleFileGenerators.ResXDescription, ProjectTypeGuidFormatted, GeneratesDesignTimeSource = true)]
    [RemoteCodeGeneratorRegistration(SingleFileGenerators.PublicResXGuid, SingleFileGenerators.PublicResXGeneratorName,
        SingleFileGenerators.PublicResXDescription, ProjectTypeGuidFormatted, GeneratesDesignTimeSource = true)]
    [RemoteCodeGeneratorRegistration(SingleFileGenerators.TextTemplatingFileGeneratorGuid, SingleFileGenerators.TextTemplatingFileGenerator,
        SingleFileGenerators.TextTemplatingFileGeneratorDescription, ProjectTypeGuidFormatted, GeneratesDesignTimeSource = true)]
    [RemoteCodeGeneratorRegistration(SingleFileGenerators.TextTemplatingFilePreprocessorGuid, SingleFileGenerators.TextTemplatingFilePreprocessor,
        SingleFileGenerators.TextTemplatingFilePreprocessorDescription, ProjectTypeGuidFormatted, GeneratesDesignTimeSource = true)]
    [GeneratorExtensionRegistration(SingleFileGenerators.TextTemplatingFileGeneratorExtension,
        SingleFileGenerators.TextTemplatingFileGenerator, ProjectTypeGuidFormatted)]
    [ClassRegistration(DebugPropertyClassId, DebugPropertyClassInfo)]
    [DplOptOutRegistration(ProjectTypeGuid, true)]
    internal class CSharpProjectSystemPackage : AsyncPackage
    {
        public const string ProjectTypeGuid = "9A19103F-16F7-4668-BE54-9A1E7A4F7556";
        public const string LegacyProjectTypeGuid = "FAE04EC0-301F-11d3-BF4B-00C04F79EFBC";
        public const string XprojTypeGuid = "8bb2217d-0f2d-49d1-97bc-3654ed321f3b";
        public const string PackageGuid = "860A27C0-B665-47F3-BC12-637E16A1050A";
        private const string ProjectTypeGuidFormatted = "{" + ProjectTypeGuid + "}";

        private const string DebugPropertyClassId = "{0273C280-1882-4ED0-9308-52914672E3AA}";
        private const string DebugPropertyClassInfo = "Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages.DebugPropertyPage";

        private IVsProjectFactory _factory;

        public CSharpProjectSystemPackage()
        {
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _factory = new MigrateXprojProjectFactory(new ProcessRunner(), new Win32FileSystem());
            _factory.SetSite(this);
            RegisterProjectFactory(_factory);
        }
    }
}
