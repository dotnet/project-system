// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem;
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
        new string[] {"dotnetcore"},
        new string[] {"SolutionHasProjectCapability:(CSharp | VB) & CPS"}
        )]

    [ProvideMenuResource("Menus.ctmenu", 3)]
    internal partial class ManagedProjectSystemPackage : AsyncPackage
    {
        public const string ActivationContextGuid = "E7DF1626-44DD-4E8C-A8A0-92EAB6DDC16E";
        public const string PackageGuid = "A4F9D880-9492-4072-8BF3-2B5EEEDC9E68";
        public const string ManagedProjectSystemCommandSet = "{568ABDF7-D522-474D-9EED-34B5E5095BA5}";
        public const long GenerateNuGetPackageProjectContextMenuCmdId = 0x2000;
        public const long GenerateNuGetPackageTopLevelBuildCmdId = 0x2001;
        public const int DebugTargetMenuDebugFrameworkMenu = 0x3000;
        public const int DebugFrameworksCmdId = 0x3050;

        public const string ManagedProjectSystemOrderCommandSet = "{6C4806E9-034E-4B64-99DE-29A6F837B993}";
        public const int MoveUpCmdId = 0x2000;
        public const int MoveDownCmdId = 0x2001;
        public const int AddNewItemAboveCmdId = 0x2002;
        public const int AddExistingItemAboveCmdId = 0x2003;
        public const int AddNewItemBelowCmdId = 0x2004;
        public const int AddExistingItemBelowCmdId = 0x2005;

        public const string SolutionExplorerGuid = "{3AE79031-E1BC-11D0-8F78-00A0C9110057}";

        public const string DefaultCapabilities = ProjectCapability.AppDesigner + "; " +
                                                  ProjectCapability.EditAndContinue + "; " +
                                                  ProjectCapability.HandlesOwnReload + "; " +
                                                  ProjectCapability.OpenProjectFile + "; " +
                                                  ProjectCapability.PreserveFormatting + "; " +
                                                  ProjectCapability.ProjectConfigurationsDeclaredDimensions + "; " +
                                                  ProjectCapability.LanguageService;

        public ManagedProjectSystemPackage()
        {
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = (IComponentModel)(await GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(true));
            ICompositionService compositionService = componentModel.DefaultCompositionService;
            var debugFrameworksCmd = componentModel.DefaultExportProvider.GetExport<DebugFrameworksDynamicMenuCommand>();

            var mcs = (await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true)) as OleMenuCommandService;
            mcs.AddCommand(debugFrameworksCmd.Value);

            var debugFrameworksMenuTextUpdater = componentModel.DefaultExportProvider.GetExport<DebugFrameworkPropertyMenuTextUpdater>();
            mcs.AddCommand(debugFrameworksMenuTextUpdater.Value);

#if DEBUG
            DebuggerTraceListener.RegisterTraceListener();
#endif
        }
    }
}
