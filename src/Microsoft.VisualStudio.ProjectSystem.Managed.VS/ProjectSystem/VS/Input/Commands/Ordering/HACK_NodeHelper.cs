using System;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    internal static class HACK_NodeHelper
    {
        public static void Select(ConfiguredProject configuredProject, IServiceProvider serviceProvider, IProjectTree node)
        {
            Select(configuredProject, serviceProvider, (uint)node.Identity.ToInt32());
        }

        public static void ExpandFolder(ConfiguredProject configuredProject, IServiceProvider serviceProvider, IProjectTree node)
        {
            ExpandFolder(configuredProject, serviceProvider, (uint)node.Identity.ToInt32());
        }

        public static void CollapseFolder(ConfiguredProject configuredProject, IServiceProvider serviceProvider, IProjectTree node)
        {
            CollapseFolder(configuredProject, serviceProvider, (uint)node.Identity.ToInt32());
        }

        private static void Select(ConfiguredProject configuredProject, IServiceProvider serviceProvider, uint itemId)
        {
            UseWindow(configuredProject, serviceProvider, (hierarchy, window) =>
            {
                // We need to unselect the item if it is already selected to re-select it correctly.
                window.ExpandItem(hierarchy, itemId, EXPANDFLAGS.EXPF_UnSelectItem);
                window.ExpandItem(hierarchy, itemId, EXPANDFLAGS.EXPF_SelectItem);
            });
        }

        private static void ExpandFolder(ConfiguredProject configuredProject, IServiceProvider serviceProvider, uint itemId)
        {
            UseWindow(configuredProject, serviceProvider, (hierarchy, window) =>
            {
                window.ExpandItem(hierarchy, itemId, EXPANDFLAGS.EXPF_ExpandFolder);
            });
        }

        private static void CollapseFolder(ConfiguredProject configuredProject, IServiceProvider serviceProvider, uint itemId)
        {
            UseWindow(configuredProject, serviceProvider, (hierarchy, window) =>
            {
                window.ExpandItem(hierarchy, itemId, EXPANDFLAGS.EXPF_CollapseFolder);
            });
        }

        private static void UseWindow(ConfiguredProject configuredProject, IServiceProvider serviceProvider, Action<IVsUIHierarchy, IVsUIHierarchyWindow> callback)
        {
            if (configuredProject.UnconfiguredProject.Services.HostObject is IVsUIHierarchy hierarchy)
            {
                callback(hierarchy, GetUIHierarchyWindow(serviceProvider, Guid.Parse(ManagedProjectSystemPackage.SolutionExplorerGuid)));
            }
        }

        /// <summary>
        /// Get reference to IVsUIHierarchyWindow interface from guid persistence slot.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="persistenceSlot">Unique identifier for a tool window created using IVsUIShell::CreateToolWindow.
        /// The caller of this method can use predefined identifiers that map to tool windows if those tool windows
        /// are known to the caller. </param>
        /// <returns>A reference to an IVsUIHierarchyWindow interface, or <c>null</c> if the window isn't available, such as command line mode.</returns>
        private static IVsUIHierarchyWindow GetUIHierarchyWindow(IServiceProvider serviceProvider, Guid persistenceSlot)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }


            if (!(serviceProvider.GetService(typeof(SVsUIShell)) is IVsUIShell shell))
            {
                throw new InvalidOperationException();
            }

            object pvar = null;
            IVsUIHierarchyWindow uiHierarchyWindow = null;

            try
            {
                if (ErrorHandler.Succeeded(shell.FindToolWindow(0, ref persistenceSlot, out var frame)) && frame != null)
                {
                    ErrorHandler.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out pvar));
                }
            }
            finally
            {
                if (pvar != null)
                {
                    uiHierarchyWindow = (IVsUIHierarchyWindow)pvar;
                }
            }

            return uiHierarchyWindow;
        }
    }
}
