// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    /// <summary>
    /// These are helper functions to select items in a IVsUIHierarchy.
    /// This is hack due to CPS not exposing functionality to do this. They have it internally though.
    /// Bug filed here: https://devdiv.visualstudio.com/DevDiv/VS%20IDE%20CPS/_workitems/edit/589115
    /// </summary>
    internal static class NodeHelper
    {
        /// <summary>
        /// Select an item in a IVsIHierarchy.
        /// Calls on the UI thread.
        /// </summary>
        public static async Task SelectAsync(ConfiguredProject configuredProject, IServiceProvider serviceProvider, IProjectTree node)
        {
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(node, nameof(node));

            await configuredProject.Services.ProjectService.Services.ThreadingPolicy.SwitchToUIThread();

            Select(configuredProject, serviceProvider, (uint)node.Identity.ToInt32());

            await TaskScheduler.Default;
        }

        /// <summary>
        /// Select an item in a IVsIHierarchy.
        /// </summary>
        private static void Select(ConfiguredProject configuredProject, IServiceProvider serviceProvider, uint itemId)
        {
            UseWindow(configuredProject, serviceProvider, (hierarchy, window) =>
            {
                if (window is not null)
                {
                    // We need to unselect the item if it is already selected to re-select it correctly.
                    window.ExpandItem(hierarchy, itemId, EXPANDFLAGS.EXPF_UnSelectItem);
                    window.ExpandItem(hierarchy, itemId, EXPANDFLAGS.EXPF_SelectItem);
                }
            });
        }

        /// <summary>
        /// Callbacks with a hierarchy and hierarchy window for use.
        /// </summary>
        private static void UseWindow(ConfiguredProject configuredProject, IServiceProvider serviceProvider, Action<IVsUIHierarchy, IVsUIHierarchyWindow?> callback)
        {
            Assumes.NotNull(configuredProject.UnconfiguredProject.Services.HostObject);
            var hierarchy = (IVsUIHierarchy)configuredProject.UnconfiguredProject.Services.HostObject;
            callback(hierarchy, GetUIHierarchyWindow(serviceProvider, VSConstants.StandardToolWindows.SolutionExplorer));
        }

        /// <summary>
        /// Get reference to IVsUIHierarchyWindow interface from guid persistence slot.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="persistenceSlot">Unique identifier for a tool window created using IVsUIShell::CreateToolWindow.
        /// The caller of this method can use predefined identifiers that map to tool windows if those tool windows
        /// are known to the caller. </param>
        /// <returns>A reference to an IVsUIHierarchyWindow interface, or <see langword="null"/> if the window isn't available, such as command line mode.</returns>
        private static IVsUIHierarchyWindow? GetUIHierarchyWindow(IServiceProvider serviceProvider, Guid persistenceSlot)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));

            if (serviceProvider is null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

#pragma warning disable RS0030 // Do not used banned APIs
            IVsUIShell shell = serviceProvider.GetService<IVsUIShell, SVsUIShell>();
#pragma warning restore RS0030 // Do not used banned APIs

            object? pvar = null;
            IVsUIHierarchyWindow? uiHierarchyWindow = null;

            try
            {
                if (ErrorHandler.Succeeded(shell.FindToolWindow(0, ref persistenceSlot, out IVsWindowFrame frame)) && frame is not null)
                {
                    ErrorHandler.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out pvar));
                }
            }
            finally
            {
                if (pvar is not null)
                {
                    uiHierarchyWindow = (IVsUIHierarchyWindow)pvar;
                }
            }

            return uiHierarchyWindow;
        }
    }
}
