// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
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
    projectTypeGuid: ProjectType.CSharp,
    displayName: "#1",                      // "C#"
    displayProjectFileExtensions: "#2",     // "C# Project Files (*.csproj);*.csproj"
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
