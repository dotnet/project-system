// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{

    /// <summary>
    /// Handles populating a menu command on the debug dropdown when the menu reflects the IEnumValues for
    /// a debug property. It shows the active framework used for running the app (F5/Ctrl+F5).
    /// </summary>
    [Export(typeof(DebugFrameworksDynamicMenuCommand))]
    internal class DebugFrameworksDynamicMenuCommand : DynamicMenuCommand
    {
        private const int MaxFrameworks = 20;

        [ImportingConstructor]
        public DebugFrameworksDynamicMenuCommand(IStartupProjectHelper startupProjectHelper)
          : base(new CommandID(new Guid(CommandGroup.ManagedProjectSystem), ManagedProjectSystemCommandId.DebugFrameworks), MaxFrameworks)
        {
            StartupProjectHelper = startupProjectHelper;
        }

        private IStartupProjectHelper StartupProjectHelper { get; }

        /// <summary>
        /// Called by the base when one if our menu ids is clicked. Need to return true if the command was handled
        /// </summary>
        public override bool ExecCommand(int cmdIndex, EventArgs e)
        {
            bool handled = false;
            ImmutableArray<IActiveDebugFrameworkServices> activeDebugFrameworks = StartupProjectHelper.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles);
            if (activeDebugFrameworks.Length > 0)
            {
                ExecuteSynchronously(async () =>
                {
                    foreach (IActiveDebugFrameworkServices activeDebugFramework in activeDebugFrameworks)
                    {
                        List<string> frameworks = await activeDebugFramework.GetProjectFrameworksAsync();
                        if (frameworks != null && cmdIndex >= 0 && cmdIndex < frameworks.Count)
                        {
                            await activeDebugFramework.SetActiveDebuggingFrameworkPropertyAsync(frameworks[cmdIndex]);
                            handled = true;
                        }
                    }
                });
            }

            return handled;
        }

        /// <summary>
        /// Called by the base when one of our menu ids is queried for. If the index is
        /// is greater than the count we want to return false
        /// </summary>
        public override bool QueryStatusCommand(int cmdIndex, EventArgs e)
        {
            ImmutableArray<IActiveDebugFrameworkServices> activeDebugFrameworks = StartupProjectHelper.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles);
            if (activeDebugFrameworks.Length > 0)
            {
                // See if the projects support at least two runtimes
                List<string> frameworks = null;
                string activeFramework = null;
                ExecuteSynchronously(async () =>
                {
                    List<string> first = null;

                    foreach (IActiveDebugFrameworkServices activeDebugFramework in activeDebugFrameworks)
                    {
                        frameworks = await activeDebugFramework.GetProjectFrameworksAsync();

                        if (first == null)
                        {
                            first = frameworks;
                        }
                        else
                        {
                            if (!first.SequenceEqual(frameworks))
                            {
                                frameworks = null;
                                break;
                            }
                        }
                    }

                    if (frameworks != null && frameworks.Count > 1 && cmdIndex < frameworks.Count)
                    {
                        // Only call this if we will need it down below.
                        activeFramework = await activeDebugFrameworks[0].GetActiveDebuggingFrameworkPropertyAsync();
                    }
                });

                if (frameworks == null || frameworks.Count < 2)
                {
                    // Hide and disable the command
                    Visible = false;
                    Enabled = false;
                    Checked = false;
                    return true;
                }
                else if (cmdIndex >= 0 && cmdIndex < frameworks.Count)
                {
                    Text = frameworks[cmdIndex];
                    Visible = true;
                    Enabled = true;

                    // Get's a check if it matches the active one, or there is no active one in which case the first one is the active one
                    Checked = (string.IsNullOrEmpty(activeFramework) && cmdIndex == 0) || string.Equals(frameworks[cmdIndex], activeFramework, StringComparison.Ordinal);
                    MatchedCommandId = 0;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// For unit testing to wrap the JTF.Run call.
        /// </summary>
        protected virtual void ExecuteSynchronously(Func<Task> asyncFunction)
        {
#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
            ThreadHelper.JoinableTaskFactory.Run(asyncFunction);
#pragma warning restore VSTHRD102 // Implement internal logic asynchronously
        }
    }
}
