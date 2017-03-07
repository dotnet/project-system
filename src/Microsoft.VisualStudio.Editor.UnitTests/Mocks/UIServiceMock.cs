using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.MockObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    class UIServiceMock : SequenceMock<IUIService>
    {
        private IWin32Window m_ownerWindow = new Control();

        public UIServiceMock()
        {
            Implement("GetDialogOwnerWindow", m_ownerWindow);
        }

        public void AddExpectationShowMessageBox(string message, MessageBoxButtons buttons, DialogResult returnValue)
        {
            //Implement("ShowMessageBox",
            //    new object[] {
            //    0,
            //    Guid.Empty,
            //    MockConstraint.IsAnything<string>(),
            //    pszText,
            //    pszHelpLink,
            //    0,
            //    msgbtn, 
            //    MockConstraint.IsAnything<OLEMSGDEFBUTTON>(), 
            //    MockConstraint.IsAnything<OLEMSGICON>(),
            //    0,
            //    0},
            //    new object[] { returnedResult });
            //Implement("ShowMessageBox",
            //    new object[] {
            //    0,
            //    Guid.Empty,
            //    MockConstraint.IsAnything<string>(),
            //    MockConstraint.IsAnything<string>(),
            //    MockConstraint.IsAnything<string>(),
            //    0,
            //    msgbtn, 
            //    MockConstraint.IsAnything<OLEMSGDEFBUTTON>(), 
            //    MockConstraint.IsAnything<OLEMSGICON>(),
            //    0,
            //    0},
            //    new object[] { returnedResult });

            Implement("ShowMessage",
                new object[] {
                    message, 
                    MockConstraint.IsAnything<string>(),
                    buttons},
                returnValue);
        }
    }
}
