// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    [ProvideEditorFactory(factoryType: typeof(LoadedProjectFileEditorFactory), nameResourceID: 7, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_HasUntrustedLogicalViews)]
    [ProvideEditorExtension(factoryType: typeof(LoadedProjectFileEditorFactory), extension: ".csproj", priority: 32, NameResourceID = 7)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    internal class ManagedProjectSystemPackage : AsyncPackage
    {
        public const string PackageGuid = "A4F9D880-9492-4072-8BF3-2B5EEEDC9E68";
        public const string ManagedProjectSystemCommandSet = "{568ABDF7-D522-474D-9EED-34B5E5095BA5}";
        public const long EditProjectFileCmdId = 0x1001;
        public const long GenerateNuGetPackageProjectContextMenuCmdId = 0x2000;
        public const long GenerateNuGetPackageTopLevelBuildCmdId = 0x2001;

        public ManagedProjectSystemPackage()
        {
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            RegisterEditorFactory(new LoadedProjectFileEditorFactory(this));
        }
    }
}
