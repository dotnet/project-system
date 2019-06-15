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
    [ProvideUIContextRule(ActivationContextGuid, "Load Managed Project Package",
        "dotnetcore",
        new string[] { "dotnetcore" },
        new string[] { "SolutionHasProjectCapability:.NET & CPS" }
        )]

    [ProvideMenuResource("Menus.ctmenu", 3)]
    internal partial class ManagedProjectSystemPackage : AsyncPackage
    {
        public const string ActivationContextGuid = "E7DF1626-44DD-4E8C-A8A0-92EAB6DDC16E";
        public const string PackageGuid = "860A27C0-B665-47F3-BC12-637E16A1050A";


        public const string DefaultCapabilities = ProjectCapability.AppDesigner + "; " +
                                                  ProjectCapability.EditAndContinue + "; " +
                                                  ProjectCapability.HandlesOwnReload + "; " +
                                                  ProjectCapability.OpenProjectFile + "; " +
                                                  ProjectCapability.PreserveFormatting + "; " +
                                                  ProjectCapability.ProjectConfigurationsDeclaredDimensions + "; " +
                                                  ProjectCapability.LanguageService + "; " +
                                                  ProjectCapability.DotNet;

        private IDotNetCoreProjectCompatibilityDetector _dotNetCoreCompatibilityDetector;
        private IVsRegisterProjectSelector _projectSelectorService;
        private uint _projectSelectorCookie = VSConstants.VSCOOKIE_NIL;

        public ManagedProjectSystemPackage()
        {
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
#pragma warning disable RS0030 // Do not used banned APIs
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _projectSelectorService = await this.GetServiceAsync<SVsRegisterProjectTypes, IVsRegisterProjectSelector>();
#pragma warning restore RS0030 // Do not used banned APIs
            Guid selectorGuid = typeof(FSharpProjectSelector).GUID;
            _projectSelectorService.RegisterProjectSelector(ref selectorGuid, new FSharpProjectSelector(), out _projectSelectorCookie);

            var componentModel = (IComponentModel)(await GetServiceAsync(typeof(SComponentModel)));
            Lazy<DebugFrameworksDynamicMenuCommand> debugFrameworksCmd = componentModel.DefaultExportProvider.GetExport<DebugFrameworksDynamicMenuCommand>();

            var mcs = (await GetServiceAsync(typeof(IMenuCommandService))) as OleMenuCommandService;
            mcs.AddCommand(debugFrameworksCmd.Value);

            Lazy<DebugFrameworkPropertyMenuTextUpdater> debugFrameworksMenuTextUpdater = componentModel.DefaultExportProvider.GetExport<DebugFrameworkPropertyMenuTextUpdater>();
            mcs.AddCommand(debugFrameworksMenuTextUpdater.Value);

            // Need to use the CPS export provider to get the dotnet compatibility detector
            Lazy<IProjectServiceAccessor> projectServiceAccessor = componentModel.DefaultExportProvider.GetExport<IProjectServiceAccessor>();
            _dotNetCoreCompatibilityDetector = projectServiceAccessor.Value.GetProjectService().Services.ExportProvider.GetExport<IDotNetCoreProjectCompatibilityDetector>().Value;
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
            if (disposing && _projectSelectorCookie != VSConstants.VSCOOKIE_NIL)
            {
                _projectSelectorService?.UnregisterProjectSelector(_projectSelectorCookie);
                _projectSelectorCookie = VSConstants.VSCOOKIE_NIL;
            }

            base.Dispose(disposing);
        }
    }
}
