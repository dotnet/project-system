using System;
using System.Text;
using System.Collections.Generic;
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
using Microsoft.VisualBasic;
using Microsoft.CSharp;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.Editors.PropertyPages;
using Microsoft.VisualStudio.Editors.UnitTests.PropertyPages;
using VSLangProj;
using System.ComponentModel;
using Microsoft.VisualStudio.Editors.MyApplication;
using EnvDTE;
using System.Windows.Forms;
using System.Reflection;
using Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks;
using VSLangProj80;
using Microsoft.VisualStudio.Shell.Design.Serialization;
using System.IO;
using System.Xml;
using System.Diagnostics;
using Microsoft.VisualStudio.Editors.PropertyPages.WPF;

//CONSIDER: tests for undo/redo
//CONSIDER: tests for properties changed outside of the property page

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPageTests.WPF
{
    [TestClass]
    [CLSCompliant(false)]
    public class ApplicationPropPageVBWPFTests
    {
        private const string APPXAML_StartupUriNonEmpty =
            @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml' "
            + "StartupUri = \"Startup Page.xaml\"/>";
        private const string APPXAML_StartupUriMissing =
            @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml' "
            + "StartupUri = \"\"/>";
        private const string APPXAML_StartupUriEmpty =
            @"<Application xmlns='http://schemas.microsoft.com/presentation' xmlns:x='http://schemas.microsoft.com/xaml' "
            + "StartupUri = \"\"/>";

        #region Utilities

        internal AppDotXamlDocument CreateAppDotXamlDocument(string text)
        {
            Debug.Assert(_vsTextBufferFake == null, "CreateAppDotXamlDocument() has already been called");
            _vsTextBufferFake = new VsTextBufferFake(text);
            return CreateAppDotXamlDocument(_vsTextBufferFake);
        }

        static internal AppDotXamlDocument CreateAppDotXamlDocument(VsTextBufferFake vsTextBufferFake)
        {
            Debug.Assert(vsTextBufferFake != null);
            TextReader textReader = new StringReader(vsTextBufferFake.Fake_AllText);
            XmlTextReader xmlTextReader = new XmlTextReader(textReader);
            return new AppDotXamlDocument(vsTextBufferFake);
        }

        #endregion

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
        ApplicationPropPageVBWPFMock _page;
        Microsoft_VisualStudio_Editors_PropertyPages_WPF_ApplicationPropPageVBWPFAccessor _accessor;
        StandardTestsForApplicationPropPageVBBaseDescendentPropertyPages _standardApplicationPageTests;
        FakePropertyPageHosting_NonConfigDependent _fakeHosting;
        VsTextBufferFake _vsTextBufferFake = null;

        [TestInitialize]
        public void TestInitialize()
        {
            _page = new ApplicationPropPageVBWPFMock();
            _accessor = new Microsoft_VisualStudio_Editors_PropertyPages_WPF_ApplicationPropPageVBWPFAccessor(_page);
            _standardApplicationPageTests = new StandardTestsForApplicationPropPageVBBaseDescendentPropertyPages(_page);
            _fakeHosting = new FakePropertyPageHosting_NonConfigDependent();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Exception exReport = null;
            try
            {
                _fakeHosting.Dispose();
            }
            catch (Exception ex)
            {
                exReport = ex;
            }

            try
            {
                if (_vsTextBufferFake != null)
                    _vsTextBufferFake.Dispose();
            }
            catch (Exception ex)
            {
                exReport = ex;
            }

            try
            {
                _page.Dispose();
            }
            catch (Exception ex)
            {
                exReport = ex;
            }

            if (exReport != null)
                throw exReport;
        }

        #endregion

        #region Standard tests

        [TestMethod]
        public void StandardPropertyPageTests()
        {
            _fakeHosting.InitializePageForUnitTests(_page);
            StandardTestsForPropertyPages standardTests = new StandardTestsForPropertyPages(_page);
            standardTests.RunStandardTests();
        }

        #endregion

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
            _page = new ApplicationPropPageVBWPFMock();
            _fakeHosting.InitializePageForUnitTests(_page);

            _page.RootNamespaceTextBox.Text = "RootNamespace2";
            _page.OnPropertyChanged("RootNamespace", null, "RS1", "RS2");

            Assert.IsTrue(((ApplicationPropPageVBWPFMock)_page).Fake_OnRootNamespaceChangedWasCalled,
                "OnRootNamespaceChanged wasn't called");
        }

        #endregion

        #region All properties hidden/readonly

        [TestMethod]
        public void AllPropertiesHidden()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
                {
                    _fakeHosting.Fake_Flavor_SetAllPropertiesToHidden();
                });

            Assert.IsTrue(_accessor.GetPropertyControlData((int)VsProjPropId.VBPROJPROPID_RootNamespace).IsMissing, "Property should have been marked IsMissing");
            Assert.IsFalse(_page.RootNamespaceTextBox.Enabled, "Property control should have been disabled");
            Assert.IsTrue(_page.RootNamespaceTextBox.Visible, "Property control should have been visible");


            //CONSIDER: other properties
        }

        [TestMethod]
        public void AllPropertiesReadOnly()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
                {
                    _fakeHosting.Fake_Flavor_SetAllPropertiesToReadOnly();
                });

            Assert.IsFalse(_accessor.GetPropertyControlData((int)VsProjPropId.VBPROJPROPID_RootNamespace).IsMissing, "Property should not have been marked IsMissing");
            Assert.IsTrue(_accessor.GetPropertyControlData((int)VsProjPropId.VBPROJPROPID_RootNamespace).IsReadOnly, "Property should have been marked ReadOnly");
            Assert.IsTrue(_page.RootNamespaceTextBox.ReadOnly && _page.RootNamespaceTextBox.Enabled, "Textbox should have been enabled but read-only");
            Assert.IsTrue(_page.RootNamespaceTextBox.Visible, "Property control should have been visible");

            //CONSIDER: other properties
        }

        #endregion

        #region Application types

        [TestMethod]
        public void ApplicationTypesPopulatedDefault()
        {
            _fakeHosting.InitializePageForUnitTests(_page,
                delegate()
                {
                    _fakeHosting.Fake_hierarchy.Fake_supportedMyApplicationTypes = "WindowsApp;WindowsClassLib;CommandLineApp;WindowsService;WebControl";
                });

            CollectionAssert.AreEqual(
                new object[] { "WPF Application",
                    "WPF Class Library",
                    "WPF Console Application" },
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
                new object[] { "WPF Application",
                    "WPF Class Library",
                    "WPF Console Application" },
                Mocks.Utility.GetToStringValuesOfCollection(_page.ApplicationTypeComboBox.Items));
        }

        [TestMethod]
        public void ApplicationTypesPopulatedListFiltered()
        {
            _fakeHosting.InitializePageForUnitTests(_page,
                delegate()
                {
                    _fakeHosting.Fake_hierarchy.Fake_supportedMyApplicationTypes = "WindowsClassLib;WindowsService";
                });

            CollectionAssert.AreEqual(
                new object[] { "WPF Class Library" },
                Mocks.Utility.GetToStringValuesOfCollection(_page.ApplicationTypeComboBox.Items));
        }

        [TestMethod]
        public void OutputTypeGet_Console()
        {
            _fakeHosting.InitializePageForUnitTests(_page,
                delegate()
                {
                    _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeExe);
                });

            object value = null;
            _accessor.GetOutputTypeFromUI(_page.ApplicationTypeComboBox, new UserPropertyDescriptor("foo", typeof(prjOutputType)), ref value);
            Assert.AreEqual(prjOutputType.prjOutputTypeExe, value);
        }

        [TestMethod]
        public void OutputTypeGet_WinExe()
        {
            _fakeHosting.InitializePageForUnitTests(_page,
                delegate()
                {
                    _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
                });

            object value = null;
            _accessor.GetOutputTypeFromUI(_page.ApplicationTypeComboBox, new UserPropertyDescriptor("foo", typeof(prjOutputType)), ref value);
            Assert.AreEqual(prjOutputType.prjOutputTypeWinExe, value);
        }

        [TestMethod]
        public void ApplicationTypeFromOutputType()
        {
            Assert.AreEqual(ApplicationTypes.WindowsApp, ApplicationPropPageVBWPF.ApplicationTypeFromOutputType(prjOutputType.prjOutputTypeWinExe));
            Assert.AreEqual(ApplicationTypes.CommandLineApp, ApplicationPropPageVBWPF.ApplicationTypeFromOutputType(prjOutputType.prjOutputTypeExe));
            Assert.AreEqual(ApplicationTypes.WindowsClassLib, ApplicationPropPageVBWPF.ApplicationTypeFromOutputType(prjOutputType.prjOutputTypeLibrary));
        }

        [TestMethod]
        public void OutputTypeFromApplicationType()
        {
            Assert.AreEqual(prjOutputType.prjOutputTypeWinExe, ApplicationPropPageVBWPF.OutputTypeFromApplicationType(ApplicationTypes.WindowsApp));
            Assert.AreEqual(prjOutputType.prjOutputTypeExe, ApplicationPropPageVBWPF.OutputTypeFromApplicationType(ApplicationTypes.CommandLineApp));
            Assert.AreEqual(prjOutputType.prjOutputTypeLibrary, ApplicationPropPageVBWPF.OutputTypeFromApplicationType(ApplicationTypes.WindowsClassLib));
        }

        #endregion

        #region ShutdownMode class

        [TestMethod]
        public void ShutdownModeClass_Constructor_Properties()
        {
            ApplicationPropPageVBWPF.ShutdownMode x = new ApplicationPropPageVBWPF.ShutdownMode("foo", "bar");

            Assert.AreEqual("foo", x.Value);
            Assert.AreEqual("bar", x.Description);
            Assert.AreEqual("bar", x.ToString());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShutdownModeClass_Constructor_ValueNull()
        {
            ApplicationPropPageVBWPF.ShutdownMode x = new ApplicationPropPageVBWPF.ShutdownMode(null, "foo");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ShutdownModeClass_Constructor_DescriptionNull()
        {
            ApplicationPropPageVBWPF.ShutdownMode x = new ApplicationPropPageVBWPF.ShutdownMode("foo", null);
        }

        #endregion

        #region nested class StartupObjectOrUri, StartupObject, StartupUri

        [TestMethod]
        public void StartupObjectOrUri_Constructors()
        {
            ApplicationPropPageVBWPF.StartupObject ob = new ApplicationPropPageVBWPF.StartupObject("val", "desc");
            Assert.AreEqual("val", ob.Value);
            Assert.AreEqual("desc", ob.Description);

            ApplicationPropPageVBWPF.StartupObjectNone none = new ApplicationPropPageVBWPF.StartupObjectNone();
            Assert.AreEqual("", none.Value);
            Assert.AreEqual("(None)", none.Description);

            ApplicationPropPageVBWPF.StartupUri uri = new ApplicationPropPageVBWPF.StartupUri("val");
            Assert.AreEqual("val", uri.Value);
            Assert.AreEqual("val", uri.Description);
        }

        [TestMethod]
        public void StartupObjectOrUri_ConstructorNulls()
        {
            ApplicationPropPageVBWPF.StartupObjectOrUri or = new ApplicationPropPageVBWPF.StartupObject(null, null);
            Assert.AreEqual("", or.Value);
            Assert.AreEqual("", or.Description);
            Assert.IsNotNull(or.Value);
            Assert.IsNotNull(or.Description);
        }

        [TestMethod]
        public void StartupObject_Equals()
        {
            Assert.AreEqual(new ApplicationPropPageVBWPF.StartupObject("a", "b"),
                new ApplicationPropPageVBWPF.StartupObject("a", "b"));
            Assert.AreEqual(new ApplicationPropPageVBWPF.StartupObject("a", "b"),
                new ApplicationPropPageVBWPF.StartupObject("a", "c"));
            Assert.AreEqual(new ApplicationPropPageVBWPF.StartupObject("a", "b"),
                new ApplicationPropPageVBWPF.StartupObject("a", "c"));
        }

        [TestMethod]
        public void StartupObject_Equals_SubMain()
        {
            Assert.AreEqual(new ApplicationPropPageVBWPF.StartupObject("", "b"),
                new ApplicationPropPageVBWPF.StartupObject("", "c"));
            Assert.AreEqual(new ApplicationPropPageVBWPF.StartupObject("Sub Main", "b"),
                new ApplicationPropPageVBWPF.StartupObject("", "c"));
            Assert.AreEqual(new ApplicationPropPageVBWPF.StartupObject("", "b"),
                new ApplicationPropPageVBWPF.StartupObject("sub main", "c"));
            Assert.AreEqual(new ApplicationPropPageVBWPF.StartupObject("Sub Main", "b"),
                new ApplicationPropPageVBWPF.StartupObject("Sub Main", "c"));
        }

        [TestMethod]
        public void StartupUri_Equals()
        {
            Assert.AreEqual(new ApplicationPropPageVBWPF.StartupUri("a"),
                new ApplicationPropPageVBWPF.StartupUri("a"));

            Assert.AreNotEqual(new ApplicationPropPageVBWPF.StartupUri(""),
                new ApplicationPropPageVBWPF.StartupUri("Sub Main"));
            Assert.AreNotEqual(new ApplicationPropPageVBWPF.StartupUri(""),
                new ApplicationPropPageVBWPF.StartupObject("", ""));
        }

        [TestMethod]
        public void StartupObjectNone_Equals()
        {
            Assert.AreEqual(new ApplicationPropPageVBWPF.StartupObjectNone(),
                new ApplicationPropPageVBWPF.StartupObjectNone());

            Assert.AreNotEqual(new ApplicationPropPageVBWPF.StartupObject("a", "b"),
                new ApplicationPropPageVBWPF.StartupObjectNone());
            Assert.AreNotEqual(new ApplicationPropPageVBWPF.StartupObject("", ""),
                new ApplicationPropPageVBWPF.StartupObjectNone());
            Assert.AreNotEqual(new ApplicationPropPageVBWPF.StartupObject("(None)", ""),
                new ApplicationPropPageVBWPF.StartupObjectNone());
            Assert.AreNotEqual(new ApplicationPropPageVBWPF.StartupUri("(None)"),
                new ApplicationPropPageVBWPF.StartupObjectNone());
            Assert.AreNotEqual(new ApplicationPropPageVBWPF.StartupUri(""),
                new ApplicationPropPageVBWPF.StartupObjectNone());
        }

        #endregion

        #region Assembly Name textbox

        [TestMethod]
        public void AssemblyName()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("AssemblyName", "foo.assembly.name");
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
            });

            Assert.AreEqual("foo.assembly.name", _page.AssemblyNameTextBox.Text);
        }

        [TestMethod]
        public void AssemblyName_Change()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("AssemblyName", "foo.assembly.name");
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
            });
            Assert.IsTrue(_page.AssemblyNameTextBox.Enabled);

            _fakeHosting.Fake_TypeTextIntoControl(_page.AssemblyNameTextBox, "changed.assembly.name");

            Assert.AreEqual("changed.assembly.name", _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_GetPropertyValue("AssemblyName"));
            Assert.AreEqual("changed.assembly.name", _page.AssemblyNameTextBox.Text);
        }

        [TestMethod]
        public void AssemblyName_Change_InvalidValue()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("AssemblyName", "foo.assembly.name");
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
            });
            Assert.IsTrue(_page.AssemblyNameTextBox.Enabled);

            _fakeHosting.Fake_ExpectMsgBox(typeof(ValidationException), null);
            _fakeHosting.Fake_TypeTextIntoControl(_page.AssemblyNameTextBox, "");

            /*This won't work until undo/red support is added to the unit tests
            Assert.AreEqual("foo.assembly.name", _fakeHosting._hierarchy.Fake_projectProperties.AssemblyName);
            Assert.AreEqual("foo.assembly.name", _page.AssemblyNameTextBox.Text);
            */
        }

        [TestMethod]
        public void AssemblyName_Change_CheckoutCanceled()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("AssemblyName", "foo.assembly.name");
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
            });
            Assert.IsTrue(_page.AssemblyNameTextBox.Enabled);

            _fakeHosting.Fake_site.Fake_vsQueryEditQuerySave2.Fake_QueryEditFilesResult.SetToUserCancel();

            // No msgbox expected for checkout canceled
            _fakeHosting.Fake_TypeTextIntoControl(_page.AssemblyNameTextBox, "changed.assembly.name");

            /*This won't work until undo/redo support is added to the unit tests
            Assert.AreEqual("foo.assembly.name", _fakeHosting._hierarchy.Fake_projectProperties.AssemblyName);
            Assert.AreEqual("foo.assembly.name", _page.AssemblyNameTextBox.Text);
            */
        }

        [TestMethod]
        public void AssemblyName_Change_CheckoutFailed()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("AssemblyName", "foo.assembly.name");
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
            });
            Assert.IsTrue(_page.AssemblyNameTextBox.Enabled);

            _fakeHosting.Fake_site.Fake_vsQueryEditQuerySave2.Fake_QueryEditFilesResult.SetToCheckoutFailed();

            _fakeHosting.Fake_ExpectMsgBox(typeof(ValidationException), null);
            _fakeHosting.Fake_TypeTextIntoControl(_page.AssemblyNameTextBox, "changed.assembly.name");

            Assert.AreEqual("foo.assembly.name", _fakeHosting.Fake_projectProperties.Fake_GetPropertyValue("AssemblyName"));
            /*This won't work until undo/redo support is added to the unit tests
            Assert.AreEqual("foo.assembly.name", _page.AssemblyNameTextBox.Text);
            */
        }

        [TestMethod]
        public void AssemblyName_Change_HiddenByFlavor()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("AssemblyName", "foo.assembly.name");
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
                _fakeHosting.Fake_Flavor_SetPropertyToHidden("AssemblyName");
            });

            Assert.IsFalse(_page.AssemblyNameTextBox.Enabled);
            Assert.AreEqual("", _page.AssemblyNameTextBox.Text);
        }

        [TestMethod]
        public void AssemblyName_Change_ReadOnlyByFlavor()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("AssemblyName", "foo.assembly.name");
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
                _fakeHosting.Fake_Flavor_SetPropertyToReadOnly("AssemblyName");
            });
            Assert.IsTrue(_page.AssemblyNameTextBox.ReadOnly);
            Assert.AreEqual("foo.assembly.name", _page.AssemblyNameTextBox.Text);
        }

        #endregion

        #region Root Namespace textbox

        [TestMethod]
        public void RootNamespace()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("RootNamespace", "foo.rootnamespace.name");
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
            });

            Assert.AreEqual("foo.rootnamespace.name", _page.RootNamespaceTextBox.Text);
        }

        [TestMethod]
        public void RootNamespace_Change()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("RootNamespace", "foo.rootnamespace.name");
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
            });
            Assert.IsTrue(_page.RootNamespaceTextBox.Enabled);

            _fakeHosting.Fake_TypeTextIntoControl(_page.RootNamespaceTextBox, "changed.rootnamespace");

            Assert.AreEqual("changed.rootnamespace", _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_GetPropertyValue("RootNamespace"));
            Assert.AreEqual("changed.rootnamespace", _page.RootNamespaceTextBox.Text);
        }

        [TestMethod]
        public void RootNamespace_Change_InvalidValue()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("RootNamespace", "foo.rootnamespace.name");
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
            });
            Assert.IsTrue(_page.RootNamespaceTextBox.Enabled);

            _fakeHosting.Fake_ExpectMsgBox(typeof(ValidationException), null);
            _fakeHosting.Fake_TypeTextIntoControl(_page.RootNamespaceTextBox, "bad#value");

            /*This won't work until undo/red support is added to the unit tests
            Assert.AreEqual("foo.rootnamespace.name", _fakeHosting._hierarchy.Fake_projectProperties.RootNamespace);
            Assert.AreEqual("foo.rootnamespace.name", _page.RootNamespaceTextBox.Text);
            */
        }

        [TestMethod]
        public void RootNamespace_Change_CheckoutCanceled()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("RootNamespace", "foo.rootnamespace.name");
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
            });
            Assert.IsTrue(_page.RootNamespaceTextBox.Enabled);

            _fakeHosting.Fake_site.Fake_vsQueryEditQuerySave2.Fake_QueryEditFilesResult.SetToUserCancel();

            // No msgbox expected for checkout canceled
            _fakeHosting.Fake_TypeTextIntoControl(_page.RootNamespaceTextBox, "changed.rootnamespace");

            /*This won't work until undo/redo support is added to the unit tests
            Assert.AreEqual("foo.rootnamespace.name", _fakeHosting._hierarchy.Fake_projectProperties.RootNamespace);
            Assert.AreEqual("foo.rootnamespace.name", _page.RootNamespaceTextBox.Text);
            */
        }

        [TestMethod]
        public void RootNamespace_Change_CheckoutFailed()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("RootNamespace", "foo.rootnamespace.name");
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
            });
            Assert.IsTrue(_page.RootNamespaceTextBox.Enabled);

            _fakeHosting.Fake_site.Fake_vsQueryEditQuerySave2.Fake_QueryEditFilesResult.SetToCheckoutFailed();

            _fakeHosting.Fake_ExpectMsgBox(typeof(ValidationException), null);
            _fakeHosting.Fake_TypeTextIntoControl(_page.RootNamespaceTextBox, "changed.rootnamespace");

            Assert.AreEqual("foo.rootnamespace.name", _fakeHosting.Fake_projectProperties.Fake_GetPropertyValue("RootNamespace"));
            /*This won't work until undo/redo support is added to the unit tests
            Assert.AreEqual("foo.rootnamespace.name", _page.RootNamespaceTextBox.Text);
            */
        }

        [TestMethod]
        public void RootNamespace_Change_HiddenByFlavor()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("RootNamespace", "foo.rootnamespace.name");
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
                _fakeHosting.Fake_Flavor_SetPropertyToHidden("RootNamespace");
            });

            Assert.IsFalse(_page.RootNamespaceTextBox.Enabled);
            Assert.AreEqual("", _page.RootNamespaceTextBox.Text);
        }

        [TestMethod]
        public void RootNamespace_Change_ReadOnlyByFlavor()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("RootNamespace", "foo.rootnamespace.name");
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
                _fakeHosting.Fake_Flavor_SetPropertyToReadOnly("RootNamespace");
            });
            Assert.IsTrue(_page.RootNamespaceTextBox.ReadOnly);
            Assert.AreEqual("foo.rootnamespace.name", _page.RootNamespaceTextBox.Text);
        }

        #endregion

        #region Application Type combobox

        [TestMethod]
        public void ApplicationType_ChangeToWPFApp()
        {
            _fakeHosting.InitializePageForUnitTests(_page,
                delegate()
                {
                    _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeLibrary);
                });

            // Set to winexe app
            _fakeHosting.Fake_SelectItemInComboBox(_page.ApplicationTypeComboBox, "WPF Application");

            Assert.AreEqual("WPF Application", _page.ApplicationTypeComboBox.SelectedItem.ToString());
            Assert.AreEqual(prjOutputType.prjOutputTypeWinExe, _fakeHosting.Fake_projectProperties.Fake_GetPropertyValue("OutputType"));
        }

        [TestMethod]
        public void ApplicationType_ChangeToConsole()
        {
            _fakeHosting.InitializePageForUnitTests(_page,
                delegate()
                {
                    _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeLibrary);
                });

            // Set to winexe app
            _fakeHosting.Fake_SelectItemInComboBox(_page.ApplicationTypeComboBox, "WPF Console Application");

            Assert.AreEqual("WPF Console Application", _page.ApplicationTypeComboBox.SelectedItem.ToString());
            Assert.AreEqual(prjOutputType.prjOutputTypeExe, _fakeHosting.Fake_projectProperties.Fake_GetPropertyValue("OutputType"));
        }

        [TestMethod]
        public void ApplicationType_ChangeToClassLibrary()
        {
            _fakeHosting.InitializePageForUnitTests(_page,
                delegate()
                {
                    _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
                });

            // Set to winexe app
            _fakeHosting.Fake_SelectItemInComboBox(_page.ApplicationTypeComboBox, "WPF Class Library");

            Assert.AreEqual("WPF Class Library", _page.ApplicationTypeComboBox.SelectedItem.ToString());
            Assert.AreEqual(prjOutputType.prjOutputTypeLibrary, _fakeHosting.Fake_projectProperties.Fake_GetPropertyValue("OutputType"));
        }

        [TestMethod]
        public void ApplicationType_WinExe()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
            });

            Assert.AreEqual("WPF Application", _page.ApplicationTypeComboBox.SelectedItem.ToString());
        }

        [TestMethod]
        public void ApplicationType_Exe()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeExe);
            });

            Assert.AreEqual("WPF Console Application", _page.ApplicationTypeComboBox.SelectedItem.ToString());
        }

        [TestMethod]
        public void ApplicationType_Change_CheckoutCanceled()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeLibrary);
            });
            Assert.IsTrue(_page.ApplicationTypeComboBox.Enabled);
            _fakeHosting.Fake_site.Fake_vsQueryEditQuerySave2.Fake_QueryEditFilesResult.SetToUserCancel();

            // No msgbox expected for checkout canceled
            _fakeHosting.Fake_SelectItemInComboBox(_page.ApplicationTypeComboBox, "WPF Class Library");

            /*This won't work until undo/redo support is added to the unit tests
            Assert.AreEqual("foo.assembly.name", _fakeHosting._hierarchy.Fake_projectProperties.ApplicationType);
            Assert.AreEqual("foo.assembly.name", _page.ApplicationTypeTextBox.Text);
            */
            Assert.AreEqual(prjOutputType.prjOutputTypeLibrary, _fakeHosting.Fake_projectProperties.Fake_GetPropertyValue("OutputType"), "Value shouldn't have changed");
        }

        [TestMethod]
        public void ApplicationType_Change_NoChangeNoCheckout()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeExe);
            });
            Assert.IsTrue(_page.ApplicationTypeComboBox.Enabled);
            _fakeHosting.Fake_site.Fake_vsQueryEditQuerySave2.Fake_QueryEditFilesResult.SetToCheckoutFailed();

            _fakeHosting.Fake_SelectItemInComboBox(_page.ApplicationTypeComboBox, "WPF Console Application");

            Assert.AreEqual(prjOutputType.prjOutputTypeExe, _fakeHosting.Fake_projectProperties.Fake_GetPropertyValue("OutputType"), "Value shouldn't have changed");
        }

        [TestMethod]
        public void ApplicationType_Change_CheckoutFailed()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeExe);
            });
            Assert.IsTrue(_page.ApplicationTypeComboBox.Enabled);
            _fakeHosting.Fake_site.Fake_vsQueryEditQuerySave2.Fake_QueryEditFilesResult.SetToCheckoutFailed();
            _fakeHosting.Fake_ExpectMsgBox(typeof(ValidationException), null);

            _fakeHosting.Fake_SelectItemInComboBox(_page.ApplicationTypeComboBox, "WPF Class Library");

            /*This won't work until undo/redo support is added to the unit tests
            Assert.AreEqual("foo.assembly.name", _page.ApplicationTypeTextBox.Text);
            */
            Assert.AreEqual(prjOutputType.prjOutputTypeExe,
                _fakeHosting.Fake_projectProperties.Fake_GetPropertyValue("OutputType"), "Value shouldn't have changed");
        }

        [TestMethod]
        public void ApplicationType_Change_HiddenByFlavor()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
                _fakeHosting.Fake_Flavor_SetPropertyToHidden("OutputType");
            });

            Assert.IsFalse(_page.ApplicationTypeComboBox.Enabled);
            Assert.IsNull(_page.ApplicationTypeComboBox.SelectedItem);
        }

        [TestMethod]
        public void ApplicationType_Change_ReadOnlyByFlavor()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
                _fakeHosting.Fake_Flavor_SetPropertyToReadOnly("OutputType");
            });
            Assert.IsFalse(_page.ApplicationTypeComboBox.Enabled);
            Assert.AreEqual(_page.ApplicationTypeComboBox.SelectedItem.ToString(), "WPF Application");
        }


        #endregion

        #region Icon combobox

        [TestMethod]
        public void Icon_EnabledForWPFApp()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
            });
            Assert.IsTrue(_page.IconCombobox.Enabled);
            Assert.IsTrue(_page.IconPicturebox.Visible);
            Assert.IsNotNull(_page.IconPicturebox.Image);
        }

        [TestMethod]
        public void Icon_EnabledForConsole()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeExe);
            });
            Assert.IsTrue(_page.IconCombobox.Enabled);
            Assert.IsNotNull(_page.IconPicturebox.Image);
        }

        [TestMethod]
        public void Icon_DisabledForClassLibrary()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeLibrary);
            });
            Assert.IsFalse(_page.IconCombobox.Enabled);
            Assert.IsNull(_page.IconPicturebox.Image);
        }

        [TestMethod]
        public void Icon_EnabledWhenChangedToWPFApp()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeLibrary);
            });
            Assert.IsFalse(_page.IconCombobox.Enabled);

            _fakeHosting.Fake_SelectItemInComboBox(_page.ApplicationTypeComboBox, "WPF Application");

            Assert.IsTrue(_page.IconCombobox.Enabled);
            Assert.IsNotNull(_page.IconPicturebox.Image);
        }

        [TestMethod]
        public void Icon_EnabledWhenChangedToConsole()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeLibrary);
            });
            Assert.IsFalse(_page.IconCombobox.Enabled);

            _fakeHosting.Fake_SelectItemInComboBox(_page.ApplicationTypeComboBox, "WPF Console Application");

            Assert.IsTrue(_page.IconCombobox.Enabled);
            Assert.IsNotNull(_page.IconPicturebox.Image);
        }

        [TestMethod]
        public void Icon_DisabledWhenChangedToClassLibrary()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeExe);
            });
            Assert.IsTrue(_page.IconCombobox.Enabled);

            _fakeHosting.Fake_SelectItemInComboBox(_page.ApplicationTypeComboBox, "WPF Class Library");

            Assert.IsFalse(_page.IconCombobox.Enabled);
            Assert.IsNull(_page.IconPicturebox.Image);
        }

        [TestMethod]
        public void Icon_Hidden()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.InitializePageForUnitTests(_page, delegate
                    {
                        _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
                        _fakeHosting.Fake_Flavor_SetPropertyToHidden("ApplicationIcon");
                    });
            });

            Assert.IsFalse(_page.IconCombobox.Enabled);
        }

        [TestMethod]
        public void Icon_Readonly()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.InitializePageForUnitTests(_page, delegate
                    {
                        _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeWinExe);
                        _fakeHosting.Fake_Flavor_SetPropertyToReadOnly("ApplicationIcon");
                    });
            });

            Assert.IsFalse(_page.IconCombobox.Enabled);
        }

        #endregion

        #region Startup Object/URI combobox

        [TestMethod]
        public void StartupObject_NoneForClassLibrary()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeLibrary);
            });

            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Enabled);
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Visible);
            Assert.AreEqual("(None)", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
            Assert.AreEqual(1, _page.StartupObjectOrUriComboBox.Items.Count);
            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.AreEqual("Startup &object:", _page.StartupObjectOrUriLabel.Text);
        }

        [TestMethod]
        public void StartupObject_StartupObjectEmptyAndAppXamlExistsButStartupUriEmpty()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeWinExe);
                _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "");
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriEmpty);
            });

            // If StartupObject is empty (not explicitly the string "Sub Main"),
            //   and the app.xaml exists, then it's a Startup URI
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Enabled);
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Visible);
            Assert.AreEqual("Startup &URI:", _page.StartupObjectOrUriLabel.Text);
            Assert.AreEqual("", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
        }

        [TestMethod]
        public void StartupObject_StartupObjectEmptyAndAppXamlDoesntExist()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeWinExe);
                _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "");
            });

            // If StartupObject is empty (not explicitly the string "Sub Main"),
            //   but StartupURI is not specified in the app.xaml, then 
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Enabled);
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Visible);
            Assert.AreEqual("Startup &object:", _page.StartupObjectOrUriLabel.Text);
            Assert.AreEqual("Sub Main", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
            Assert.AreEqual(1, _page.StartupObjectOrUriComboBox.Items.Count);
            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
        }

        [TestMethod]
        public void StartupObject_StartupUriNonEmptyButStartupObjectAlsoNonEmpty()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "StartupModule");
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
            });

            //StartupObject takes precedence - if it's non-empty, then a start-up object
            //   is used.  Application framework checkbox is enabled but unchecked.
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Enabled);
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Visible);
            Assert.AreEqual("Startup &object:", _page.StartupObjectOrUriLabel.Text);
            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.AreEqual("StartupModule", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
        }

        [TestMethod]
        public void StartupObject_XamlExistsButStartupObjectSubMain()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "Sub Main");
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
            });

            //StartupObject takes precedence - if it's non-empty, then a start-up object
            //   is used.  Application framework checkbox is enabled but unchecked.
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Enabled);
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Visible);
            Assert.AreEqual("Startup &object:", _page.StartupObjectOrUriLabel.Text);
            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.AreEqual("Sub Main", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
        }

        [TestMethod]
        public void StartupURI_StartupUriNonEmpty()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "");
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
            });

            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Enabled);
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Visible);
            Assert.AreEqual("Startup &URI:", _page.StartupObjectOrUriLabel.Text);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.AreEqual("Startup Page.xaml", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
        }

        [TestMethod]
        public void StartupURI_StartupUriEmpty()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "");
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriEmpty);
            });

            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Enabled);
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Visible);
            Assert.AreEqual("Startup &URI:", _page.StartupObjectOrUriLabel.Text);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.AreEqual("", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
        }

        [TestMethod]
        public void StartupURI_Change()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "");
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
                _page.StartupObjectOrUriComboBox.Items.Add(
                    new ApplicationPropPageVBWPF.StartupUri("Page2.xaml"));
            });
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Enabled);
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Visible);
            Assert.AreEqual("Startup &URI:", _page.StartupObjectOrUriLabel.Text);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.AreEqual("Startup Page.xaml", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());

            _fakeHosting.Fake_SelectItemInComboBox(_page.StartupObjectOrUriComboBox, "Page2.xaml");

            using (AppDotXamlDocument document = _page.Fake_getAppDotXamlDocumentResults.CreateAppDotXamlDocumentFromTextBuffer())
            {
                Assert.AreEqual("Page2.xaml", document.GetStartupUri());
                Assert.AreEqual("Page2.xaml", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
            }
        }

        [TestMethod]
        [Ignore] //UNDONE: need to get DocData fake to call QueryEdit
        public void StartupURI_CheckOutXamlFailed()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "");
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
                _page.StartupObjectOrUriComboBox.Items.Add(
                    new ApplicationPropPageVBWPF.StartupUri("Page2.xaml"));
            });
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Enabled);
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Visible);
            Assert.AreEqual("Startup &URI:", _page.StartupObjectOrUriLabel.Text);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.AreEqual("Startup Page.xaml", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
            _fakeHosting.Fake_site.Fake_vsQueryEditQuerySave2.Fake_QueryEditFilesResult.SetToCheckoutFailed();
            _fakeHosting.Fake_ExpectMsgBox(typeof(ValidationException), null);

            _fakeHosting.Fake_SelectItemInComboBox(_page.StartupObjectOrUriComboBox, "Page2.xaml");

            using (AppDotXamlDocument document = _page.Fake_getAppDotXamlDocumentResults.CreateAppDotXamlDocumentFromTextBuffer())
            {
                Assert.AreEqual("Startup Page.xaml", document.GetStartupUri());
                Assert.AreEqual("Startup Page.xaml", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
            }
        }

        [TestMethod]
        [Ignore] //UNDONE: need to get DocData fake to call QueryEdit
        public void StartupURI_CheckOutXamlCanceled()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "");
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
                _page.StartupObjectOrUriComboBox.Items.Add(
                    new ApplicationPropPageVBWPF.StartupUri("Page2.xaml"));
            });
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Enabled);
            Assert.IsTrue(_page.StartupObjectOrUriComboBox.Visible);
            Assert.AreEqual("Startup &URI:", _page.StartupObjectOrUriLabel.Text);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.AreEqual("Startup Page.xaml", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
            _fakeHosting.Fake_site.Fake_vsQueryEditQuerySave2.Fake_QueryEditFilesResult.SetToUserCancel();
            // No msgbox expected for checkout canceled

            _fakeHosting.Fake_SelectItemInComboBox(_page.StartupObjectOrUriComboBox, "Page2.xaml");

            using (AppDotXamlDocument document = _page.Fake_getAppDotXamlDocumentResults.CreateAppDotXamlDocumentFromTextBuffer())
            {
                Assert.AreEqual("Startup Page.xaml", document.GetStartupUri());
                Assert.AreEqual("Startup Page.xaml", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
            }
        }

        [TestMethod]
        public void StartupObjectURI_StartupObjectHidden()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.InitializePageForUnitTests(_page, delegate
                    {
                        _fakeHosting.Fake_Flavor_SetPropertyToHidden("StartupObject");
                    });
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
            });

            Assert.IsFalse(_page.StartupObjectOrUriComboBox.Enabled);
            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.IsFalse(_page.WindowsAppGroupBox.Enabled);
        }

        [TestMethod]
        public void StartupObjectURI_StartupObjectReadonly()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.InitializePageForUnitTests(_page, delegate
                    {
                        _fakeHosting.Fake_Flavor_SetPropertyToReadOnly("StartupObject");
                    });
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
            });

            Assert.IsFalse(_page.StartupObjectOrUriComboBox.Enabled);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
        }

        #endregion

        #region GetAvailableStartupUris

        [TestMethod]
        public void StartupURI_PopulateDropdown_SelectedItemNotInProject()
        {
            const string projectPath = @"c:\temp\Project1\";
            ProjectItemsFake projectItems =
                new ProjectItemsFake("WindowsApplication1",
                    new ProjectItemWithBuildActionFake(projectPath, "Form1.vb", "Compile"),
                    new ProjectItemWithBuildActionFake(projectPath, "Page1.xaml", "Page"),
                    new ProjectItemWithBuildActionFake(projectPath, "Page2.xaml", "Page"));

            _fakeHosting.InitializePageForUnitTests(_page,
                delegate
                {
                    _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("FullPath", projectPath);
                    _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "");
                    _page.Fake_getAppDotXamlDocumentResults =
                        new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
                    _fakeHosting.Fake_hierarchy.Fake_project.Fake_projectItems = projectItems;
                });

            // Force the combobox to get populated by dropping it down
            _fakeHosting.Fake_DropDownCombobox(_page.StartupObjectOrUriComboBox);

            CollectionAssert.AreEqual(
                new string[] {
                    "Page1.xaml",
                    "Page2.xaml",
                    "Startup Page.xaml",
                },
                _fakeHosting.Fake_GetDisplayTextOfItemsInCombobox(_page.StartupObjectOrUriComboBox));
        }

        [TestMethod]
        public void StartupURI_PopulateDropdown_SelectedItemInProject()
        {
            const string projectPath = @"c:\temp\Project1\";
            ProjectItemsFake projectItems =
                new ProjectItemsFake("WindowsApplication1",
                    new ProjectItemWithBuildActionFake(projectPath, "Form1.vb", "Compile"),
                    new ProjectItemWithBuildActionFake(projectPath, "Page1.xaml", "Page"),
                    new ProjectItemWithBuildActionFake(projectPath, "Startup Page.xaml", "Page"),
                    new ProjectItemWithBuildActionFake(projectPath, "Page2.xaml", "Page"));

            _fakeHosting.InitializePageForUnitTests(_page,
                delegate
                {
                    _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("FullPath", projectPath);
                    _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "");
                    _page.Fake_getAppDotXamlDocumentResults =
                        new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
                    _fakeHosting.Fake_hierarchy.Fake_project.Fake_projectItems = projectItems;
                });

            // Force the combobox to get populated by dropping it down
            _fakeHosting.Fake_DropDownCombobox(_page.StartupObjectOrUriComboBox);

            CollectionAssert.AreEqual(
                new string[] {
                    "Page1.xaml",
                    "Startup Page.xaml",
                    "Page2.xaml",
                },
                _fakeHosting.Fake_GetDisplayTextOfItemsInCombobox(_page.StartupObjectOrUriComboBox));
        }

        [TestMethod]
        public void StartupURI_PopulateDropdown_Subfolders()
        {
            const string projectPath = @"c:\temp\Project1\";
            ProjectItemsFake projectItems =
                new ProjectItemsFake("WindowsApplication1",
                    new ProjectItemWithBuildActionFake(projectPath, "Form1.vb", "Compile"),
                    new ProjectItemWithBuildActionFake(projectPath, "Page1.xaml", "Page"),
                    new ProjectItemWithBuildActionFake(projectPath, "Startup Page.xaml", "Page"),
                    new ProjectItemFake(projectPath + "Subfolder1\\", "Subfolder1",
                        new ProjectItemsFake("Subfolder1",
                            new ProjectItemWithBuildActionFake(projectPath + "Subfolder1\\", "Page1.xaml", "Page")
                        )
                    ),
                    new ProjectItemWithBuildActionFake(projectPath, "Page2.xaml", "Page"));

            _fakeHosting.InitializePageForUnitTests(_page,
                delegate
                {
                    _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("FullPath", projectPath);
                    _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "");
                    _page.Fake_getAppDotXamlDocumentResults =
                        new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
                    _fakeHosting.Fake_hierarchy.Fake_project.Fake_projectItems = projectItems;
                });

            // Force the combobox to get populated by dropping it down
            _fakeHosting.Fake_DropDownCombobox(_page.StartupObjectOrUriComboBox);

            CollectionAssert.AreEqual(
                new string[] {
                    "Page1.xaml",
                    "Startup Page.xaml",
                    @"Subfolder1\Page1.xaml",
                    "Page2.xaml",
                },
                _fakeHosting.Fake_GetDisplayTextOfItemsInCombobox(_page.StartupObjectOrUriComboBox));
        }

        [TestMethod]
        public void StartupURI_PopulateDropdown_IncorrectAndMissingBuildAction()
        {
            const string projectPath = @"c:\temp\Project1\";
            ProjectItemsFake projectItems =
                new ProjectItemsFake("WindowsApplication1",
                    new ProjectItemWithBuildActionFake(projectPath, "Form1.vb", "Compile"),
                    new ProjectItemWithBuildActionFake(projectPath, "Page1.xaml", "Page"),
                    new ProjectItemFake(projectPath, "MissingBuildAction.xaml"),
                    new ProjectItemWithBuildActionFake(projectPath, "Startup Page.xaml", "Page"),
                    new ProjectItemWithBuildActionFake(projectPath, "Startup Page BuildActionWrong.xaml", "WrongBuildAction"),
                    new ProjectItemFake(projectPath + "Subfolder1\\", "Subfolder1",
                        new ProjectItemsFake("Subfolder1",
                            new ProjectItemWithBuildActionFake(projectPath + "Subfolder1\\", "Page1.xaml", "Page")
                        )
                    ),
                    new ProjectItemWithBuildActionFake(projectPath, "Page2.xaml", "Page"));

            _fakeHosting.InitializePageForUnitTests(_page,
                delegate
                {
                    _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("FullPath", projectPath);
                    _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "");
                    _page.Fake_getAppDotXamlDocumentResults =
                        new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
                    _fakeHosting.Fake_hierarchy.Fake_project.Fake_projectItems = projectItems;
                });

            // Force the combobox to get populated by dropping it down
            _fakeHosting.Fake_DropDownCombobox(_page.StartupObjectOrUriComboBox);

            CollectionAssert.AreEqual(
                new string[] {
                    "Page1.xaml",
                    "Startup Page.xaml",
                    @"Subfolder1\Page1.xaml",
                    "Page2.xaml",
                },
                _fakeHosting.Fake_GetDisplayTextOfItemsInCombobox(_page.StartupObjectOrUriComboBox));
        }

        [TestMethod]
        public void StartupURI_PopulateDropdown_LinkOutsideTheProject()
        {
            const string projectPath = @"c:\temp\Project1\";
            ProjectItemsFake projectItems =
                new ProjectItemsFake("WindowsApplication1",
                    new ProjectItemWithBuildActionFake(projectPath, "Form1.vb", "Compile"),
                    new ProjectItemWithBuildActionFake(@"c:\foo\", @"c:\foo\Page1.xaml", "Page"),
                    new ProjectItemWithBuildActionFake(projectPath, "Page2.xaml", "Page"));

            _fakeHosting.InitializePageForUnitTests(_page,
                delegate
                {
                    _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("FullPath", projectPath);
                    _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "");
                    _page.Fake_getAppDotXamlDocumentResults =
                        new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
                    _fakeHosting.Fake_hierarchy.Fake_project.Fake_projectItems = projectItems;
                });

            // Force the combobox to get populated by dropping it down
            _fakeHosting.Fake_DropDownCombobox(_page.StartupObjectOrUriComboBox);

            CollectionAssert.AreEqual(
                new string[] {
                    "Page2.xaml",
                    "Startup Page.xaml",
                },
                _fakeHosting.Fake_GetDisplayTextOfItemsInCombobox(_page.StartupObjectOrUriComboBox));
        }

        #endregion

        #region Shutdown Mode combobox

        [TestMethod]
        public void ShutdownMode_Change()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "");
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
            });
            Assert.IsTrue(_page.ShutdownModeComboBox.Enabled);
            Assert.IsTrue(_page.ShutdownModeComboBox.Visible);
            Assert.AreEqual("On last window close", _page.ShutdownModeComboBox.SelectedItem.ToString());

            _fakeHosting.Fake_SelectItemInComboBox(_page.ShutdownModeComboBox, "On main window close");

            Assert.AreEqual("On main window close", _page.ShutdownModeComboBox.SelectedItem.ToString());
        }

        [TestMethod]
        public void ShutdownMode_Change_CheckoutFailed()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_projectProperties.Fake_SetPropertyValue("StartupObject", "");
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
            });
            Assert.IsTrue(_page.ShutdownModeComboBox.Enabled);
            Assert.IsTrue(_page.ShutdownModeComboBox.Visible);
            Assert.AreEqual("On last window close", _page.ShutdownModeComboBox.SelectedItem.ToString());
            _fakeHosting.Fake_site.Fake_vsQueryEditQuerySave2.Fake_QueryEditFilesResult.SetToCheckoutFailed();
            _fakeHosting.Fake_ExpectMsgBox(typeof(ValidationException), null);

            _fakeHosting.Fake_SelectItemInComboBox(_page.ShutdownModeComboBox, "On main window close");

            Assert.AreEqual("On last window close", _page.ShutdownModeComboBox.SelectedItem.ToString());
        }

        #endregion


        #region Use Application Framework checkbox

        #region "Utilities"

        private ProjectItem FindApplicationXamlProjectItemByBuildAction(VsHierarchyFake vsHierarchyFake)
        {
            foreach (ProjectItem projectItem in vsHierarchyFake.Fake_projectItems.Values)
            {
                if (((ProjectItemFake)projectItem).Fake_PropertiesCollection.Fake_PropertiesDictionary["ItemType"].Value.Equals("ApplicationDefinition"))
                    return projectItem;
            }

            return null;
        }

        private ProjectItem FindApplicationXamlProjectItemByFileName(VsHierarchyFake vsHierarchyFake)
        {
            foreach (ProjectItem projectItem in vsHierarchyFake.Fake_projectItems.Values)
            {
                if (System.IO.Path.GetFileName(projectItem.get_FileNames(1)).Equals("Application.xaml", StringComparison.OrdinalIgnoreCase))
                    return projectItem;
            }

            return null;
        }

        #endregion

        [TestMethod]
        public void UseApplicationFramework_EnabledForWPFApp()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeWinExe);
            });

            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsFalse(_page.WindowsAppGroupBox.Enabled);
        }

        [TestMethod]
        public void UseApplicationFramework_DisabledForClassLibrary()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeLibrary);
            });

            // For class library, application framework checkbox should be disabled
            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.IsFalse(_page.WindowsAppGroupBox.Enabled);
        }

        [TestMethod]
        public void UseApplicationFramework_DisabledForConsole()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeExe);
            });

            // For class library, application framework checkbox should be disabled
            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.IsFalse(_page.WindowsAppGroupBox.Enabled);
        }

        [TestMethod]
        public void UseApplicationFramework_DisabledWhenChangedToClassLibrary()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType", prjOutputType.prjOutputTypeExe);
            });

            _fakeHosting.Fake_SelectItemInComboBox(_page.ApplicationTypeComboBox, "WPF Class Library");

            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.IsFalse(_page.WindowsAppGroupBox.Enabled);
        }

        [TestMethod]
        public void UseApplicationFramework_EnabledWhenChangedFromClassLibraryToWPFApp()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeLibrary);
            });

            _fakeHosting.Fake_SelectItemInComboBox(_page.ApplicationTypeComboBox, "WPF Application");

            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.IsFalse(_page.WindowsAppGroupBox.Enabled);
        }

        [TestMethod]
        public void UseApplicationFramework_ChangeToChecked_AppXamlAlreadyExists()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeWinExe);
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("StartupObject",
                    "Module1");
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriEmpty);
            });
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsFalse(_page.WindowsAppGroupBox.Enabled);
            Assert.AreEqual("Startup &object:", _page.StartupObjectOrUriLabel.Text);

            _page.UseApplicationFrameworkCheckBox.Checked = true;

            // From spec:  When the checkbox is checked by the user, the <StartupObject> 
            //   element in the .vbproj file is removed, the “Windows application framework 
            //   properties” groupbox is enabled, and a valid application definition file 
            //   is added to the project if one does not exist.
            using (AppDotXamlDocument document = _page.Fake_getAppDotXamlDocumentResults.CreateAppDotXamlDocumentFromTextBuffer())
            {
                Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
                Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Checked);
                Assert.IsTrue(_page.WindowsAppGroupBox.Enabled);
                Assert.AreEqual("", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
                Assert.AreEqual("Startup &URI:", _page.StartupObjectOrUriLabel.Text);
                Assert.AreEqual("", _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_GetPropertyValue("StartupObject"));
                Assert.AreEqual("", document.GetStartupUri(), "StartupUri in app.xaml shouldn't have actually changed");
            }
        }

        [TestMethod]
        public void UseApplicationFramework_ChangeToChecked_AppXamlAlreadyExists_CheckoutCanceled()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeWinExe);
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("StartupObject",
                    "Module1");
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriEmpty);
            });
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsFalse(_page.WindowsAppGroupBox.Enabled);
            Assert.AreEqual("Startup &object:", _page.StartupObjectOrUriLabel.Text);
            _fakeHosting.Fake_site.Fake_vsQueryEditQuerySave2.Fake_QueryEditFilesResult.SetToUserCancel();

            _page.UseApplicationFrameworkCheckBox.Checked = true;
            Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
            Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Checked);
            Assert.IsFalse(_page.WindowsAppGroupBox.Enabled);
            Assert.AreEqual("Startup &object:", _page.StartupObjectOrUriLabel.Text);
        }

        [TestMethod]
        public void UseApplicationFramework_ChangeToUnchecked()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeWinExe);
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("StartupObject",
                    "");
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
            });
            using (AppDotXamlDocument document = _page.Fake_getAppDotXamlDocumentResults.CreateAppDotXamlDocumentFromTextBuffer())
            {
                Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
                Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Checked);
                Assert.IsTrue(_page.WindowsAppGroupBox.Enabled);
                Assert.AreEqual("Startup &URI:", _page.StartupObjectOrUriLabel.Text);
                Assert.AreEqual("Startup Page.xaml", document.GetStartupUri());
                Assert.IsNotNull(FindApplicationXamlProjectItemByFileName(_fakeHosting.Fake_hierarchy));
                Assert.IsNotNull(FindApplicationXamlProjectItemByBuildAction(_fakeHosting.Fake_hierarchy));
                Assert.AreEqual("ApplicationDefinition", FindApplicationXamlProjectItemByBuildAction(_fakeHosting.Fake_hierarchy).Properties.Item("ItemType").Value);
                Assert.AreEqual("ApplicationDefinition", FindApplicationXamlProjectItemByFileName(_fakeHosting.Fake_hierarchy).Properties.Item("ItemType").Value);
            }
           
            _page.UseApplicationFrameworkCheckBox.Checked = false;

            // From spec:  When the checkbox is unchecked, the “Startup Uri:” combo box...
            //   its value set to “Sub Main” and its label changed to “Startup Object:”.  The 
            //   <StartupObject> element in the .vbproj file is subsequently set to “Sub Main”.
            using (AppDotXamlDocument document = _page.Fake_getAppDotXamlDocumentResults.CreateAppDotXamlDocumentFromTextBuffer())
            {
                Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
                Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Checked);
                Assert.IsFalse(_page.WindowsAppGroupBox.Enabled);
                Assert.AreEqual("Startup &object:", _page.StartupObjectOrUriLabel.Text);
                Assert.AreEqual("Sub Main", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
                Assert.AreEqual("Sub Main", _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_GetPropertyValue("StartupObject"));
                Assert.AreEqual("Startup Page.xaml", document.GetStartupUri(), "StartupUri in app.xaml shouldn't have actually changed");
            }

            // The an application definition file should still be in the project, with its Build Action is set to None.
            Assert.IsNull(FindApplicationXamlProjectItemByBuildAction(_fakeHosting.Fake_hierarchy), "Shouldn't be a file with buildaction=applicationdefinition anymore");
            Assert.IsNotNull(FindApplicationXamlProjectItemByFileName(_fakeHosting.Fake_hierarchy), "Application.xaml should still be in the project");
            Assert.AreEqual("None", FindApplicationXamlProjectItemByFileName(_fakeHosting.Fake_hierarchy).Properties.Item("ItemType").Value, "Application.xaml buildaction should be applicationdefinition");
        }

        /*
        [TestMethod]
        public void UseApplicationFramework_ChangeToChecked()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeWinExe);
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("StartupObject",
                    "");
                _fakeHosting.Fake_hierarchy.Fake_vsProjectSpecialFiles.Fake_AppXamlSpecialFile.DeleteFile();
            });
            using (AppDotXamlDocument document = _page.Fake_getAppDotXamlDocumentResults.CreateAppDotXamlDocumentFromTextBuffer())
            {
                Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
                Assert.IsFalse(_page.UseApplicationFrameworkCheckBox.Checked);
                Assert.IsFalse(_page.WindowsAppGroupBox.Enabled);
                Assert.AreEqual("Startup &object:", _page.StartupObjectOrUriLabel.Text);
                Assert.AreEqual("Sub Main", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
                Assert.IsNull(FindApplicationXamlProjectItemByFileName(_fakeHosting.Fake_hierarchy));
                Assert.IsNull(FindApplicationXamlProjectItemByBuildAction(_fakeHosting.Fake_hierarchy));
            }

            _page.UseApplicationFrameworkCheckBox.Checked = true;

            using (AppDotXamlDocument document = _page.Fake_getAppDotXamlDocumentResults.CreateAppDotXamlDocumentFromTextBuffer())
            {
                Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
                Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Checked);
                Assert.IsTrue(_page.WindowsAppGroupBox.Enabled);
                Assert.AreEqual("Startup &URI:", _page.StartupObjectOrUriLabel.Text);
                Assert.AreEqual("Sub Main", _page.StartupObjectOrUriComboBox.SelectedItem.ToString());
                Assert.AreEqual("Sub Main", _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_GetPropertyValue("StartupObject"));
                Assert.AreEqual("Startup Page.xaml", document.GetStartupUri(), "StartupUri in app.xaml shouldn't have actually changed");
            }

            // The application definition file should still be in the project, with its Build Action is set to None.
            Assert.IsNull(FindApplicationXamlProjectItemByBuildAction(_fakeHosting.Fake_hierarchy), "Shouldn't be a file with buildaction=applicationdefinition anymore");
            Assert.IsNotNull(FindApplicationXamlProjectItemByFileName(_fakeHosting.Fake_hierarchy), "Application.xaml should still be in the project");
            Assert.AreEqual("None", FindApplicationXamlProjectItemByFileName(_fakeHosting.Fake_hierarchy).Properties.Item("ItemType").Value, "Application.xaml buildaction should be applicationdefinition");
        }
        */

        [TestMethod]
        public void UseApplicationFramework_ChangeToUnchecked_CheckoutFailed()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeWinExe);
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("StartupObject",
                    "");
                _page.Fake_getAppDotXamlDocumentResults =
                    new GetAppDotXamlDocumentResults(APPXAML_StartupUriNonEmpty);
            });
            using (AppDotXamlDocument document = _page.Fake_getAppDotXamlDocumentResults.CreateAppDotXamlDocumentFromTextBuffer())
            {
                Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
                Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Checked);
                Assert.IsTrue(_page.WindowsAppGroupBox.Enabled);
                Assert.AreEqual("Startup &URI:", _page.StartupObjectOrUriLabel.Text);
                Assert.AreEqual("Startup Page.xaml", document.GetStartupUri());
                _fakeHosting.Fake_site.Fake_vsQueryEditQuerySave2.Fake_QueryEditFilesResult.SetToCheckoutFailed();
                _fakeHosting.Fake_ExpectMsgBox(typeof(ValidationException), null);
            }

            _page.UseApplicationFrameworkCheckBox.Checked = false;

            using (AppDotXamlDocument document = _page.Fake_getAppDotXamlDocumentResults.CreateAppDotXamlDocumentFromTextBuffer())
            {
                Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Enabled);
                Assert.IsTrue(_page.UseApplicationFrameworkCheckBox.Checked);
                Assert.IsTrue(_page.WindowsAppGroupBox.Enabled);
                Assert.AreEqual("Startup &URI:", _page.StartupObjectOrUriLabel.Text);
                Assert.AreEqual("Startup Page.xaml", document.GetStartupUri());
                Assert.IsNotNull(FindApplicationXamlProjectItemByFileName(_fakeHosting.Fake_hierarchy));
                Assert.IsNotNull(FindApplicationXamlProjectItemByBuildAction(_fakeHosting.Fake_hierarchy));
                Assert.AreEqual("ApplicationDefinition", FindApplicationXamlProjectItemByBuildAction(_fakeHosting.Fake_hierarchy).Properties.Item("ItemType").Value);
                Assert.AreEqual("ApplicationDefinition", FindApplicationXamlProjectItemByFileName(_fakeHosting.Fake_hierarchy).Properties.Item("ItemType").Value);
            }
        }


        #endregion


        #region XBAP

        [TestMethod]
        public void XBAP_ControlsDisabled()
        {
            _fakeHosting.InitializePageForUnitTests(_page, delegate
            {
                _fakeHosting.Fake_hierarchy.Fake_projectProperties.Fake_SetPropertyValue("OutputType",
                    prjOutputType.prjOutputTypeWinExe);
                _fakeHosting.Fake_projectProperties.Fake_AddProperty("HostInBrowser", true);
            });
            Assert.IsFalse(_page.IconCombobox.Enabled);
            Assert.IsNull(_page.IconPicturebox.Image);
            Assert.IsFalse(_page.ShutdownModeComboBox.Enabled);
            Assert.IsFalse(_page.ApplicationTypeComboBox.Enabled);
        }

        #endregion
    }









    #region "Mocks"

    class ApplicationPropPageVBWPFMock : ApplicationPropPageVBWPF
    {
        public GetAppDotXamlDocumentResults Fake_getAppDotXamlDocumentResults = new GetAppDotXamlDocumentResults();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Fake_getAppDotXamlDocumentResults != null)
                {
                    Fake_getAppDotXamlDocumentResults.Dispose();
                    Fake_getAppDotXamlDocumentResults = null;
                }
            }
            base.Dispose(disposing);
        }

        public bool Fake_OnRootNamespaceChangedWasCalled;
        protected override void OnRootNamespaceChanged(EnvDTE.Project Project, System.IServiceProvider ServiceProvider, string OldRootNamespace, string NewRootNamespace)
        {
            Fake_OnRootNamespaceChangedWasCalled = true;
        }

        protected override AppDotXamlDocument CreateAppDotXamlDocumentForApplicationDefinitionFile(bool createAppXamlIfDoesNotExist)
        {
            if (Fake_getAppDotXamlDocumentResults.exceptionToThrow != null)
                throw Fake_getAppDotXamlDocumentResults.exceptionToThrow;

            return Fake_getAppDotXamlDocumentResults.CreateAppDotXamlDocumentFromTextBuffer();
        }

        protected override bool ApplicationXamlFileExistsInProject()
        {
            return (Fake_getAppDotXamlDocumentResults.appDotXamlFileTextBuffer != null);
        }


    }

    internal class GetAppDotXamlDocumentResults : IDisposable
    {
        public VsTextBufferFake appDotXamlFileTextBuffer = null;
        public Exception exceptionToThrow = null;

        #region IDisposable Members

        public GetAppDotXamlDocumentResults()
        {
        }

        public GetAppDotXamlDocumentResults(string appDotXamlFileContents)
        {
            appDotXamlFileTextBuffer = new VsTextBufferFake(appDotXamlFileContents);
        }

        public void Dispose()
        {
            if (appDotXamlFileTextBuffer != null)
            {
                appDotXamlFileTextBuffer.Dispose();
                appDotXamlFileTextBuffer = null;
            }
        }

        public AppDotXamlDocument CreateAppDotXamlDocumentFromTextBuffer()
        {
            if (appDotXamlFileTextBuffer == null)
                return null;

            return ApplicationPropPageVBWPFTests.CreateAppDotXamlDocument(appDotXamlFileTextBuffer);
        }

        #endregion
    }



    #endregion

}
