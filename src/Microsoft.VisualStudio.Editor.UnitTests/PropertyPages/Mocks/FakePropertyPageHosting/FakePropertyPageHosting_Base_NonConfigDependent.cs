using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Editors.PropertyPages;
using Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks;
using Microsoft.VisualStudio.Editors.ApplicationDesigner;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks.ProjectFlavor;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages
{
    class FakePropertyPageHosting_NonConfigDependent : FakePropertyPageHosting_Base
    {
        public override void InitializePageForUnitTests(PropPageUserControlBase page)
        {
            InitializePageForUnitTests(page, null);
        }

        public delegate void BeforeSetObjectsDelegate();
        public void InitializePageForUnitTests(PropPageUserControlBase page, BeforeSetObjectsDelegate beforeSetObjects)
        {
            if (Fake_dte == null)
            {
                Fake_dte = new DTEFake();
            }

            if (Fake_hierarchy == null)
            {
                Fake_hierarchy = new VsHierarchyFake(Fake_dte);
            }

            if (Fake_propertyPageSiteOwner == null) {
                Fake_propertyPageSiteOwner = new PropertyPageSiteOwnerFake();
            }

            if (Fake_comPropertyPage == null) {
                Fake_comPropertyPage = new ComPropertyPageFake(page);
            }

            if (Fake_site == null)
            {
                Fake_site = new PropertyPageSiteFake((IPropertyPageSiteOwner)Fake_propertyPageSiteOwner, (IPropertyPage)Fake_comPropertyPage);
            }

            if (Fake_objects == null)
            {
                Fake_objects = new object[] { Fake_hierarchy.Fake_projectProperties };
            }

            ((IPropertyPageInternal)page).SetPageSite((IPropertyPageSiteInternal)Fake_site);

            if (beforeSetObjects != null)
            {
                beforeSetObjects.Invoke();
            }

            page.SetObjects(Fake_objects);
        }


        protected override void Fake_EnsureProjectExtenderAdded()
        {
            if (!Fake_site.Fake_objectExtenders.Fake_RegisteredExtenders.ContainsKey(ProjectExtenderName))
            {
                Fake_site.Fake_objectExtenders.Fake_RegisteredExtenders.Add(
                    ProjectExtenderName, 
                    new ProjectExtenderFake());
            }
        }
    }
}
