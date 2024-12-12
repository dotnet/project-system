// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;

public abstract partial class RelatableItemBase
{
    /// <summary>
    /// Creates a <see cref="IContextMenuController"/> for use in overrides of <see cref="ContextMenuController"/>.
    /// </summary>
    public static IContextMenuController CreateContextMenuController(Guid menuGuid, int menuId) => new MenuController(menuGuid, menuId);

    internal sealed class MenuController(Guid menuGuid, int menuId) : IContextMenuController
    {
        public static ImmutableArray<IRelatableItem> CurrentItems { get; private set; } = [];

        public bool ShowContextMenu(IEnumerable<object> items, Point location)
        {
            ImmutableArray<IRelatableItem>? relatableItems = GetItems();

            if (relatableItems is null)
            {
                return false;
            }

            CurrentItems = relatableItems.Value;

            try
            {
                return ShowContextMenu();
            }
            finally
            {
                CurrentItems = [];
            }

            ImmutableArray<IRelatableItem>? GetItems()
            {
                ImmutableArray<IRelatableItem>.Builder? builder = null;

                foreach (object item in items)
                {
                    if (item is IRelatableItem relatableItem)
                    {
                        builder ??= ImmutableArray.CreateBuilder<IRelatableItem>();
                        builder.Add(relatableItem);
                    }
                    else
                    {
                        return null;
                    }
                }

                if (builder is null)
                {
                    return null;
                }

                return builder.ToImmutable();
            }

            bool ShowContextMenu()
            {
                if (Package.GetGlobalService(typeof(SVsUIShell)) is IVsUIShell shell)
                {
                    Guid guidContextMenu = menuGuid;

                    int result = shell.ShowContextMenu(
                        dwCompRole: 0,
                        rclsidActive: ref guidContextMenu,
                        nMenuId: menuId,
                        pos: [new POINTS { x = (short)location.X, y = (short)location.Y }],
                        pCmdTrgtActive: null);

                    return ErrorHandler.Succeeded(result);
                }

                return false;
            }
        }
    }
}
