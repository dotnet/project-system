using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Editors;
using Microsoft.VisualStudio.Editors.DesignerFramework;
using Microsoft.VisualStudio.Editors.UnitTests.DesignerFramework;
using SD = Microsoft.VisualStudio.Editors.SettingsDesigner;

using Microsoft.VisualStudio.TestTools.MockObjects;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;
using Microsoft.VisualBasic;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using Microsoft.VisualStudio.Designer.Interfaces;

namespace Microsoft.VisualStudio.Editors.UnitTests.SettingsDesigner
{
    [TestClass]
    public class SettingsDesignerAccessModifierComboboxTests
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

        #region "Utilities"

        public void TestGetCurrentValue(string expectedCurrentValue, object customToolValue, bool supportCustomToolValueInProject, CodeDomProvider codeProvider)
        {
            SD.SettingsDesigner designer = new SD.SettingsDesigner();
            ProjectItemFake projectItem;
            if (supportCustomToolValueInProject)
            {
                projectItem = new ProjectItemWithCustomToolFake(customToolValue);
            }
            else
            {
                projectItem = new ProjectItemFake();
            }

            Mock<IVSMDCodeDomProvider> vsmdCodeDomProviderMock = new Mock<IVSMDCodeDomProvider>();
            vsmdCodeDomProviderMock.Implement("get_CodeDomProvider", codeProvider);

            ServiceProviderMock spMock = new ServiceProviderMock();
            if (codeProvider != null)
            {
                spMock.Fake_AddService(typeof(IVSMDCodeDomProvider), vsmdCodeDomProviderMock.Instance);
            }
            else
            {
                spMock.Fake_AddService(typeof(IVSMDCodeDomProvider), null);
            }

            SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox combo =
                new SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox(
                    designer,
                    spMock.Instance,
                    projectItem,
                    codeProvider is VBCodeProvider ? "My" : null);
            Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor accessor =
                new Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor(combo);

            Assert.AreEqual(expectedCurrentValue, accessor.GetCurrentValue());
        }

        static void TestSetCurrentValue(CodeDomProvider codeProvider, string initialCustomTool, string initialNamespace, string newCurrentValue, string expectedCustomTool, string expectedNamespace)
        {
            SD.SettingsDesigner designer = new SD.SettingsDesigner();
            ProjectItemFake projectItem;
            projectItem = new ProjectItemWithCustomToolFake(initialCustomTool, initialNamespace);

            Mock<IVSMDCodeDomProvider> vsmdCodeDomProviderMock = new Mock<IVSMDCodeDomProvider>();
            vsmdCodeDomProviderMock.Implement("get_CodeDomProvider", codeProvider);

            ServiceProviderMock spMock = new ServiceProviderMock();
            if (codeProvider != null)
            {
                spMock.Fake_AddService(typeof(IVSMDCodeDomProvider), vsmdCodeDomProviderMock.Instance);
            }
            else
            {
                spMock.Fake_AddService(typeof(IVSMDCodeDomProvider), null);
            }

            SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox combo =
                new SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox(
                    designer,
                    spMock.Instance,
                    projectItem,
                    codeProvider is VBCodeProvider ? "My" : null);
            Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor accessor =
                new Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor(combo);

            // Call the method under test
            accessor.SetCurrentValue(newCurrentValue);

            // Verify results
            Assert.AreEqual(expectedCustomTool, projectItem.Fake_PropertiesCollection.Item("CustomTool").Value);
            Assert.AreEqual(expectedNamespace, projectItem.Fake_PropertiesCollection.Item("CustomToolNamespace").Value);
        }

