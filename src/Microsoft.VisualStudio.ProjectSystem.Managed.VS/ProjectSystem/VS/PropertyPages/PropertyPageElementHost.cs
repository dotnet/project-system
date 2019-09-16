// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal class PropertyPageElementHost : ElementHost
    {
        private const int WM_KEYFIRST = 0x0100;
        private const int WM_KEYLAST = 0x0108;

        public override bool PreProcessMessage(ref Message msg)
        {
            if (msg.Msg >= WM_KEYFIRST && msg.Msg <= WM_KEYLAST)
            {
                var filterKeys = (IVsFilterKeys2)ServiceProvider.GlobalProvider.GetService(typeof(SVsFilterKeys));
                var oleMSG = new OLE.Interop.MSG() { hwnd = msg.HWnd, lParam = msg.LParam, wParam = msg.WParam, message = (uint)msg.Msg };

                //Ask the shell to do the command mapping for us and without firing off the command. We need to check if this command is one of the
                //supported commands first before actually firing the command.
                filterKeys.TranslateAcceleratorEx(new OLE.Interop.MSG[] { oleMSG },
                                                  (uint)(__VSTRANSACCELEXFLAGS.VSTAEXF_NoFireCommand | __VSTRANSACCELEXFLAGS.VSTAEXF_UseGlobalKBScope | __VSTRANSACCELEXFLAGS.VSTAEXF_AllowModalState),
                                                  0 /*scope count*/,
                                                  Array.Empty<Guid>() /*scopes*/,
                                                  out Guid cmdGuid,
                                                  out _,
                                                  out int fTranslated,
                                                  out _);

                if (ShouldRouteCommandBackToVS(cmdGuid, fTranslated == 1))
                {
                    return false;
                }
            }

            return base.PreProcessMessage(ref msg);
        }

        private static bool ShouldRouteCommandBackToVS(Guid cmdGuid, bool translated)
        {
            //Any command that wasn't translated by TranslateAcceleratorEx or has no VS handler in global scope should be routed to WPF
            if (!translated || cmdGuid == Guid.Empty)
            {
                return false;
            }

            //Allow VS to take over for anything that would be a common shell command
            //  (CTRL+Tab, CTRL+Shift+TAB, Shift+ALT+Enter, etc.)
            if (cmdGuid == VSConstants.GUID_VSStandardCommandSet97)
            {
                //If there's a GUID_VSStandardCommandSet97 command that should be handled by WPF instead, check for them and return false

                //Otherwise indicate that the command should be handled by VS
                return true;
            }

            //If there are additional commands that VS should be handling instead of WPF, check for them and return true here

            //Otherwise indicate that the command should be handled by WPF
            return false;
        }
    }
}
