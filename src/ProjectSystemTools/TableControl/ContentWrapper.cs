﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.TableControl
{
    internal class ContentWrapper : Border
    {
        private readonly int _contextMenuId;

        internal ContentWrapper(int contextMenuId)
        {
            _contextMenuId = contextMenuId;
        }

        protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e) => OpenContextMenu();

        internal static bool PreProcessMessage(ref Message m, IOleCommandTarget cmdTarget) =>
            m.Msg == 0x007B &&
            ErrorHandler.Succeeded(cmdTarget.Exec(VSConstants.VSStd2K, (uint)VSConstants.VSStd2KCmdID.SHOWCONTEXTMENU, 0, IntPtr.Zero, IntPtr.Zero));

        internal void OpenContextMenu()
        {
            if (_contextMenuId == -1)
            {
                return;
            }

            Guid guidContextMenu = ProjectSystemToolsPackage.UIGuid;
            Point location = GetContextMenuLocation();
            POINTS[] locationPoints = new[] { new POINTS { x = (short)location.X, y = (short)location.Y } };

            // Show context menu blocks, so we need to yield out of this method
            // for e.Handled to be noticed by WPF
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
            Dispatcher.BeginInvoke(new Action(() =>
#pragma warning restore VSTHRD001 
                ProjectSystemToolsPackage.VsUIShell.ShowContextMenu(0, ref guidContextMenu, _contextMenuId,
                    locationPoints, pCmdTrgtActive: null)));
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
