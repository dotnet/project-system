// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands;
using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    [ProvideAutoLoad(ActivationContextGuid)]
    [ProvideUIContextRule(ActivationContextGuid, "Load Managed Project Package",
        "dotnetcore",
        new string[] { "dotnetcore" },
        new string[] { "SolutionHasProjectCapability:.NET & CPS" }
        )]

    [ProvideMenuResource("Menus.ctmenu", 3)]
    internal partial class ManagedProjectSystemPackage : AsyncPackage
    {
        public const string ActivationContextGuid = "E7DF1626-44DD-4E8C-A8A0-92EAB6DDC16E";
        public const string PackageGuid = "A4F9D880-9492-4072-8BF3-2B5EEEDC9E68";


        public const string DefaultCapabilities = ProjectCapability.AppDesigner + "; " +
                                                  ProjectCapability.EditAndContinue + "; " +
                                                  ProjectCapability.HandlesOwnReload + "; " +
                                                  ProjectCapability.OpenProjectFile + "; " +
                                                  ProjectCapability.PreserveFormatting + "; " +
                                                  ProjectCapability.ProjectConfigurationsDeclaredDimensions + "; " +
                                                  ProjectCapability.LanguageService + "; " +
                                                  ProjectCapability.DotNet;

        private IDotNetCoreProjectCompatibilityDetector _dotNetCoreCompatibilityDetector;

        public ManagedProjectSystemPackage()
        {
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = (IComponentModel)(await GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(true));
            ICompositionService compositionService = componentModel.DefaultCompositionService;
            Lazy<DebugFrameworksDynamicMenuCommand> debugFrameworksCmd = componentModel.DefaultExportProvider.GetExport<DebugFrameworksDynamicMenuCommand>();

            var mcs = (await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true)) as OleMenuCommandService;
            mcs.AddCommand(debugFrameworksCmd.Value);

            Lazy<DebugFrameworkPropertyMenuTextUpdater> debugFrameworksMenuTextUpdater = componentModel.DefaultExportProvider.GetExport<DebugFrameworkPropertyMenuTextUpdater>();
            mcs.AddCommand(debugFrameworksMenuTextUpdater.Value);

            // Need to use the CPS export provider to get the dotnet compatibility detector
            Lazy<IProjectServiceAccessor> projectServiceAccessor = componentModel.DefaultExportProvider.GetExport<IProjectServiceAccessor>();
            _dotNetCoreCompatibilityDetector = projectServiceAccessor.Value.GetProjectService().Services.ExportProvider.GetExport<IDotNetCoreProjectCompatibilityDetector>().Value;
            await _dotNetCoreCompatibilityDetector.InitializeAsync().ConfigureAwait(true);

#if DEBUG
            DebuggerTraceListener.RegisterTraceListener();
#endif
        }
    }
}
