// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;
using Microsoft.VisualStudio.Shell.Interop;

using Flags = Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.DependencyTreeFlags;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Commands;

/// <summary>
/// Implements Visual Studio's "Open in Object Browser" command for dependencies tree nodes.
/// </summary>
/// <remarks>
/// Items that can be opened in the Object Browser must advertise the fact.
/// The approach depends on the type of node:
/// <list type="bullet">
/// <item>
/// Direct project dependencies, modelled as <see cref="IDependency"/>, must also implement <see cref="IDependencyWithBrowseObject"/>,
/// include the <see cref="Flags.SupportsObjectBrowser"/> tree flag, and populate the <c>BrowsePath</c> property on the browse object.
/// </item>
/// <item>
/// Relatable items (attached sub-nodes) modelled as <see cref="IRelatableItem"/>, must implement <see cref="IObjectBrowserItem"/>.
/// </item>
/// </list>
/// </remarks>
[ProjectCommand(VSConstants.CMDSETID.StandardCommandSet2K_string, (long)VSConstants.VSStd2KCmdID.VIEWREFINOBJECTBROWSER)]
[AppliesTo(ProjectCapability.DependenciesTree)]
[Order(Order.Default)]
[method: ImportingConstructor]
internal sealed class ObjectBrowserCommand(UnconfiguredProject project, IVsUIService<SVsObjBrowser, IVsNavigationTool> navigationTool) : AbstractSingleNodeProjectCommand
{
    // This code needs a little explanation. Consider a tree like this:
    //
    // - Dependencies
    //   - Packages
    //     - MyPackage
    //       - MyPackage.dll
    // - Frameworks
    //   - Microsoft.NETCore.App
    //     - Microsoft.CSharp
    //
    // The "MyPackage" and "Microsoft.NETCore.App" nodes are top-level dependencies, and they have IProjectTree nodes.
    //
    // The "MyPackage.dll" and "Microsoft.CSharp" nodes are transitive nodes, added via the "AttachedCollections" API
    // (IRelatableItem) and do not have IProjectTree nodes.
    //
    // When the user right-clicks on an IRelatableItem node, CPS is called with the IProjectTree node of the closest
    // ancestor, which is not the node the user clicked on. There's no easy way to thread that state through on the
    // call stack, so we use a static collection on the menu controller that's only populated while the context menu
    // is open. If it has items here, we know we're in the transitive case and will attempt to handle the command.

    protected override async Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, bool focused, string? commandText, CommandStatus progressiveStatus)
    {
        string? path = await TryGetAssemblyPathAsync(node);

        if (path is not null)
        {
            // We handle this.
            return new CommandStatusResult(handled: true, commandText, CommandStatus.Enabled | CommandStatus.Supported);
        }

        // We won't handle this. Allow other handlers to do so.
        return CommandStatusResult.Unhandled;
    }

    protected override async Task<bool> TryHandleCommandAsync(IProjectTree node, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
    {
        string? path = await TryGetAssemblyPathAsync(node);

        if (path is not null)
        {
            ShowAssemblyInObjectBrowser();

            // Handled.
            return true;
        }

        // Unhandled. Should not happen in the context menu, as we only show the command when we can handle it.
        // Might theoretically happen if someone invoked the command directly on the tree.
        return false;

        void ShowAssemblyInObjectBrowser()
        {
            // We're opening a file as a container (DLL).
            SYMBOL_DESCRIPTION_NODE[] symbolNodes = [
                new() { dwType = (uint)_LIB_LISTTYPE.LLT_PHYSICALCONTAINERS, pszName = path }
            ];

            // The container is a "COM+" (.NET) library. This tells Object Browser how to load the file.
            Guid libraryGuid = VSConstants.guidCOMPLUSLibrary;

            // Open the Object Browser.
            Verify.HResult(navigationTool.Value.NavigateToSymbol(ref libraryGuid, symbolNodes, (uint)symbolNodes.Length));
        }
    }

    private async Task<string?> TryGetAssemblyPathAsync(IProjectTree node)
    {
        if (RelatableItemBase.MenuController.CurrentItems.Length is not 0)
        {
            // An attached relatable items is selected. This command handler is invoked on the closest
            // IProjectTree ancestor node rather than the clicked items. So "node" should be ignored.
            // Instead, use the "current items" of the menu controller to access selected relatable items.
            return HandleTransitiveNode();
        }
        else
        {
            return await HandleDirectNodeAsync();
        }

        string? HandleTransitiveNode()
        {
            // We only handle a single item of the required type.
            if (RelatableItemBase.MenuController.CurrentItems is [IObjectBrowserItem { AssemblyPath: string assemblyPath }])
            {
                // Single item, with the required interface.
                return IfExists(assemblyPath);
            }

            // Multiple items. Unhandled.
            return null;
        }

        async Task<string?> HandleDirectNodeAsync()
        {
            if (node.Flags.ContainsAny(Flags.SupportsObjectBrowser))
            {
                // Get the browse path from the browse object.
                string? path = await DependencyServices.GetBrowsePathAsync(project, node);

                return IfExists(path);
            }

            // Node doesn't support the object browser.
            return null;
        }

        static string? IfExists(string? path) => path is not null && File.Exists(path) ? path : null;
    }
}
