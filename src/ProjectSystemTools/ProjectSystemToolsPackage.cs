// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tools
{
    [PackageRegistration(AllowsBackgroundLoading = true, UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(BuildLoggingToolWindow))]
    public sealed class ProjectSystemToolsPackage : AsyncPackage
    {
        public const string PackageGuidString = "e3bfb509-b8fd-4692-b4c4-4b2f6ed62bc7";
        public static readonly Guid CommandSetGuid = new Guid("cf0c6f43-4716-4419-93d0-2c246c8eb5ee");

        public const int BuildLoggingCommandId = 0x0100;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            mcs.AddCommand(new MenuCommand(ShowBuildLoggingToolWindow, new CommandID(CommandSetGuid, BuildLoggingCommandId)));
        }

        private void ShowBuildLoggingToolWindow(object sender, EventArgs e)
        {
            var window = FindToolWindow(typeof(BuildLoggingToolWindow), 0, true);
            if ((window == null) || (window.Frame == null))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            var windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
