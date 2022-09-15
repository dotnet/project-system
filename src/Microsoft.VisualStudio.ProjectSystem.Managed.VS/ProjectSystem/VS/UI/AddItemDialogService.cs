// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    /// <summary>
    ///     Provides an implementation of <see cref="IAddItemDialogService"/> that wraps <see cref="IVsAddProjectItemDlg"/>.
    /// </summary>
    [Export(typeof(IAddItemDialogService))]
    internal class AddItemDialogService : IAddItemDialogService
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IPhysicalProjectTree _projectTree;
        private readonly IVsUIService<IVsAddProjectItemDlg?> _addProjectItemDialog;

        [ImportingConstructor]
        public AddItemDialogService(IUnconfiguredProjectVsServices unconfiguredProjectVsServices, IPhysicalProjectTree projectTree, IVsUIService<SVsAddProjectItemDlg, IVsAddProjectItemDlg?> addProjectItemDialog)
        {
            _projectVsServices = unconfiguredProjectVsServices;
            _projectTree = projectTree;
            _addProjectItemDialog = addProjectItemDialog;
        }

        public Task<bool> ShowAddNewItemDialogAsync(IProjectTree node)
        {
            return ShowDialogAsync(node,
                __VSADDITEMFLAGS.VSADDITEM_AddNewItems |
                __VSADDITEMFLAGS.VSADDITEM_SuggestTemplateName |
                __VSADDITEMFLAGS.VSADDITEM_AllowHiddenTreeView);
        }

        public Task<bool> ShowAddNewItemDialogAsync(IProjectTree node, string directoryLocalizedName, string templateLocalizedName)
        {
            Requires.NotNullOrEmpty(directoryLocalizedName, nameof(directoryLocalizedName));
            Requires.NotNullOrEmpty(templateLocalizedName, nameof(templateLocalizedName));

            return ShowDialogAsync(node,
               __VSADDITEMFLAGS.VSADDITEM_AddNewItems |
               __VSADDITEMFLAGS.VSADDITEM_SuggestTemplateName |
               __VSADDITEMFLAGS.VSADDITEM_AllowHiddenTreeView,
               directoryLocalizedName,
               templateLocalizedName);
        }

        public Task<bool> ShowAddExistingItemsDialogAsync(IProjectTree node)
        {
            return ShowDialogAsync(node,
                __VSADDITEMFLAGS.VSADDITEM_AddExistingItems |
                __VSADDITEMFLAGS.VSADDITEM_AllowMultiSelect |
                __VSADDITEMFLAGS.VSADDITEM_AllowStickyFilter |
                __VSADDITEMFLAGS.VSADDITEM_ProjectHandlesLinks);
        }

        private async Task<bool> ShowDialogAsync(IProjectTree node, __VSADDITEMFLAGS flags, string? localizedDirectoryName = null, string? localizedTemplateName = null)
        {
            string? path = _projectTree.TreeProvider.GetAddNewItemDirectory(node);
            if (path is null)
                throw new ArgumentException("Node is marked with DisableAddItemFolder or DisableAddItemRecursiveFolder, call CanAddNewOrExistingItemTo before calling this method.", nameof(node));

            await _projectVsServices.ThreadingService.SwitchToUIThread();

            string filter = string.Empty;
            Guid addItemTemplateGuid = Guid.Empty;  // Let the dialog ask the hierarchy itself

            IVsAddProjectItemDlg? addProjectItemDialog = _addProjectItemDialog.Value;
            if (addProjectItemDialog is null)
                return false;

            HResult result = addProjectItemDialog.AddProjectItemDlg(
                node.GetHierarchyId(),
                ref addItemTemplateGuid,
                _projectVsServices.VsProject,
                (uint)flags,
                localizedDirectoryName,
                localizedTemplateName,
                ref path,
                ref filter,
                out _);

            if (result == HResult.Ole.PromptSaveCancelled)
                return false;

            if (result.Failed)
                throw result.Exception!;

            return true;
        }

        public bool CanAddNewOrExistingItemTo(IProjectTree node)
        {
            return _projectTree.TreeProvider.GetAddNewItemDirectory(node) is not null;
        }
    }
}
