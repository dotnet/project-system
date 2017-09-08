// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
{
    internal class ContentWrapper : Border
    {
        private readonly int _contextMenuId;

        internal ContentWrapper(int contextMenuId)
        {
            _contextMenuId = contextMenuId;
        }

        protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e) => OpenContextMenu();

        // Handles WM_CONTEXTMENU message to invoke context menu.
        internal static bool PreProcessMessage(ref Message m, IOleCommandTarget cmdTarget) =>
            m.Msg == 0x007B &&
            ErrorHandler.Succeeded(cmdTarget.Exec(VSConstants.VSStd2K, (uint)VSConstants.VSStd2KCmdID.SHOWCONTEXTMENU, 0, IntPtr.Zero, IntPtr.Zero));

        internal void OpenContextMenu()
        {
            var guidContextMenu = ProjectSystemToolsPackage.UIGuid;
            var location = GetContextMenuLocation();
            var locationPoints = new[] { new POINTS { x = (short)location.X, y = (short)location.Y } };

            // Show context menu blocks, so we need to yield out of this method
            // for e.Handled to be noticed by WPF
            Dispatcher.BeginInvoke(new Action(() => ProjectSystemToolsPackage.VsUIShell.ShowContextMenu(0, ref guidContextMenu, _contextMenuId, locationPoints, pCmdTrgtActive: null)));
        }

        // Default to the bottom-left corner of the control for the position of contect menu invoked from keyboard
        private Point GetKeyboardContextMenuAnchorPoint() => PointToScreen(new Point(0, RenderSize.Height));

        // Get the current mouse position and convert it to screen coordinates as the shell expects a screen position
        private Point GetContextMenuLocation() =>
            InputManager.Current.MostRecentInputDevice is KeyboardDevice
                ? GetKeyboardContextMenuAnchorPoint()
                : PointToScreen(Mouse.GetPosition(this));
    }
}
