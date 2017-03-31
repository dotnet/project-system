// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

/* NYI

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
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ManagedInterfaces.ProjectDesigner;
using System.ComponentModel.Design;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    class PropertyPageSiteFakeWithUndo : PropertyPageSiteFake, IVsProjectDesignerPageSite
    {
        public PropertyPageSiteFakeWithUndo(IPropertyPageSiteOwner view, OleInterop.IPropertyPage page)
            : base(view, page)
        {
        }


        #region IVsProjectDesignerPageSite Members

        System.ComponentModel.Design.DesignerTransaction IVsProjectDesignerPageSite.GetTransaction(string description)
        {
            DesignerTransaction transaction = new DesignerTransactionFake();
            return transaction;
        }

        void IVsProjectDesignerPageSite.OnPropertyChanged(string propertyName, System.ComponentModel.PropertyDescriptor propertyDescriptor, object oldValue, object newValue)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void IVsProjectDesignerPageSite.OnPropertyChanging(string propertyName, System.ComponentModel.PropertyDescriptor propertyDescriptor)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
*/
