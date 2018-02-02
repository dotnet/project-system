using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    /// <summary>
    /// This is a HACK due to having to call Vs directly to show a dialog.
    /// Ideally, we should be able to show a dialog from CPS, but we can't at the moment.
    /// </summary>
    internal static class HACK_AddItemHelper
    {
        /// <summary>
        /// Show the item dialog window to add new items.
        /// </summary>
        public static Task ShowAddNewFileDialogAsync(
            IPhysicalProjectTree projectTree,
            IUnconfiguredProjectVsServices projectVsServices,
            SVsServiceProvider serviceProvider,
            IProjectTree target)
        {
            return ShowAddItemDialogAsync(projectTree, projectVsServices, serviceProvider, target, AddItemAction.NewItem);
        }

        /// <summary>
        /// Show the item dialog window to add existing items.
        /// </summary>
        public static Task ShowAddExistingFilesDialogAsync(
            IPhysicalProjectTree projectTree,
            IUnconfiguredProjectVsServices projectVsServices,
            SVsServiceProvider serviceProvider,
            IProjectTree target)
        {
            return ShowAddItemDialogAsync(projectTree, projectVsServices, serviceProvider, target,AddItemAction.ExistingItem);
        }

        /// <summary>
        /// Show the item dialog window to add new/existing items.
        /// </summary>
        private static async Task ShowAddItemDialogAsync(
            IPhysicalProjectTree projectTree, 
            IUnconfiguredProjectVsServices projectVsServices, 
            SVsServiceProvider serviceProvider, 
            IProjectTree target,
            AddItemAction addItemAction)
        {
            Requires.NotNull(projectTree, nameof(projectTree));
            Requires.NotNull(projectVsServices, nameof(projectVsServices));
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(target, nameof(target));

            await projectVsServices.ThreadingService.SwitchToUIThread();

            var strBrowseLocations = projectTree.TreeProvider.GetAddNewItemDirectory(target);
            ShowAddItemDialog(serviceProvider, target, projectVsServices.VsProject, strBrowseLocations, addItemAction);
        }

        private enum AddItemAction { NewItem=0, ExistingItem=1 }

        /// <summary>
        /// Direct Vs call to show the add item dialog.
        /// </summary>
        private static int ShowAddItemDialog(SVsServiceProvider serviceProvider, IProjectTree target, IVsProject vsProject, string strBrowseLocations, AddItemAction addItemAction)
        {
            var addItemDialog = serviceProvider.GetService<IVsAddProjectItemDlg, SVsAddProjectItemDlg>();
            Assumes.Present(addItemDialog);

            var uiFlags = __VSADDITEMFLAGS.VSADDITEM_AddNewItems | __VSADDITEMFLAGS.VSADDITEM_SuggestTemplateName | __VSADDITEMFLAGS.VSADDITEM_AllowHiddenTreeView;
            if (addItemAction == AddItemAction.ExistingItem)
            {
                uiFlags = __VSADDITEMFLAGS.VSADDITEM_AddExistingItems | __VSADDITEMFLAGS.VSADDITEM_AllowMultiSelect | __VSADDITEMFLAGS.VSADDITEM_AllowStickyFilter | __VSADDITEMFLAGS.VSADDITEM_ProjectHandlesLinks;
            }

            var strFilter = string.Empty;
            Guid addItemTemplateGuid = Guid.Empty;  // Let the dialog ask the hierarchy itself

            return addItemDialog.AddProjectItemDlg(target.GetHierarchyId(), ref addItemTemplateGuid, vsProject, (uint)uiFlags,
                null, null, ref strBrowseLocations, ref strFilter, out int iDontShowAgain);
        }
    }
}
