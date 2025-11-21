// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Flags = Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.DependencyTreeFlags;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Commands;

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
            Flags.ProjectDependency |
            Flags.SharedProjectDependency);
    }

    private async Task NavigateToAsync(IProjectTree node)
    {
        await _threadingService.SwitchToUIThread();

        if (!_solutionExplorer.IsAvailable)
            return;

        // Find the hierarchy based on the project file
        var hierarchy = (IVsUIHierarchy?)_projectServices.GetHierarchyByProjectName(_project.FullPath);

        if (hierarchy is null)
        {
            string? browsePath = await DependencyServices.GetBrowsePathAsync(_project, node);
            if (browsePath is null)
                return;

            // Find the hierarchy based on the browse path
            hierarchy = (IVsUIHierarchy?)_projectServices.GetHierarchyByProjectName(browsePath);
        }

        if (hierarchy is null)
            return;

        // Select the project node in the tree
        _ = _solutionExplorer.Select(hierarchy, HierarchyId.Root);
    }
}
