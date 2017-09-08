// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
using System.ComponentModel.Design;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tools
{
    [PackageRegistration(AllowsBackgroundLoading = true, UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(BuildLoggingToolWindow), Style = VsDockStyle.Tabbed, Window = ToolWindowGuids.Outputwindow)]
    public sealed class ProjectSystemToolsPackage : AsyncPackage
    {
        public const string PackageGuidString = "e3bfb509-b8fd-4692-b4c4-4b2f6ed62bc7";

        public static readonly Guid CommandSetGuid = new Guid("cf0c6f43-4716-4419-93d0-2c246c8eb5ee");

        public const int BuildLoggingCommandId = 0x0100;
        public const int StartLoggingCommandId = 0x0101;
        public const int StopLoggingCommandId = 0x0102;
        public const int ClearCommandId = 0x0103;
        public const int SaveLogsCommandId = 0x0107;

        public static readonly Guid UIGuid = new Guid("629080DF-2A44-40E5-9AF4-371D4B727D16");

        public const int BuildLoggingToolbarMenuId = 0x0100;
        public const int BuildLoggingContextMenuId = 0x0105;

        public static IVsUIShell VsUIShell { get; private set; }

        public static IVsFindManager VsFindManager { get; private set; }

        public static ProjectSystemToolsPackage Instance;

        internal static ITableManagerProvider TableManagerProvider { get; private set; }
        public static IWpfTableControlProvider TableControlProvider { get; private set; }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            VsUIShell = GetService(typeof(IVsUIShell)) as IVsUIShell;
            VsFindManager = GetService(typeof(SVsFindManager)) as IVsFindManager;

            var componentModel = GetService(typeof(SComponentModel)) as IComponentModel;
            TableControlProvider = componentModel?.GetService<IWpfTableControlProvider>();
            TableManagerProvider = componentModel?.GetService<ITableManagerProvider>();

            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            mcs?.AddCommand(new MenuCommand(ShowBuildLoggingToolWindow, new CommandID(CommandSetGuid, BuildLoggingCommandId)));

            Instance = this;
        }

        public static void UpdateQueryStatus()
        {
            // Force the shell to refresh the QueryStatus for all the command since some of them may have been flagged as
            // not supported (because the host had focus but the view did not) and switching focus from the zoom control
            // back to the view will not automatically force the shell to requery for the command status.
            VsUIShell?.UpdateCommandUI(0);
        }

        private void ShowBuildLoggingToolWindow(object sender, EventArgs e)
        {
            var window = FindToolWindow(typeof(BuildLoggingToolWindow), 0, true);
            if (window?.Frame == null)
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            var windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