        #endregion

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorServiceProviderNull()
        {
            SD.SettingsDesigner designer = new SD.SettingsDesigner();
            SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox x =
                new SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox(
                    designer,
                    null,
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorRootDesignerNull()
        {
            SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox x =
                new SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox(
                    null,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(null),
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorProjectItemNull()
        {
            SD.SettingsDesigner designer = new SD.SettingsDesigner();
            SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox x =
                new SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox(
                    designer,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(null),
                    null,
                    null);
        }

        [TestMethod]
        public void GetMenuCommandsToRegister()
        {
            SD.SettingsDesigner designer = new SD.SettingsDesigner();
            SettingsDesignerAccessModifierComboboxSubclass combo =
                new SettingsDesignerAccessModifierComboboxSubclass(
                    true,
                    designer,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(null),
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);

            List<MenuCommand> commands = new List<MenuCommand>();

            Guid guid1 = new Guid("c2013470-51ac-4278-9ac5-389c72a1f926");
            Guid guid2 = new Guid("c2013470-51ac-4278-9ac5-389c72a1f926");
            CommandID commandId1 = new CommandID(guid1, 0x2106);
            CommandID commandId2 = new CommandID(guid2, 0x2107);

            foreach (MenuCommand cmd in combo.GetMenuCommandsToRegister())
            {
                commands.Add(cmd);
            }

            Assert.IsInstanceOfType(commands[0], typeof(DesignerCommandBarComboBox), "Should have created a combobox menu item and a filler menu item");
            Assert.IsInstanceOfType(commands[1], typeof(DesignerCommandBarComboBoxFiller));

            Assert.AreEqual(commandId1, commands[0].CommandID);
            Assert.AreEqual(commandId2, commands[1].CommandID);

            Assert.IsTrue(combo.Fake_isMenuCommandForwarderRegistered);
        }

        [TestMethod]
        public void GetDropdownValuesVB()
        {
            SD.SettingsDesigner designer = new SD.SettingsDesigner();
            SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox combo =
                new SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox(
                    designer,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(new VBCodeProvider()),
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);

            CollectionAssert.AreEqual(new string[] { "Friend", "Public" }, combo.GetDropdownValues());
        }

#if false //JSharp no longer supported
        [TestMethod]
        public void GetDropdownValuesJSharp()
        {
            SD.SettingsDesigner designer = new SD.SettingsDesigner();
            SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox combo =
                new SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox(
                    designer,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(new VJSharpCodeProvider()),
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);

            CollectionAssert.AreEqual(new string[] { "Public" }, combo.GetDropdownValues());
        }
#endif

        [TestMethod]
        public void GetDropdownValuesCSharp()
        {
            SD.SettingsDesigner designer = new SD.SettingsDesigner();
            SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox combo =
                new SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox(
                    designer,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(new CSharpCodeProvider()),
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);

            CollectionAssert.AreEqual(new string[] { "Internal", "Public" }, combo.GetDropdownValues());
        }

        [TestMethod]
        public void CurrentValueNoCodeDomProvider()
        {
            TestGetCurrentValue("Internal", "SettingsSingleFileGenerator", true, null);
        }

        [TestMethod]
        public void CurrentValueFriend()
        {
            TestGetCurrentValue("Friend", "SettingsSingleFileGenerator", true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValuePublic()
        {
            TestGetCurrentValue("Public", "PublicSettingsSingleFileGenerator", true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueFriendCSharp()
        {
            TestGetCurrentValue("Internal", "SettingsSingleFileGenerator", true, new CSharpCodeProvider());
        }

        [TestMethod]
        public void CurrentValuePublicCSharp()
        {
            TestGetCurrentValue("Public", "PublicSettingsSingleFileGenerator", true, new CSharpCodeProvider());
        }

#if false //JSharp no longer supported
        [TestMethod]
        public void CurrentValuePublicJSharp()
        {
            TestGetCurrentValue("Public", "SettingsSingleFileGenerator", true, new VJSharpCodeProvider());
        }
#endif

        [TestMethod]
        public void CurrentValueEmpty()
        {
            TestGetCurrentValue("(Custom)", "", true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueCustom()
        {
            TestGetCurrentValue("(Custom)", "unknown generator", true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueCustomToolIsNull()
        {
            TestGetCurrentValue("(Custom)", null, true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueCustomToolIsNotString()
        {
            TestGetCurrentValue("(Custom)", new object(), true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueCustomToolIsNotSupported()
        {
            TestGetCurrentValue("(Custom)", null, false, new VBCodeProvider());
        }

        #region SetCurrentValue

        [TestMethod]
        public void SetCurrentValueVbMyPublic()
        {
            TestSetCurrentValue(
                new VBCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Public", //newCurrentValue
                "PublicSettingsSingleFileGenerator", //expectedCustomTool
                "My" //expectedNamespace
                );
        }

        [TestMethod]
        public void SetCurrentValueVbMyFriend()
        {
            TestSetCurrentValue(
                new VBCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Friend", //newCurrentValue
                "SettingsSingleFileGenerator", //expectedCustomTool
                "My" //expectedNamespace
                );
        }

        [TestMethod]
        public void SetCurrentValueVbMyUnrecognizedDoesNothing()
        {
            TestSetCurrentValue(
                new VBCodeProvider(),
                "abc", //initialCustomTool
                "Original namespace", //initialNamespace
                "Internal", //newCurrentValue
                "abc", //expectedCustomTool
                "Original namespace" //expectedNamespace
                );
        }

        [TestMethod]
        public void SetCurrentValueVbPublic()
        {
            TestSetCurrentValue(
                new VBCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Public", //newCurrentValue
                "PublicSettingsSingleFileGenerator", //expectedCustomTool
                "My" //expectedNamespace
                );
        }

        [TestMethod]
        public void SetCurrentValueVbFriend()
        {
            TestSetCurrentValue(
                new VBCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Friend", //newCurrentValue
                "SettingsSingleFileGenerator", //expectedCustomTool
                "My" //expectedNamespace
                );
        }

        [TestMethod]
        public void SetCurrentValueCSharpPublic()
        {
            TestSetCurrentValue(
                new CSharpCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Public", //newCurrentValue
                "PublicSettingsSingleFileGenerator", //expectedCustomTool
                "Original namespace" //expectedNamespace
                );
        }

        [TestMethod]
        public void SetCurrentValueCSharpFriend()
        {
            TestSetCurrentValue(
                new CSharpCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Internal", //newCurrentValue
                "SettingsSingleFileGenerator", //expectedCustomTool
                "Original namespace" //expectedNamespace
                );
        }

        #endregion

        #region Setting (or not) the custom tool namespace

        [TestMethod]
        public void SetCurrentValueChangeToMyIfVBAndCurrentToolIsEmpty()
        {
            TestSetCurrentValue(
                new VBCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Public", //newCurrentValue
                "PublicSettingsSingleFileGenerator", //expectedCustomTool
                "My" //expectedNamespace
                );
        }

        [TestMethod]
        public void SetCurrentValueDontChangeToMyIfNotVB()
        {
            TestSetCurrentValue(
                new CSharpCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Public", //newCurrentValue
                "PublicSettingsSingleFileGenerator", //expectedCustomTool
                "Original namespace" //expectedNamespace
                );
        }

        [TestMethod]
        public void SetCurrentValueDontChangeToMyIfCurrentToolIsNotEmpty()
        {
            TestSetCurrentValue(
                new VBCodeProvider(),
                "Friend", //initialCustomTool
                "Original namespace", //initialNamespace
                "Public", //newCurrentValue
                "PublicSettingsSingleFileGenerator", //expectedCustomTool
                "Original namespace" //expectedNamespace
                );
        }

        #endregion

    }


    #region private class SettingsDesignerAccessModifierComboboxSubclass

    class SettingsDesignerAccessModifierComboboxSubclass : SD.SettingsDesignerView.SettingsDesignerAccessModifierCombobox
    {
        public bool Fake_isDesignerEditable;
        public bool Fake_isMenuCommandForwarderRegistered;

        public SettingsDesignerAccessModifierComboboxSubclass(
                bool isDesignerEditable,
                BaseRootDesigner rootDesigner,
                IServiceProvider serviceProvider,
                EnvDTE.ProjectItem projectItem,
                string namespaceToOverrideIfCustomToolIsEmpty)
            : base(rootDesigner, serviceProvider, projectItem, namespaceToOverrideIfCustomToolIsEmpty)
        {
            Fake_isDesignerEditable = isDesignerEditable;
        }

        protected override bool IsDesignerEditable()
        {
            return Fake_isDesignerEditable;
        }

        protected override void RegisterMenuCommandForwarder()
        {
            Fake_isMenuCommandForwarderRegistered = true;
        }

        protected override void UnregisterMenuCommandForwarder()
        {
            Fake_isMenuCommandForwarderRegistered = false;
        }

    }

    #endregion

}
