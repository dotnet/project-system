// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Editors.PropertyPages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages
{
    class StandardTestsForApplicationPropPageVBBaseDescendentPropertyPages
    {
        ApplicationPropPageVBBase _page;
        Microsoft_VisualStudio_Editors_PropertyPages_ApplicationPropPageVBBaseAccessor _accessor;

        public StandardTestsForApplicationPropPageVBBaseDescendentPropertyPages(ApplicationPropPageVBBase page)
        {
            _page = page;
            _accessor = new Microsoft_VisualStudio_Editors_PropertyPages_ApplicationPropPageVBBaseAccessor(_page);
        }

        public void TestIconComboboxIsPopulated()
        {
            Assert.AreEqual("&Icon:", _page.m_CommonControls.IconLabel.Text);
            CollectionAssert.AreEqual(
                new string[] { "(Default Icon)", "<Browse...>" },
                _page.m_CommonControls.IconCombobox.Items);
        }

    }
}
