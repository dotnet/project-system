// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Editors.PropertyPages;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.MockObjects;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;
using Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks;
using System.Windows.Forms;
using System.Reflection;
using Microsoft.VisualStudio.Editors.ApplicationDesigner;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages
{
    /// <summary>
    /// UNDONE
    /// This provides a base set of support for implementing complex unit test for property pages.
    ///   Actually, they really support something more than traditional unit tests, it's more like
    ///   running the full property page inside a faked VS environment, so that user interactions,
    ///   property changes, SCC, errors, undo/redo, etc., can all be tested easily in the unit
    ///   test environment.
    ///   
    /// Your property page test should inherit from either BaseTestClassForPropertyPages_ConfigDependent
    ///   or BaseTestClassForPropertyPages_NonConfigDependent.
    ///   
    /// See ApplicationPropPageVBWPFTests for examples of how to put this together and how to
    ///   modify the expectations and functionality of the supporting mocks/fakes.
    /// </summary>
    abstract class FakePropertyPageHosting_Base : IDisposable
    {
        #region Per-TestMethod fields

        public object[] Fake_objects;
        public VsHierarchyFake Fake_hierarchy;
        public PropertyPageSiteFake Fake_site;
        public DTEFake Fake_dte;
        public PropertyPageSiteOwnerFake Fake_propertyPageSiteOwner;
        public ComPropertyPageFake Fake_comPropertyPage; //of type ComPropertyPageFake - Can't define it as public because that would be exposing a friend interface via a public class, sigh.

        public ProjectPropertiesFake Fake_projectProperties
        {
            get
            {
                return Fake_hierarchy.Fake_projectProperties;
            }
        }

        #endregion

        abstract public void InitializePageForUnitTests(PropPageUserControlBase page);

        #region Support for fake user events

        public void Fake_FireControlEvent(Control control, string eventName, EventArgs eventArgs)
        {
            MethodInfo onEventMethod = control.GetType().GetMethod("On" + eventName, System.Reflection.BindingFlags.NonPublic | BindingFlags.Instance);
            if (onEventMethod == null)
            {
                MessageBox.Show("Couldn't find OnXXX method for event " + eventName);
                throw new InvalidOperationException();
            }
            onEventMethod.Invoke(control, new object[] { eventArgs });
        }

        /// <summary>
        /// Sets a control's text on the property page, making sure that the normal
        ///   events get fired (or at least that the page thinks they get fired), to
        ///   mimic a user's having typed in text and committed it.
        /// </summary>
        /// <param name="textbox"></param>
        /// <param name="value"></param>
        public void Fake_TypeTextIntoControl(TextBox textbox, string value)
        {
            Fake_TypeTextIntoControl(textbox, value, false);
        }

        /// <summary>
        /// Sets a control's text on the property page, making sure that the normal
        ///   events get fired (or at least that the page thinks they get fired), to
        ///   mimic a user's having typed in text and committed it.
        /// </summary>
        /// <param name="textbox"></param>
        /// <param name="value"></param>
        public void Fake_TypeTextIntoControl(TextBox textbox, string value, bool doNotCommit)
        {
            textbox.Text = value; // This will fire TextChanged

            if (!doNotCommit)
            {
                // To make the control commit, we need to fire LostFocus (we can't do this
                //   directly because you can't set focus to non-visible controls.
                Fake_FireControlEvent(textbox, "LostFocus", EventArgs.Empty);
            }
        }

        public void Fake_SelectItemInComboBox(ComboBox combobox, string itemDisplayText)
        {
            foreach (object item in combobox.Items)
            {
                if (item.ToString().Equals(itemDisplayText, StringComparison.OrdinalIgnoreCase))
                {
                    combobox.SelectedItem = item;
                    Fake_FireControlEvent(combobox, "SelectionChangeCommitted", EventArgs.Empty);
                    return;
                }
            }

            throw new Exception("Couldn't find specified entry '" + itemDisplayText + "'in the combobox");
        }

        public void Fake_DropDownCombobox(ComboBox combobox)
        {
            Fake_FireControlEvent(combobox, "DropDown", EventArgs.Empty);
        }

        public string[] Fake_GetDisplayTextOfItemsInCombobox(ComboBox combobox)
        {
            List<string> items = new List<string>();
            foreach (object item in combobox.Items)
            {
                items.Add(item.ToString());
            }

            return items.ToArray();
        }

        /// <summary>
        /// Create an expectation that a messagebox is shown with the given exception type
        /// </summary>
        /// <param name="exceptionType"></param>
        /// <param name="helpLink"></param>
        public void Fake_ExpectMsgBox(Type exceptionType, string helpLink)
        {
            ((PropertyPageSiteOwnerFake)Fake_propertyPageSiteOwner).Fake_msgBoxes.ExpectMsgBox(
                exceptionType, helpLink);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (Fake_propertyPageSiteOwner != null && Fake_propertyPageSiteOwner is IDisposable)
            {
                ((IDisposable)Fake_propertyPageSiteOwner).Dispose();
            }
        }

        #endregion

        #region Project flavor support

        protected string ProjectExtenderName = "MyFakeProjectExtender";

        protected abstract void Fake_EnsureProjectExtenderAdded();

        public void Fake_Flavor_SetAllPropertiesToHidden()
        {
            Fake_EnsureProjectExtenderAdded();
            Fake_site.Fake_objectExtenders.Fake_RegisteredExtenders[ProjectExtenderName].PropertyFilterOptions.AllPropertiesAreHidden = true;
        }

        public void Fake_Flavor_SetPropertyToHidden(string propertyName)
        {
            Fake_EnsureProjectExtenderAdded();
            Fake_site.Fake_objectExtenders.Fake_RegisteredExtenders[ProjectExtenderName]
                .PropertyFilterOptions.HiddenPropertyNames.Add(propertyName);
        }

        public void Fake_Flavor_SetAllPropertiesToReadOnly()
        {
            Fake_EnsureProjectExtenderAdded();
            Fake_site.Fake_objectExtenders.Fake_RegisteredExtenders[ProjectExtenderName]
                .PropertyFilterOptions.AllPropertiesAreReadOnly = true;
        }

        public void Fake_Flavor_SetPropertyToReadOnly(string propertyName)
        {
            Fake_EnsureProjectExtenderAdded();
            Fake_site.Fake_objectExtenders.Fake_RegisteredExtenders[ProjectExtenderName]
                .PropertyFilterOptions.ReadOnlyPropertyNames.Add(propertyName);
        }

        #endregion

    }

}
