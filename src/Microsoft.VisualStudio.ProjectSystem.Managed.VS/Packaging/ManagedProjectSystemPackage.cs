// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.FSharp;
using Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands;
using Microsoft.VisualStudio.ProjectSystem.VS.Xproj;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    [ProvideProjectFactory(typeof(XprojProjectFactory), null, "#27", "xproj", "xproj", null)]
    [ProvideAutoLoad(ActivationContextGuid, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideUIContextRule(ActivationContextGuid,
        name: "Load Managed Project Package",
        expression: "dotnetcore",
        termNames: new[] { "dotnetcore" },
        termValues: new[] { "SolutionHasProjectCapability:.NET & CPS" }
        )]
    [ProvideMenuResource("Menus.ctmenu", 4)]
    internal partial class ManagedProjectSystemPackage : AsyncPackage
    {
        public const string ActivationContextGuid = "E7DF1626-44DD-4E8C-A8A0-92EAB6DDC16E";
        public const string PackageGuid = "860A27C0-B665-47F3-BC12-637E16A1050A";

        private IDotNetCoreProjectCompatibilityDetector? _dotNetCoreCompatibilityDetector;
        private IDisposable? _projectSelector;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
#pragma warning disable RS0030 // Do not used banned APIs
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
#pragma warning restore RS0030 // Do not used banned APIs

            _projectSelector = await FSharpProjectSelector.RegisterAsync(this);

            IComponentModel componentModel = await this.GetServiceAsync<SComponentModel, IComponentModel>();

            var solutionService = (SolutionService)componentModel.GetService<ISolutionService>();
            solutionService.StartListening();

            var mcs = (OleMenuCommandService)await GetServiceAsync(typeof(IMenuCommandService));
            mcs.AddCommand(componentModel.GetService<DebugFrameworksDynamicMenuCommand>());
            mcs.AddCommand(componentModel.GetService<DebugFrameworkPropertyMenuTextUpdater>());

            // Need to use the CPS export provider to get the dotnet compatibility detector
            IProjectServiceAccessor projectServiceAccessor = componentModel.GetService<IProjectServiceAccessor>();
            _dotNetCoreCompatibilityDetector = projectServiceAccessor.GetProjectService().Services.ExportProvider.GetExport<IDotNetCoreProjectCompatibilityDetector>().Value;
            await _dotNetCoreCompatibilityDetector.InitializeAsync();

            IVsProjectFactory factory = new XprojProjectFactory();
            factory.SetSite(new ServiceProviderToOleServiceProviderAdapter(ServiceProvider.GlobalProvider));
            RegisterProjectFactory(factory);

#if DEBUG
            DebuggerTraceListener.RegisterTraceListener();
#endif
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _projectSelector?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
