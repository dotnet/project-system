// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Xproj;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

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

        /// <summary>
        /// A static instance of the package's JTF. Will be null if the package has not
        /// yet been initialized. Note that package initialization occurs after many
        /// MEF components are instantiated.
        /// </summary>
        /// <remarks>
        /// Async work queued with this instance of the JTF will be awaited before
        /// the package is unloaded. This allows shutdown work to be safely scheduled
        /// to run in the background without blocking the main thread, without having to
        /// worry that the work might not complete before the process exits.
        /// </remarks>
        public static JoinableTaskFactory? PackageJoinableTaskFactory { get; private set; }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // Assign the package's JTF instance to a public static property for use elsewhere
            PackageJoinableTaskFactory = JoinableTaskFactory;

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
