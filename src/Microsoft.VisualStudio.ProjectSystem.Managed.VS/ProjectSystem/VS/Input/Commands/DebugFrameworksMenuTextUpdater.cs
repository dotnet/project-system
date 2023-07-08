// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Design;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    /// <summary>
    /// Updates the text of the Frameworks menu to include the current active framework. Instead of just saying
    /// Frameworks it will say Frameworks (netcoreapp1.0).
    /// </summary>
    [Export(PackageCommandRegistrationService.PackageCommandContract, typeof(MenuCommand))]
    internal class DebugFrameworkPropertyMenuTextUpdater : OleMenuCommand
    {
        [ImportingConstructor]
        public DebugFrameworkPropertyMenuTextUpdater(IStartupProjectHelper startupProjectHelper)
            : base(
                ExecHandler,
                delegate { },
                QueryStatusHandler,
                new CommandID(new Guid(CommandGroup.ManagedProjectSystem), ManagedProjectSystemCommandId.DebugTargetMenuDebugFrameworkMenu))
        {
            StartupProjectHelper = startupProjectHelper;
        }

        private IStartupProjectHelper StartupProjectHelper { get; }

        /// <summary>
        /// Exec handler called when one of the menu items is selected. Does some
        /// basic validation before calling the commands QueryStatusCommand to update
        /// its state
        /// </summary>
        public static void ExecHandler(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// QueryStatus handler called to update the status of the menu items. Does some
        /// basic validation before calling the commands QueryStatusCommand to update
        /// its state
        /// </summary>
        public static void QueryStatusHandler(object sender, EventArgs e)
        {
            if (sender is DebugFrameworkPropertyMenuTextUpdater command)
            {
                command.QueryStatus();
            }
        }

        public void QueryStatus()
        {
            ImmutableArray<IActiveDebugFrameworkServices> activeDebugFrameworks = StartupProjectHelper.GetExportFromDotNetStartupProjects<IActiveDebugFrameworkServices>(ProjectCapability.LaunchProfiles);
            if (!activeDebugFrameworks.IsEmpty)
            {
                string? activeFramework = null;
                List<string>? frameworks = null;
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

                    if (frameworks?.Count > 1)
                    {
                        // Only get this if we will need it down below
                        activeFramework = await activeDebugFrameworks[0].GetActiveDebuggingFrameworkPropertyAsync();
                    }
                });

                if (frameworks?.Count > 1)
                {
                    // If no active framework or the current active property doesn't match any of the frameworks, then
                    // set it to the first one.
                    if (!Strings.IsNullOrEmpty(activeFramework) && frameworks.Contains(activeFramework))
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
#pragma warning disable VSTHRD102 // Only wrapped for test purposes
#pragma warning disable RS0030 // Do not used banned APIs
            ThreadHelper.JoinableTaskFactory.Run(asyncFunction);
#pragma warning restore RS0030 // Do not used banned APIs
#pragma warning restore VSTHRD102
        }
    }
}
