using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.OLE;
using Microsoft.VisualStudio.Editors.ApplicationDesigner;
using Microsoft.VisualStudio.Editors.PropertyPages;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;
using System.Windows.Forms.Design;
using OleInterop = Microsoft.VisualStudio.OLE.Interop;

using OLEInterop = Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using Microsoft.VisualStudio.Editors.Interop;
using Microsoft.VisualStudio.TestTools.MockObjects;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    public interface IMsgBox
    {
        void DsMsgBox(Exception ex, string helpLink);
    }

    class PropertyPageSiteOwnerFake : IPropertyPageSiteOwner, IDisposable
    {
        public class MsgBoxControl : SequenceMock<IMsgBox>
        {
            public void ExpectMsgBox(Type exceptionType, string helpLink)
            {
                AddExpectation("DsMsgBox",
                    new object[] { MockConstraint.IsInstanceOfType<Exception>(exceptionType), helpLink });

            }
        }

        public MsgBoxControl Fake_msgBoxes = new MsgBoxControl();
        private bool isDisposed;

        ~PropertyPageSiteOwnerFake()
        {
            if (!isDisposed)
            {
                Debug.Fail("PropertyPageSiteOwnerFake was not disposed");
            }
        }

        #region IPropertyPageSiteOwner Members

        void IPropertyPageSiteOwner.DelayRefreshDirtyIndicators()
        {
            //NYI
        }

        void IPropertyPageSiteOwner.DsMsgBox(Exception ex, string helpLink)
        {
            Fake_msgBoxes.Instance.DsMsgBox(ex, helpLink);
        }

        uint IPropertyPageSiteOwner.GetLocaleID()
        {
            return 1033;
        }

        object IPropertyPageSiteOwner.GetService(Type ServiceType)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            isDisposed = true;
            Fake_msgBoxes.Verify();
        }

        #endregion
    }
}
