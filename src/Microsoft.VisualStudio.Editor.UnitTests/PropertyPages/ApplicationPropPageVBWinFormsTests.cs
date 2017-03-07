using System;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Editors;
using Microsoft.VisualStudio.Editors.DesignerFramework;
using Microsoft.VisualStudio.Editors.ResourceEditor;

using Microsoft.VisualStudio.TestTools.MockObjects;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Editors.UnitTests.DesignerFramework;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;
using System.CodeDom.Compiler;
using VB = Microsoft.VisualBasic;
using Microsoft.CSharp;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.Editors.PropertyPages;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Editors.UnitTests.PropertyPages;
using Microsoft.VisualStudio.Editors.MyApplication;
using Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPageTests
{
    [TestClass]
    [CLSCompliant(false)]
    public class ApplicationPropPageVBWinFormsTests
    {
        #region Additional test attributes
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

        #region "Per-test goo"
        ApplicationPropPageVBWinForms _page;
        Microsoft_VisualStudio_Editors_PropertyPages_ApplicationPropPageVBWinFormsAccessor _accessor;
        Microsoft_VisualStudio_Editors_PropertyPages_ApplicationPropPageVBBaseAccessor _baseAccessor;
        StandardTestsForApplicationPropPageVBBaseDescendentPropertyPages _standardApplicationPageTests;
        FakePropertyPageHosting_NonConfigDependent _fakeHosting;

        [TestInitialize]
        public void TestInitialize()
        {
            _page = new ApplicationPropPageVBWinForms();
            _accessor = new Microsoft_VisualStudio_Editors_PropertyPages_ApplicationPropPageVBWinFormsAccessor(_page);
            _baseAccessor = new Microsoft_VisualStudio_Editors_PropertyPages_ApplicationPropPageVBBaseAccessor(_page);
            _standardApplicationPageTests = new StandardTestsForApplicationPropPageVBBaseDescendentPropertyPages(_page);
            _fakeHosting = new FakePropertyPageHosting_NonConfigDependent();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _fakeHosting.Dispose();
        }

        #endregion




        [TestMethod]
        public void StandardPropertyPageTests()
        {
            _fakeHosting.InitializePageForUnitTests(_page);
            StandardTestsForPropertyPages standardTests = new StandardTestsForPropertyPages(_page);
            standardTests.RunStandardTests();
        }

        [TestMethod]
        public void Constructor()
        {
            Assert.AreEqual(ComboBoxStyle.DropDownList, _accessor.StartupObjectComboBox.DropDownStyle);
        }

        [TestMethod]
        public void PopulateStartupObjectNotSupported()
        {
            //Run test code
            _accessor.PopulateStartupObject(false, true);
            Assert.AreEqual(1, _accessor.StartupObjectComboBox.Items.Count, "Expected only a None entry");
            Assert.AreEqual("(None)", _accessor.StartupObjectComboBox.Items[0], "Expected only a None entry");
            Assert.AreEqual(ComboBoxStyle.DropDownList, _accessor.StartupObjectComboBox.DropDownStyle);
        }

        #region Standard tests for ApplicationPropPageVBBase classes

        [TestMethod]
        public void Standard_IconComboboxIsPopulated()
        {
            _fakeHosting.InitializePageForUnitTests(_page);
            _standardApplicationPageTests.TestIconComboboxIsPopulated();
        }

        [TestMethod]
        public void Standard_RootNamespaceChange()
        {
            _page = new ApplicationPropPageVBWinFormsMock_OnRootNamespaceChanged();
            _fakeHosting.InitializePageForUnitTests(_page);

            _page.RootNamespaceTextBox.Text = "RootNamespace2";
            _page.OnPropertyChanged("RootNamespace", null, "RS1", "RS2");

            Assert.IsTrue(((ApplicationPropPageVBWinFormsMock_OnRootNamespaceChanged)_page).Fake_OnRootNamespaceChangedWasCalled,
                "OnRootNamespaceChanged wasn't called");
        }

        #endregion

        [TestMethod]
        public void PopulateStartupObjectNoFill()
        {
            _fakeHosting.InitializePageForUnitTests(_page);
            _page.m_fInsideInit = true;
            _accessor.UseApplicationFrameworkCheckBox.Checked = false;
            _page.m_fInsideInit = false;

            //Run test code
            _accessor.PopulateStartupObject(true, false);

            //Validation
            Assert.AreEqual(1, _accessor.StartupObjectComboBox.Items.Count, "Expected only the first entry for performance reasons");
            Assert.AreEqual("Sub Main", _accessor.StartupObjectComboBox.Items[0], "Expected only the first entry for performance reasons");
            Assert.AreEqual(ComboBoxStyle.DropDownList, _accessor.StartupObjectComboBox.DropDownStyle);
        }

        [TestMethod]
        public void PopulateStartupObject()
        {
            _fakeHosting.InitializePageForUnitTests(_page);
            _page.m_fInsideInit = true;
            _accessor.UseApplicationFrameworkCheckBox.Checked = false;
            _page.m_fInsideInit = false;

            //Run test code
            //UNDONE: some more entries
            _accessor.PopulateStartupObject(true, true);

            //Validation
            Assert.AreEqual(1, _accessor.StartupObjectComboBox.Items.Count);
            Assert.AreEqual("Sub Main", _accessor.StartupObjectComboBox.Items[0]);
            Assert.AreEqual(ComboBoxStyle.DropDownList, _accessor.StartupObjectComboBox.DropDownStyle);
        }

        [TestMethod]
        public void PopulateStartupObject_ApplicationFrameworkEnabled()
        {
            //Setup
            _fakeHosting.InitializePageForUnitTests(_page);
#if false
            _page.m_fInsideInit = true;
            _accessor.UseApplicationFrameworkCheckBox.Checked = true;
            _accessor.UpdateApplicationTypeUI();
            _page.m_fInsideInit = false;
#endif
            Assert.IsTrue(_accessor.MyApplicationPropertiesSupported);
            Assert.IsTrue(_accessor.MyApplicationFrameworkSupported());
            Assert.IsTrue(_accessor.MyApplicationFrameworkEnabled());

            //Run test code
            _accessor.PopulateStartupObject(true, true);

            //Validation
            Assert.AreEqual(2, _accessor.StartupObjectComboBox.Items.Count);
            Assert.AreEqual("Form1", _accessor.StartupObjectComboBox.Items[0]);
            Assert.AreEqual("Form2", _accessor.StartupObjectComboBox.Items[1]);
            Assert.AreEqual(ComboBoxStyle.DropDownList, _accessor.StartupObjectComboBox.DropDownStyle);
        }

        [TestMethod]
        public void CheckApplicationFrameworkEnabled()
        {
            //Setup
            _fakeHosting.InitializePageForUnitTests(_page);
            Assert.IsTrue(_accessor.UseApplicationFrameworkCheckBox.Checked);

            //Validation
            Assert.AreEqual(1, _accessor.StartupObjectComboBox.Items.Count);
            Assert.AreEqual("Form1", _accessor.StartupObjectComboBox.Items[0]);
            Assert.AreEqual(ComboBoxStyle.DropDownList, _accessor.StartupObjectComboBox.DropDownStyle);
        }

#if false
        [TestMethod]
        public void UncheckApplicationFrameworkEnabled()
        {
            //Setup
            InitializePageForUnitTests(_page);
            Assert.IsTrue(_accessor.UseApplicationFrameworkCheckBox.Checked);

            //Run test code
            _accessor.UseApplicationFrameworkCheckBox.Checked = false;

            //Validation
            Assert.AreEqual(1, _accessor.StartupObjectComboBox.Items.Count);
            Assert.AreEqual("Sub Main", _accessor.StartupObjectComboBox.Items[0]);
            Assert.AreEqual(ComboBoxStyle.DropDownList, _accessor.StartupObjectComboBox.DropDownStyle);
        }
#endif

        [TestMethod]
        public void CustomSubMain()
        {
            //Setup
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                MyApplicationPropertiesFake myapp = (MyApplicationPropertiesFake)_fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_properties["MyApplication"].Value;
                myapp.Fake_CustomSubMainRaw = true;
            });

            //Validation
            Assert.IsFalse(_accessor.UseApplicationFrameworkCheckBox.Checked);
            Assert.AreEqual(1, _accessor.StartupObjectComboBox.Items.Count);
            Assert.AreEqual("Sub Main", _accessor.StartupObjectComboBox.Items[0]);
            Assert.AreEqual(ComboBoxStyle.DropDownList, _accessor.StartupObjectComboBox.DropDownStyle);
        }

        [TestMethod]
        public void ApplicationFrameworkEnabled()
        {
            //Setup
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                MyApplicationPropertiesFake myapp = (MyApplicationPropertiesFake)_fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_properties["MyApplication"].Value;
                myapp.Fake_CustomSubMainRaw = false;
            });

            //Validation
            Assert.IsTrue(_accessor.UseApplicationFrameworkCheckBox.Checked);
            Assert.AreEqual(1, _accessor.StartupObjectComboBox.Items.Count);
            Assert.AreEqual("Form1", _accessor.StartupObjectComboBox.Items[0]);
            Assert.AreEqual(ComboBoxStyle.DropDownList, _accessor.StartupObjectComboBox.DropDownStyle);
        }

        [TestMethod]
        public void ApplicationTypesPopulatedDefault()
        {
            _fakeHosting.InitializePageForUnitTests(_page,
                delegate()
                {
                    _fakeHosting.Fake_hierarchy.Fake_supportedMyApplicationTypes = "WindowsApp;WindowsClassLib;CommandLineApp;WindowsService;WebControl";
                });

            CollectionAssert.AreEqual(
                new object[] { "Windows Forms Application",
                    "Class Library",
                    "Console Application",
                    "Windows Service",
                    "Web Control Library" },
                Mocks.Utility.GetToStringValuesOfCollection(_page.ApplicationTypeComboBox.Items));
        }

        [TestMethod]
        public void ApplicationTypesPopulatedUnsupportedTypesIncluded()
        {
            _fakeHosting.InitializePageForUnitTests(_page,
                delegate()
                {
                    _fakeHosting.Fake_hierarchy.Fake_supportedMyApplicationTypes = "xx;WindowsApp;WindowsClassLib;yyy;CommandLineApp;;;;WindowsService;WebControl;zzz";
                });

            CollectionAssert.AreEqual(
                new object[] { "Windows Forms Application",
                    "Class Library",
                    "Console Application",
                    "Windows Service",
                    "Web Control Library" },
                Mocks.Utility.GetToStringValuesOfCollection(_page.ApplicationTypeComboBox.Items));
        }

        [TestMethod]
        public void ApplicationTypesPopulatedListFiltered()
        {
            _fakeHosting.InitializePageForUnitTests(_page,
                delegate()
                {
                    _fakeHosting.Fake_hierarchy.Fake_supportedMyApplicationTypes = "WindowsApp;CommandLineApp;WindowsService";
                });

            CollectionAssert.AreEqual(
                new object[] { "Windows Forms Application",
                    "Console Application",
                    "Windows Service" },
                Mocks.Utility.GetToStringValuesOfCollection(_page.ApplicationTypeComboBox.Items));
        }
    }



    //NOTE: Due to some sort of C# or VSTS bug, tests after this class will not be
    //  seen by VSTS.  (Need repro to file bug.)
    #region "Mocks"

    class ApplicationPropPageVBWinFormsMock_OnRootNamespaceChanged : ApplicationPropPageVBWinForms
    {
        public bool Fake_OnRootNamespaceChangedWasCalled;
        protected override void OnRootNamespaceChanged(EnvDTE.Project Project, System.IServiceProvider ServiceProvider, string OldRootNamespace, string NewRootNamespace)
        {
            Fake_OnRootNamespaceChangedWasCalled = true;
        }
    }

    #endregion
}

