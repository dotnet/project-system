using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Editors.PropertyPages;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.MockObjects;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;
using Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages
{
    [TestClass]
    [CLSCompliant(false)]
    public class AdvCompilerSettingsPropPageTests
    {
        #region Initialization goo
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //

        #endregion

        [TestMethod]
        public void Constructor()
        {
            AdvCompilerSettingsPropPage page = new AdvCompilerSettingsPropPage();
        }

        [TestMethod]
        public void ConstructorDebugInfoComboboxDropContents()
        {
            AdvCompilerSettingsPropPage page = new AdvCompilerSettingsPropPage();

            CollectionAssert.AreEqual(
                new object[] { "None", "Full", "pdb-only" },
                page.DebugInfoComboBox.Items);
        }

        private List<Control> GetAllDescendents(Control parent)
        {
            List<Control> controls = new List<Control>();
            foreach (Control child in parent.Controls)
            {
                controls.Add(child);
                controls.AddRange(GetAllDescendents(child));
            }

            return controls;
        }

        private Control FindControl(Control parent, string name)
        {
            if (parent.Name.Equals(name, StringComparison.Ordinal))
            {
                return parent;
            }

            foreach (Control child in parent.Controls)
            {
                Control cFound = FindControl(child, name);
                if (cFound != null)
                    return cFound;
            }

            return null;
        }

        private void VerifyDialogExpandsOnLongControlText(string controlName)
        {
            AdvCompilerSettingsPropPage page = new AdvCompilerSettingsPropPage();
            Size preferredSizeOriginal = page.PreferredSize;
            Control c = FindControl(page, controlName);
            Assert.IsNotNull("Couldn't find control " + controlName);
            c.Text = new string('x', 500);
            Assert.IsTrue(page.PreferredSize.Width > preferredSizeOriginal.Width,
                "Dialog did not expand when the control's text length was increased.  ControlName = " + controlName);
        }

        [TestMethod]
        public void DialogExpandsOnLongControlText()
        {
            AdvCompilerSettingsPropPage page = new AdvCompilerSettingsPropPage();
            Assert.IsTrue(GetAllDescendents(page).Contains(page.DefineDebug), "GetAllDescendents isn't working");

            foreach (Control c in GetAllDescendents(page))
            {
                if (!string.IsNullOrEmpty(c.Text))
                {
                    VerifyDialogExpandsOnLongControlText(c.Name);
                }
            }
        }
    }
}
