// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
//using Microsoft.VJSharp;
using Microsoft.VisualStudio.Designer.Interfaces;

namespace Microsoft.VisualStudio.Editors.UnitTests.ResourceEditor
{
    [TestClass]
    public class ResourceEditorAccessModifierComboboxTests
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

        enum ResXFileType
        {
            DefaultResX,
            StandaloneResX
        }

        static void TestGetCurrentValue(string expectedCurrentValue, ResXFileType resXFileType, object initialCustomToolValue, bool supportCustomToolValueInProject, CodeDomProvider codeProvider)
        {
        }

        static void TestIsEnabled(bool expectedIsEnabled, ResXFileType resXFileType, object initialCustomToolValue, bool supportCustomToolValueInProject, CodeDomProvider codeProvider)
        {
            if (resXFileType == ResXFileType.DefaultResX)
            {
                Assert.IsTrue(codeProvider == null || codeProvider is VBCodeProvider, "Problem in the unit test itself: don't pass in ResXFileType.DefaultResX except for VB");
            }

            ResourceEditorRootDesigner designer = new ResourceEditorRootDesigner();
            ProjectItemFake projectItem;
            if (supportCustomToolValueInProject)
            {
                projectItem = new ProjectItemWithCustomToolFake(initialCustomToolValue);
            }
            else
            {
                projectItem = new ProjectItemFake();
            }

            IServiceProvider sp = AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(codeProvider);

            ResourceEditorAccessModifierComboboxSubclass combo =
                new ResourceEditorAccessModifierComboboxSubclass(
                    true,
                    resXFileType == ResXFileType.DefaultResX,
                    !(resXFileType == ResXFileType.DefaultResX),
                    designer,
                    sp,
                    projectItem,
                    codeProvider is VBCodeProvider ? "My.Resources" : null
                    );
            Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor accessor =
                new Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor(combo);

            Assert.AreEqual(expectedIsEnabled, accessor.ShouldBeEnabled());
        }

