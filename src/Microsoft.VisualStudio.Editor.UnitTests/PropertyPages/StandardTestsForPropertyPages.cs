// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Editors.PropertyPages;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages
{
    /// <summary>
    ///  Standard tests to run for all property pages
    /// </summary>
    class StandardTestsForPropertyPages
    {
        PropPageUserControlBase _page;
        Microsoft_VisualStudio_Editors_PropertyPages_PropPageUserControlBaseAccessor _accessor;

        public StandardTestsForPropertyPages(PropPageUserControlBase page)
        {
            _page = page;
            _accessor = new Microsoft_VisualStudio_Editors_PropertyPages_PropPageUserControlBaseAccessor(_page);
        }

        public void RunStandardTests()
        {
            StandardTest_SetObjectsNull();
            StandardTest_SetObjectsEmpty();
        }

        private void StandardTest_SetObjectsNull()
        {
            _page.SetObjects(null);
            VerifyAfterEmptyOrNullSetObjects();
        }

        private void VerifyAfterEmptyOrNullSetObjects()
        {
            Assert.IsNull(_accessor.m_Objects);
            Assert.IsNull(_accessor.m_ExtendedObjects);
            Assert.IsNull(_accessor.m_DTEProject);
            Assert.IsNull(_accessor.m_DTE);
            Assert.IsNull(_accessor.m_ProjectPropertiesObject);
            Assert.IsNull(_accessor.m_ServiceProvider);
            Assert.IsNull(_accessor.m_Site);
            Assert.IsNull(_accessor.m_CachedRawPropertiesSuperset);
        }

        private void StandardTest_SetObjectsEmpty()
        {
            _page.SetObjects(new object[] { });
            VerifyAfterEmptyOrNullSetObjects();
        }

    }
}
