// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Design;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    /// <summary>
    /// Handles populating a menu command on the debug dropdown when the menu reflects the IEnumValues for
    /// a debug property. It shows the active framework used for running the app (F5/Ctrl+F5).
    /// </summary>
    [Export(PackageCommandRegistrationService.PackageCommandContract, typeof(MenuCommand))]
    internal class DebugFrameworksDynamicMenuCommand : DynamicMenuCommand
    {
        private const int MaxFrameworks = 20;
        private readonly IStartupProjectHelper _startupProjectHelper;
        private readonly JoinableTaskContext _joinableTaskContext;

        [ImportingConstructor]
        public DebugFrameworksDynamicMenuCommand(IStartupProjectHelper startupProjectHelper, JoinableTaskContext joinableTaskContext)
          : base(new CommandID(new Guid(CommandGroup.ManagedProjectSystem), ManagedProjectSystemCommandId.DebugFrameworks), MaxFrameworks)
        {
            _startupProjectHelper = startupProjectHelper;
            _joinableTaskContext = joinableTaskContext;
        }

        /// <summary>
        /// Called by the base when one if our menu ids is clicked. Need to return true if the command was handled
        /// </summary>
        public override bool ExecCommand(int cmdIndex, EventArgs e)
        {
            bool handled = false;
            ImmutableArray<IActiveDebugFrameworkServices> activeDebugFrameworks = _startupProjectHelper.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles);
            if (!activeDebugFrameworks.IsEmpty)
            {
                ExecuteSynchronously(async () =>
                {
                    foreach (IActiveDebugFrameworkServices activeDebugFramework in activeDebugFrameworks)
                    {
                        List<string>? frameworks = await activeDebugFramework.GetProjectFrameworksAsync();
                        if (frameworks is not null && cmdIndex >= 0 && cmdIndex < frameworks.Count)
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
            ImmutableArray<IActiveDebugFrameworkServices> activeDebugFrameworks = _startupProjectHelper.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles);
            if (!activeDebugFrameworks.IsEmpty)
            {
                // See if the projects support at least two runtimes
                List<string>? frameworks = null;
                string? activeFramework = null;
                ExecuteSynchronously(async () =>
                {
                    List<string>? first = null;

                    foreach (IActiveDebugFrameworkServices activeDebugFramework in activeDebugFrameworks)
                    {
                        frameworks = await activeDebugFramework.GetProjectFrameworksAsync();

                        if (first is null)
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

                    if (frameworks?.Count > 1 && cmdIndex < frameworks.Count)
                    {
                        // Only call this if we will need it down below.
                        activeFramework = await activeDebugFrameworks[0].GetActiveDebuggingFrameworkPropertyAsync();
                    }
                });

                if (frameworks is null || frameworks.Count < 2)
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
                    Checked = (string.IsNullOrEmpty(activeFramework) && cmdIndex == 0) || string.Equals(frameworks[cmdIndex], activeFramework, StringComparisons.ConfigurationDimensionNames);
                    MatchedCommandId = 0;
                    return true;
                }
            }

            return false;
        }

        private void ExecuteSynchronously(Func<Task> asyncFunction)
        {
#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
            _joinableTaskContext.Factory.Run(asyncFunction);
#pragma warning restore VSTHRD102 // Implement internal logic asynchronously
        }
    }
}