        static void TestSetCurrentValue(ResXFileType resXFileType, bool allowNoCodeGeneration, CodeDomProvider codeProvider, string initialCustomTool, string initialNamespace, string newCurrentValue, string expectedCustomTool, string expectedNamespace)
        {
            ResourceEditorRootDesigner designer = new ResourceEditorRootDesigner();
            ProjectItemFake projectItem;
            projectItem = new ProjectItemWithCustomToolFake(initialCustomTool, initialNamespace);

            IServiceProvider sp = AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(codeProvider);

            ResourceEditorView.ResourceEditorAccessModifierCombobox combo =
                new ResourceEditorView.ResourceEditorAccessModifierCombobox(
                    resXFileType == ResXFileType.DefaultResX,
                    allowNoCodeGeneration,
                    designer,
                    sp,
                    projectItem,
                    codeProvider is VBCodeProvider ? "My.Resources" : null
                    );
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
        public void ConstructorRootDesignerNull()
        {
            ResourceEditorView.ResourceEditorAccessModifierCombobox x =
                new ResourceEditorView.ResourceEditorAccessModifierCombobox(
                    false,
                    false,
                    null,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(null),
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorServiceProviderNull()
        {
            ResourceEditorRootDesigner designer = new ResourceEditorRootDesigner();
            ResourceEditorView.ResourceEditorAccessModifierCombobox x =
                new ResourceEditorView.ResourceEditorAccessModifierCombobox(
                    false,
                    true,
                    designer,
                    null,
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorProjectItemNull()
        {
            ResourceEditorRootDesigner designer = new ResourceEditorRootDesigner();
            ResourceEditorView.ResourceEditorAccessModifierCombobox x =
                new ResourceEditorView.ResourceEditorAccessModifierCombobox(
                    false,
                    false,
                    designer,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(null),
                    null,
                    null);
        }

        [TestMethod]
        public void GetMenuCommandsToRegister()
        {
            ResourceEditorRootDesigner designer = new ResourceEditorRootDesigner();
            ResourceEditorAccessModifierComboboxSubclass combo =
                new ResourceEditorAccessModifierComboboxSubclass(
                    true,
                    false,
                    false,
                    designer,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(null),
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);

            List<MenuCommand> commands = new List<MenuCommand>();

            Guid guid1 = new Guid("66BD4C1D-3401-4bcc-A942-E4990827E6F7");
            Guid guid2 = new Guid("66BD4C1D-3401-4bcc-A942-E4990827E6F7");
            CommandID commandId1 = new CommandID(guid1, 0x2061);
            CommandID commandId2 = new CommandID(guid2, 0x2062);

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
            ResourceEditorRootDesigner designer = new ResourceEditorRootDesigner();
            ResourceEditorView.ResourceEditorAccessModifierCombobox combo =
                new ResourceEditorView.ResourceEditorAccessModifierCombobox(
                    false,
                    false,
                    designer,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(new VBCodeProvider()),
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);

            CollectionAssert.AreEqual(new string[] { "Friend", "Public" }, combo.GetDropdownValues());

            combo =
                new ResourceEditorView.ResourceEditorAccessModifierCombobox(
                    true,
                    false,
                    designer,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(new VBCodeProvider()),
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);
            CollectionAssert.AreEqual(new string[] { "Friend", "Public" }, combo.GetDropdownValues());

            combo =
                new ResourceEditorView.ResourceEditorAccessModifierCombobox(
                    false,
                    true,
                    designer,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(new VBCodeProvider()),
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);

            CollectionAssert.AreEqual(new string[] { "Friend", "Public", "No code generation" }, combo.GetDropdownValues());

            combo =
                new ResourceEditorView.ResourceEditorAccessModifierCombobox(
                    true,
                    true,
                    designer,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(new VBCodeProvider()),
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);
            CollectionAssert.AreEqual(new string[] { "Friend", "Public", "No code generation" }, combo.GetDropdownValues());
        }

#if false //JSharp no longer supported
        [TestMethod]
        public void GetDropdownValuesJSharp()
        {
            ResourceEditorRootDesigner designer = new ResourceEditorRootDesigner();
            ResourceEditorView.ResourceEditorAccessModifierCombobox combo =
                new ResourceEditorView.ResourceEditorAccessModifierCombobox(
                    false,
                    false,
                    designer,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(new VJSharpCodeProvider()),
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);

            CollectionAssert.AreEqual(new string[] { "Public" }, combo.GetDropdownValues());

            combo =
                new ResourceEditorView.ResourceEditorAccessModifierCombobox(
                    false,
                    true,
                    designer,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(new VJSharpCodeProvider()),
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);

            CollectionAssert.AreEqual(new string[] { "Public", "No code generation" }, combo.GetDropdownValues());
        }
#endif

        [TestMethod]
        public void GetDropdownValuesCSharp()
        {
            ResourceEditorRootDesigner designer = new ResourceEditorRootDesigner();
            ResourceEditorView.ResourceEditorAccessModifierCombobox combo =
                new ResourceEditorView.ResourceEditorAccessModifierCombobox(
                    false,
                    false,
                    designer,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(new CSharpCodeProvider()),
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);

            CollectionAssert.AreEqual(new string[] { "Internal", "Public" }, combo.GetDropdownValues());

            combo =
                new ResourceEditorView.ResourceEditorAccessModifierCombobox(
                    false,
                    true,
                    designer,
                    AccessModifierComboboxTests.CreateServiceProviderWithIVSMDCodeDomProvider(new CSharpCodeProvider()),
                    new Mock<EnvDTE.ProjectItem>().Instance,
                    null);

            CollectionAssert.AreEqual(new string[] { "Internal", "Public", "No code generation" }, combo.GetDropdownValues());
        }


        #region My.Resources - default resx file

        [TestMethod]
        public void CurrentValueDefaultResXNoCodeDomProvider()
        {
            TestGetCurrentValue("Internal", ResXFileType.DefaultResX, "VbMyResourcesResXFileCodeGenerator", true, null);
        }

        [TestMethod]
        public void CurrentValueDefaultResXFriend()
        {
            TestGetCurrentValue("Friend", ResXFileType.DefaultResX, "VbMyResourcesResXFileCodeGenerator", true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueDefaultResXPublic()
        {
            TestGetCurrentValue("Public", ResXFileType.DefaultResX, "PublicVbMyResourcesResXFileCodeGenerator", true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueDefaultResXEmpty()
        {
            TestGetCurrentValue("No code generation", ResXFileType.DefaultResX, "", true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueDefaultResXCustom()
        {
            TestGetCurrentValue("(Custom)", ResXFileType.DefaultResX, "unknown generator", true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueDefaultResXCustomToolIsNull()
        {
            TestGetCurrentValue("(Custom)", ResXFileType.DefaultResX, null, true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueDefaultResXCustomToolIsNotString()
        {
            TestGetCurrentValue("(Custom)", ResXFileType.DefaultResX, new object(), true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueDefaultResXCustomToolIsNotSupported()
        {
            TestGetCurrentValue("(Custom)", ResXFileType.DefaultResX, null, false, new VBCodeProvider());
        }

        [TestMethod]
        public void EnabledWhenCustomToolRecognizedButNotWhatWeWouldSet()
        {
            TestIsEnabled(true, ResXFileType.DefaultResX, "ResXFileCodeGenerator", true, new VBCodeProvider());
            TestIsEnabled(true, ResXFileType.DefaultResX, "PublicResXFileCodeGenerator", true, new VBCodeProvider());
            TestIsEnabled(true, ResXFileType.StandaloneResX, "VbMyResourcesResXFileCodeGenerator", true, new VBCodeProvider());
            TestIsEnabled(true, ResXFileType.StandaloneResX, "PublicVbMyResourcesResXFileCodeGenerator", true, new VBCodeProvider());
        }

        [TestMethod]
        public void EnabledWhenCustomToolRecognized()
        {
            TestIsEnabled(true, ResXFileType.StandaloneResX, "ResXFileCodeGenerator", true, new VBCodeProvider());
            TestIsEnabled(true, ResXFileType.StandaloneResX, "PublicResXFileCodeGenerator", true, new VBCodeProvider());
            TestIsEnabled(true, ResXFileType.DefaultResX, "VbMyResourcesResXFileCodeGenerator", true, new VBCodeProvider());
            TestIsEnabled(true, ResXFileType.DefaultResX, "PublicVbMyResourcesResXFileCodeGenerator", true, new VBCodeProvider());
        }

        [TestMethod]
        public void EnabledWhenCustomToolEmpty()
        {
            TestIsEnabled(true, ResXFileType.StandaloneResX, "", true, new VBCodeProvider());
            TestIsEnabled(true, ResXFileType.DefaultResX, "", true, new VBCodeProvider());
            TestIsEnabled(true, ResXFileType.StandaloneResX, null, true, new VBCodeProvider());
            TestIsEnabled(true, ResXFileType.DefaultResX, null, true, new VBCodeProvider());
        }

        [TestMethod]
        public void DisabledWhenCustomToolNotRecognized()
        {
            TestIsEnabled(false, ResXFileType.StandaloneResX, "not recognized", true, new VBCodeProvider());
        }

        #endregion

        #region Stand-alone resx files

        [TestMethod]
        public void CurrentValueStandaloneResXFriend()
        {
            TestGetCurrentValue("Friend", ResXFileType.StandaloneResX, "ResXFileCodeGenerator", true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueStandaloneResXPublic()
        {
            TestGetCurrentValue("Public", ResXFileType.StandaloneResX, "PublicResXFileCodeGenerator", true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueStandaloneResXFriendCSharp()
        {
            TestGetCurrentValue("Internal", ResXFileType.StandaloneResX, "ResXFileCodeGenerator", true, new CSharpCodeProvider());
        }

        [TestMethod]
        public void CurrentValueStandaloneResXPublicCSharp()
        {
            TestGetCurrentValue("Public", ResXFileType.StandaloneResX, "PublicResXFileCodeGenerator", true, new CSharpCodeProvider());
        }

#if false //JSharp no longer supported
        [TestMethod]
        public void CurrentValueStandaloneResXPublicJSharp()
        {
            TestGetCurrentValue("Public", ResXFileType.StandaloneResX, "ResXFileCodeGenerator", true, new VJSharpCodeProvider());
        }
#endif

        [TestMethod]
        public void CurrentValueStandaloneResXEmpty()
        {
            TestGetCurrentValue("No code generation", ResXFileType.StandaloneResX, "", true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueStandaloneResXCustom()
        {
            TestGetCurrentValue("(Custom)", ResXFileType.StandaloneResX, "unknown generator", true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueStandaloneResXCustomToolIsNull()
        {
            TestGetCurrentValue("(Custom)", ResXFileType.StandaloneResX, null, true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueStandaloneResXCustomToolIsNotString()
        {
            TestGetCurrentValue("(Custom)", ResXFileType.StandaloneResX, new object(), true, new VBCodeProvider());
        }

        [TestMethod]
        public void CurrentValueStandaloneResXCustomToolIsNotSupported()
        {
            TestGetCurrentValue("(Custom)", ResXFileType.StandaloneResX, null, false, new VBCodeProvider());
        }
        #endregion

        #region SetCurrentValue

        [TestMethod]
        public void SetCurrentValueVbMyPublic()
        {
            TestSetCurrentValue(ResXFileType.DefaultResX,
                true,
                new VBCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Public", //newCurrentValue
                "PublicVbMyResourcesResXFileCodeGenerator", //expectedCustomTool
                "My.Resources" //expectedNamespace
                );
        }

        [TestMethod]
        public void SetCurrentValueVbMyFriend()
        {
            TestSetCurrentValue(ResXFileType.DefaultResX,
                false,
                new VBCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Friend", //newCurrentValue
                "VbMyResourcesResXFileCodeGenerator", //expectedCustomTool
                "My.Resources" //expectedNamespace
                );
        }

        [TestMethod]
        public void SetCurrentValueVbMyUnrecognizedDoesNothing()
        {
            TestSetCurrentValue(ResXFileType.DefaultResX,
                true,
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
            TestSetCurrentValue(ResXFileType.StandaloneResX,
                true,
                new VBCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Public", //newCurrentValue
                "PublicResXFileCodeGenerator", //expectedCustomTool
                "My.Resources" //expectedNamespace
                );
        }

        [TestMethod]
        public void SetCurrentValueVbFriend()
        {
            TestSetCurrentValue(ResXFileType.StandaloneResX,
                true,
                new VBCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Friend", //newCurrentValue
                "ResXFileCodeGenerator", //expectedCustomTool
                "My.Resources" //expectedNamespace
                );
        }

        [TestMethod]
        public void SetCurrentValueCSharpPublic()
        {
            TestSetCurrentValue(ResXFileType.StandaloneResX,
                true,
                new CSharpCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Public", //newCurrentValue
                "PublicResXFileCodeGenerator", //expectedCustomTool
                "Original namespace" //expectedNamespace
                );
        }

        [TestMethod]
        public void SetCurrentValueCSharpFriend()
        {
            TestSetCurrentValue(ResXFileType.StandaloneResX,
                true,
                new CSharpCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Internal", //newCurrentValue
                "ResXFileCodeGenerator", //expectedCustomTool
                "Original namespace" //expectedNamespace
                );
        }

        #endregion

        #region Setting (or not) the custom tool namespace

        [TestMethod]
        public void SetCurrentValueChangeToMyResourcesIfVBAndCurrentToolIsEmpty()
        {
            TestSetCurrentValue(ResXFileType.DefaultResX,
                false,
                new VBCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Public", //newCurrentValue
                "PublicVbMyResourcesResXFileCodeGenerator", //expectedCustomTool
                "My.Resources" //expectedNamespace
                );

            TestSetCurrentValue(ResXFileType.DefaultResX,
                true,
                new VBCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Public", //newCurrentValue
                "PublicVbMyResourcesResXFileCodeGenerator", //expectedCustomTool
                "My.Resources" //expectedNamespace
                );
        }

        [TestMethod]
        public void SetCurrentValueDontChangeToMyResourcesIfNotVB()
        {
            TestSetCurrentValue(ResXFileType.DefaultResX,
                false,
                new CSharpCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Public", //newCurrentValue
                "PublicVbMyResourcesResXFileCodeGenerator", //expectedCustomTool
                "Original namespace" //expectedNamespace
                );

            TestSetCurrentValue(ResXFileType.DefaultResX,
                true,
                new CSharpCodeProvider(),
                "", //initialCustomTool
                "Original namespace", //initialNamespace
                "Public", //newCurrentValue
                "PublicVbMyResourcesResXFileCodeGenerator", //expectedCustomTool
                "Original namespace" //expectedNamespace
                );
        }

        [TestMethod]
        public void SetCurrentValueDontChangeToMyResourcesIfCurrentToolIsNotEmpty()
        {
            TestSetCurrentValue(ResXFileType.DefaultResX,
                false,
                new VBCodeProvider(),
                "Friend", //initialCustomTool
                "Original namespace", //initialNamespace
                "Public", //newCurrentValue
                "PublicVbMyResourcesResXFileCodeGenerator", //expectedCustomTool
                "Original namespace" //expectedNamespace
                );

            TestSetCurrentValue(ResXFileType.DefaultResX,
                true,
                new VBCodeProvider(),
                "Friend", //initialCustomTool
                "Original namespace", //initialNamespace
                "Public", //newCurrentValue
                "PublicVbMyResourcesResXFileCodeGenerator", //expectedCustomTool
                "Original namespace" //expectedNamespace
        );
        }

        #endregion



        #region private class ResourceEditorAccessModifierComboboxSubclass

        private class ResourceEditorAccessModifierComboboxSubclass : ResourceEditorView.ResourceEditorAccessModifierCombobox
        {
            public bool Fake_isDesignerEditable;
            public bool Fake_isMenuCommandForwarderRegistered;

            public ResourceEditorAccessModifierComboboxSubclass(
                    bool isDesignerEditable,
                    bool useVbMyResXCodeGenerator,
                    bool allowNoCodeGeneration,
                    BaseRootDesigner rootDesigner,
                    IServiceProvider serviceProvider,
                    EnvDTE.ProjectItem projectItem,
                    string namespaceToOverrideIfCustomToolIsEmpty)
                : base(useVbMyResXCodeGenerator, allowNoCodeGeneration, rootDesigner, serviceProvider, projectItem, namespaceToOverrideIfCustomToolIsEmpty)
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

            protected override bool CustomToolRegistered
            {
                get
                {
                    return true;
                }
            }

        }

        #endregion

    }
}
