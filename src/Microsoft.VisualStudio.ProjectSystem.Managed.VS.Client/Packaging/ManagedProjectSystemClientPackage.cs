// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    [ProvideEditorFactory(
        typeof(ProjectPropertiesEditorFactory),
        nameResourceID: 1100,
        deferUntilIntellisenseIsReady: false,
        TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    internal sealed class ManagedProjectSystemClientPackage : AsyncPackage
    {
        public const string PackageGuid = "AE74FDFC-B9CE-4948-9E2F-F443B5BE8D37";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            RegisterEditorFactory(new ProjectPropertiesEditorFactory());
        }
    }
}
