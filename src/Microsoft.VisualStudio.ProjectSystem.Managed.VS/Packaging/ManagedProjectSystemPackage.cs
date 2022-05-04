// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Xproj;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.Assembly, UseManagedResourcesOnly = true)]
    [ProvideProjectFactory(typeof(XprojProjectFactory), null, "#27", "xproj", "xproj", null)]
    [ProvideAutoLoad(ActivationContextGuid, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideUIContextRule(ActivationContextGuid,
        name: "Load Managed Project Package",
        expression: "dotnetcore",
        termNames: new[] { "dotnetcore" },
        termValues: new[] { "SolutionHasProjectCapability:.NET & CPS" }
        )]
    [ProvideMenuResource("Menus.ctmenu", 5)]
    internal sealed class ManagedProjectSystemPackage : AsyncPackage
    {
        public const string ActivationContextGuid = "E7DF1626-44DD-4E8C-A8A0-92EAB6DDC16E";
        public const string PackageGuid = "860A27C0-B665-47F3-BC12-637E16A1050A";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            IComponentModel componentModel = await this.GetServiceAsync<SComponentModel, IComponentModel>();

            IEnumerable<IPackageService> packageServices = componentModel.GetExtensions<IPackageService>();

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            foreach (IPackageService packageService in packageServices)
            {
                await packageService.InitializeAsync(this);
            }
        }
    }
}
