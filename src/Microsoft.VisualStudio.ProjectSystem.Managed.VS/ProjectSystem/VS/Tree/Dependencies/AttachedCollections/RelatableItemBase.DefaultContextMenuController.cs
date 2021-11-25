// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    public abstract partial class RelatableItemBase
    {
        internal sealed class MenuController : IContextMenuController
        {
            private readonly Guid _menuGuid;
            private readonly int _menuId;

            public MenuController(Guid menuGuid, int menuId)
            {
                _menuGuid = menuGuid;
                _menuId = menuId;
            }

            public bool ShowContextMenu(IEnumerable<object> items, Point location)
            {
                bool shouldShowMenu = items.All(item => item is IRelatableItem);

                if (shouldShowMenu)
                {
                    if (Package.GetGlobalService(typeof(SVsUIShell)) is IVsUIShell shell)
                    {
                        Guid guidContextMenu = _menuGuid;

                        int result = shell.ShowContextMenu(
                            dwCompRole: 0,
                            rclsidActive: ref guidContextMenu,
                            nMenuId: _menuId,
                            pos: new[] { new POINTS { x = (short)location.X, y = (short)location.Y } },
                            pCmdTrgtActive: null);

                        return ErrorHandler.Succeeded(result);
                    }
                }

                return false;
            }
        }
    }
}
