// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Editors.PropertyPages;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    internal class ComPropertyPageFake : IPropertyPage2
    {
        internal IPropertyPageSite Fake_site;
        internal IPropertyPageInternal Fake_realPage;

        public ComPropertyPageFake(IPropertyPageInternal realPage)
        {
            Fake_realPage = realPage;
        }

        #region IPropertyPage2 Members

        public void Activate(IntPtr hWndParent, RECT[] pRect, int bModal)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Apply()
        {
            Fake_realPage.Apply();
        }

        public void Deactivate()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void EditProperty(int DISPID)
        {
            Fake_realPage.EditProperty(DISPID);
        }

        public void GetPageInfo(PROPPAGEINFO[] pPageInfo)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Help(string pszHelpDir)
        {
            Fake_realPage.Help(pszHelpDir);
        }

        public int IsPageDirty()
        {
            if (Fake_realPage.IsPageDirty())
                return VSConstants.S_OK;
            else
                return VSConstants.S_FALSE;
        }

        public void Move(RECT[] pRect)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void SetObjects(uint cObjects, object[] ppunk)
        {
            Fake_realPage.SetObjects(ppunk);
        }

        public void SetPageSite(IPropertyPageSite pPageSite)
        {
            Fake_site = pPageSite;
        }

        public void Show(uint nCmdShow)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int TranslateAccelerator(MSG[] pMsg)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IPropertyPage Members


        int IPropertyPage.Apply()
        {
            Fake_realPage.Apply();
            return VSConstants.S_OK;
        }

        #endregion
    }
}
