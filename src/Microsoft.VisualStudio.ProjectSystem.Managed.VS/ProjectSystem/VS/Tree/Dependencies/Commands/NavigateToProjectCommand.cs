// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Commands
{
    /// <summary>
    ///     Navigates from a reference to associated project or shared project 
    ///     in Solution Explorer.
    /// </summary>
    [ProjectCommand(CommandGroup.ManagedProjectSystem, ManagedProjectSystemCommandId.NavigateToProject)]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class NavigateToProjectCommand : AbstractSingleNodeProjectCommand
    {
        private readonly UnconfiguredProject _project;
        private readonly IProjectThreadingService _threadingService;
        private readonly IVsProjectServices _projectServices;
        private readonly SolutionExplorerWindow _solutionExplorer;

        [ImportingConstructor]
        public NavigateToProjectCommand(
            UnconfiguredProject project,
            IProjectThreadingService threadingService,
            IVsProjectServices projectServices,
            SolutionExplorerWindow solutionExplorer)
        {
            _project = project;
            _threadingService = threadingService;
            _projectServices = projectServices;
            _solutionExplorer = solutionExplorer;
        }

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string? commandText, CommandStatus progressiveStatus)
        {
            if (CanNavigateTo(node))
            {
                return GetCommandStatusResult.Handled(commandText, progressiveStatus | CommandStatus.Enabled);
            }

            return GetCommandStatusResult.Unhandled;
        }

        protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (CanNavigateTo(node))
            {
                await NavigateToAsync(node);
                return true;
            }

            return false;
        }

        private static bool CanNavigateTo(IProjectTree node)
        {
            return node.Flags.ContainsAny(
                DependencyTreeFlags.ProjectDependency |
                DependencyTreeFlags.SharedProjectDependency);
        }

        private async Task NavigateToAsync(IProjectTree node)
        {
            string? browsePath = await DependencyServices.GetBrowsePathAsync(_project, node);
            if (browsePath is null)
                return;

            await _threadingService.SwitchToUIThread();

            // Find the hierarchy based on the project file, and then select it
            var hierarchy = (IVsUIHierarchy?)_projectServices.GetHierarchyByProjectName(browsePath);
            if (hierarchy is null || !_solutionExplorer.IsAvailable)
                return;

            _ = _solutionExplorer.Select(hierarchy, HierarchyId.Root);
        }
    }
}
