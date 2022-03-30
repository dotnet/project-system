// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Commands
{
    /// <summary>
    /// Provides context menus for nodes in the dependencies tree.
    /// </summary>
    [Export(typeof(IProjectItemContextMenuProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(ProjectSystem.Order.Default)]
    internal class DependenciesContextMenuProvider : IProjectItemContextMenuProvider
    {
        private static class Menus
        {
            public const int IDM_VS_CTXT_REFERENCEROOT              = 0x0450;
            public const int IDM_VS_CTXT_REFERENCE                  = 0x0451;
            public const int IDM_VS_CTXT_DEPENDENCYTARGET           = 0X04A0; // Target framework group node
            public const int IDM_VS_CTXT_REFERENCE_GROUP            = 0X04A1; // Assembly reference group
            public const int IDM_VS_CTXT_PACKAGEREFERENCE_GROUP     = 0x04A2;
            public const int IDM_VS_CTXT_PACKAGEREFERENCE           = 0x04A3;
            public const int IDM_VS_CTXT_COMREFERENCE_GROUP         = 0x04A4;
            public const int IDM_VS_CTXT_COMREFERENCE               = 0x04A5;
            public const int IDM_VS_CTXT_PROJECTREFERENCE_GROUP     = 0x04A6;
            public const int IDM_VS_CTXT_PROJECTREFERENCE           = 0x04A7;
            public const int IDM_VS_CTXT_SHAREDPROJECTREFERENCE     = 0x04A8;
            public const int IDM_VS_CTXT_FRAMEWORKREFERENCE_GROUP   = 0x04A9;
            public const int IDM_VS_CTXT_FRAMEWORKREFERENCE         = 0x04AA;
            public const int IDM_VS_CTXT_ANALYZERREFERENCE_GROUP    = 0x04AB;
            public const int IDM_VS_CTXT_ANALYZERREFERENCE          = 0x04AC;
            public const int IDM_VS_CTXT_SDKREFERENCE_GROUP         = 0x04AD;
            public const int IDM_VS_CTXT_SDKREFERENCE               = 0x04AE;
        }

        public bool TryGetContextMenu(IProjectTree projectItem, out Guid menuCommandGuid, out int menuCommandId)
        {
            Requires.NotNull(projectItem, nameof(projectItem));

            if (projectItem.Flags.Contains(DependencyTreeFlags.DependenciesRootNode))
            {
                menuCommandId = Menus.IDM_VS_CTXT_REFERENCEROOT;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.TargetNode))
            {
                menuCommandId = Menus.IDM_VS_CTXT_DEPENDENCYTARGET;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.AssemblyDependencyGroup))
            {
                menuCommandId = Menus.IDM_VS_CTXT_REFERENCE_GROUP;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.AssemblyDependency))
            {
                menuCommandId = Menus.IDM_VS_CTXT_REFERENCE;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.PackageDependencyGroup))
            {
                menuCommandId = Menus.IDM_VS_CTXT_PACKAGEREFERENCE_GROUP;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.PackageDependency))
            {
                menuCommandId = Menus.IDM_VS_CTXT_PACKAGEREFERENCE;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.ComDependencyGroup))
            {
                menuCommandId = Menus.IDM_VS_CTXT_COMREFERENCE_GROUP;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.ComDependency))
            {
                menuCommandId = Menus.IDM_VS_CTXT_COMREFERENCE;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.ProjectDependencyGroup))
            {
                menuCommandId = Menus.IDM_VS_CTXT_PROJECTREFERENCE_GROUP;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.ProjectDependency))
            {
                menuCommandId = Menus.IDM_VS_CTXT_PROJECTREFERENCE;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.SharedProjectDependency))
            {
                menuCommandId = Menus.IDM_VS_CTXT_SHAREDPROJECTREFERENCE;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.AnalyzerDependencyGroup))
            {
                menuCommandId = Menus.IDM_VS_CTXT_ANALYZERREFERENCE_GROUP;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.AnalyzerDependency))
            {
                menuCommandId = Menus.IDM_VS_CTXT_ANALYZERREFERENCE;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.FrameworkDependencyGroup))
            {
                menuCommandId = Menus.IDM_VS_CTXT_FRAMEWORKREFERENCE_GROUP;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.FrameworkDependency))
            {
                menuCommandId = Menus.IDM_VS_CTXT_FRAMEWORKREFERENCE;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.SdkDependencyGroup))
            {
                menuCommandId = Menus.IDM_VS_CTXT_SDKREFERENCE_GROUP;
            }
            else if (projectItem.Flags.Contains(DependencyTreeFlags.SdkDependency))
            {
                menuCommandId = Menus.IDM_VS_CTXT_SDKREFERENCE;
            }
            else
            {
                menuCommandGuid = default;
                menuCommandId = default;
                return false;
            }

            menuCommandGuid = VSConstants.CMDSETID.ShellMainMenu_guid;
            return true;
        }

        public bool TryGetMixedItemsContextMenu(IEnumerable<IProjectTree> projectItems, out Guid menuCommandGuid, out int menuCommandId)
        {
            Requires.NotNull(projectItems, nameof(projectItems));

            menuCommandGuid = default;
            menuCommandId = default;

            // If there are multiple items, and any item is a group node, suppress the context menu altogether

            int count = 0;
            bool containsProhibited = false;

            foreach (IProjectTree item in projectItems)
            {
                count++;

                if (!containsProhibited)
                {
                    if (item.Flags.Contains(DependencyTreeFlags.DependencyGroup) ||
                        item.Flags.Contains(DependencyTreeFlags.TargetNode) ||
                        item.Flags.Contains(DependencyTreeFlags.DependenciesRootNode))
                    {
                        containsProhibited = true;
                    }
                }

                if (containsProhibited && count > 1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
