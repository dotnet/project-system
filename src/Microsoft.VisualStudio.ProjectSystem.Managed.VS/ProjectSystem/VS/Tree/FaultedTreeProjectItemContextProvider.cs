// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree
{
    [Export(typeof(IProjectItemContextMenuProvider))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    [Order(1)] // Ahead of CPS provider
    internal class FaultedTreeProjectItemContextProvider : IProjectItemContextMenuProvider
    {
        public bool TryGetContextMenu(IProjectTree projectItem, out Guid menuCommandGuid, out int menuCommandId)
        {
            // TODO: Switch to ProjectTreeFlags.AdditionalFlags.FaultTree when we take updated CPS bits.
            // https://github.com/dotnet/roslyn-project-system/issues/1340
            if (projectItem.Root.Flags.Contains("FaultTree"))
            {
                menuCommandGuid = VsMenus.guidSHLMainMenu;
                menuCommandId = VsMenus.IDM_VS_CTXT_PROJNODE;
                return true;
            }
            else
            {
                menuCommandGuid = Guid.Empty;
                menuCommandId = 0;
                return false;
            }
        }

        public bool TryGetMixedItemsContextMenu(IEnumerable<IProjectTree> projectItems, out Guid menuCommandGuid, out int menuCommandId)
        {
            // For mixed items, we let the default implementation handle it.
            menuCommandGuid = Guid.Empty;
            menuCommandId = 0;
            return false;
        }
    }
}
