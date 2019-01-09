// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Xproj;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Task = System.Threading.Tasks.Task;

// Register ourselves as a CPS project type
[assembly: ProjectTypeRegistration(
    projectTypeGuid: CSharpProjectSystemPackage.ProjectTypeGuid,
    displayName: "#1",
    displayProjectFileExtensions: "#2",
    defaultProjectExtension: "csproj",
    language: "CSharp",
    resourcePackageGuid: CSharpProjectSystemPackage.PackageGuid,
    Capabilities = ManagedProjectSystemPackage.DefaultCapabilities + "; " + ProjectCapability.CSharp,
    DisableAsynchronousProjectTreeLoad = true)]

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    [ProvideProjectFactory(typeof(MigrateXprojProjectFactory), null, "#8", "xproj", "xproj", null)]
    internal class CSharpProjectSystemPackage : AsyncPackage
    {
        public const string ProjectTypeGuid = "9A19103F-16F7-4668-BE54-9A1E7A4F7556";
        public const string LegacyProjectTypeGuid = "FAE04EC0-301F-11d3-BF4B-00C04F79EFBC";
        public const string PackageGuid = "860A27C0-B665-47F3-BC12-637E16A1050A";

        private IVsProjectFactory _factory;

        public CSharpProjectSystemPackage()
        {
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _factory = new MigrateXprojProjectFactory(new ProcessRunner(),
                new Win32FileSystem(),
                ServiceProvider.GlobalProvider,
                new GlobalJsonRemover.GlobalJsonSetup());
            _factory.SetSite(new ServiceProviderToOleServiceProviderAdapter(ServiceProvider.GlobalProvider));
            RegisterProjectFactory(_factory);
        }
    }
}
