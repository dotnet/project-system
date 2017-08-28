// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    /// <summary>
    /// Updates the text of the Frameworks menu to include the current active framework. Instead of just saying
    /// Frameworks it will say Frameworks (netcoreapp1.0).
    /// </summary>
    [Export(typeof(DebugFrameworkPropertyMenuTextUpdater))]
    internal class DebugFrameworkPropertyMenuTextUpdater : OleMenuCommand
    {
        [ImportingConstructor]
        public DebugFrameworkPropertyMenuTextUpdater(IStartupProjectHelper startupProjectHelper)
                :base(ExecHandler, delegate { }, QueryStatusHandler, 
                      new CommandID(new Guid(ManagedProjectSystemPackage.ManagedProjectSystemCommandSet), ManagedProjectSystemPackage.DebugTargetMenuDebugFrameworkMenu))
        {
            StartupProjectHelper = startupProjectHelper;
        }

        IStartupProjectHelper StartupProjectHelper { get; }

        /// <summary>
        /// Exec handler called when one of the menu items is selected. Does some
        /// basic validation before calling the commands QueryStatusCommand to update
        /// its state
        /// </summary>
        public static void ExecHandler(object sender, EventArgs e)
        {
            return;
        }


        /// <summary>
        /// QueryStatus handler called to update the status of the menu items. Does some
        /// basic validation before calling the commands QueryStatusCommand to update
        /// its state
        /// </summary>
        public static void QueryStatusHandler(object sender, EventArgs e)
        {   
            var command = sender as DebugFrameworkPropertyMenuTextUpdater;
            if(command == null)
            {
                return;
            }

            command.QueryStatus();
        }

        public void QueryStatus()
        {
            var activeDebugFramework = StartupProjectHelper.GetExportFromSingleDotNetStartupProject<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles);
            if(activeDebugFramework != null)
            {
                string activeFramework = null;
                List<string> frameworks = null;
                ExecuteSynchronously(async () =>
                {
                    frameworks = await activeDebugFramework.GetProjectFrameworksAsync().ConfigureAwait(false);
                    if(frameworks != null && frameworks.Count > 1)
                    {
                        // Only get this if we will need it down below
                        activeFramework = await activeDebugFramework.GetActiveDebuggingFrameworkPropertyAsync().ConfigureAwait(false);
                    }
                });

                if(frameworks != null && frameworks.Count > 1)
                {
                    // If no active framework or the current active property doesn't match any of the frameworks, then
                    // st it to the first one.
                    if(!string.IsNullOrEmpty(activeFramework) && frameworks.Contains(activeFramework))
                    {
                        Text = string.Format(VSResources.DebugFrameworkMenuText, activeFramework);
                    }
                    else
                    {
                        Text = string.Format(VSResources.DebugFrameworkMenuText, frameworks[0]);
                    }

                    Visible = true;
                    Enabled = true;
                }
            }
        }

        /// <summary>
        /// For unit testing to wrap the JTF.Run call.
        /// </summary>
        protected virtual void ExecuteSynchronously(Func<Task> asyncFunction)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {   
                await asyncFunction().ConfigureAwait(false);
            });
        }
    }
}
