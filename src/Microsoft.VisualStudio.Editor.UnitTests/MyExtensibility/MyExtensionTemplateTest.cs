// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//-----------------------------------------------------------------------
// <copyright file="MyExtensionTemplateTest.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
//     Information Contained Herein is Proprietary and Confidential.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.MockObjects;
using Microsoft.VisualStudio.Editors.MyExtensibility;
using Microsoft.VisualStudio.Editors.MyExtensibility.EnvDTE90Interop;
using Util = Microsoft.VisualStudio.Editors.UnitTests.MyExtensibility.MyExtensibilityTestUtil;

namespace Microsoft.VisualStudio.Editors.UnitTests.MyExtensibility
{
    /// <summary>
    /// Test class for Microsoft.VisualStudio.Editors.MyExtensibility.MyExtensionTemplate.
    /// </summary>
    [TestClass()]
    public class MyExtensionTemplateTest
    {
        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        private TestContext testContextInstance;
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestMethod]
        public void MyExtensionTemplateCreateInstanceNullID()
        {
            Mock<Template> templateMock = Util.CreateTemplateMock(
                "Template without ID", 
                "This is an extension template without ID", 
                "C:\\Temp", "BaseName.vb",
                "<VBMyExtensionTemplate \n    Version=\"1.0.0.0\"\n    AssemblyFullName=\"System.Speech, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\"\n/>");
            MyExtensionTemplate extensionTemplate = MyExtensionTemplate.CreateInstance(templateMock.Instance);
            Assert.AreEqual(null, extensionTemplate);
        }

        [TestMethod]
        public void MyExtensionTemplateCreateInstanceNullVersion()
        {
            Mock<Template> templateMock = Util.CreateTemplateMock(
                "Template without version",
                "This is an extension template without version",
                "C:\\Temp", "BaseName.vb",
                "<VBMyExtensionTemplate \n    ID=\"Microsoft.VisualBasic.Speech.MyExtension\" \n    AssemblyFullName=\"System.Speech, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\"\n/>");
            MyExtensionTemplate extensionTemplate = MyExtensionTemplate.CreateInstance(templateMock.Instance);
            Assert.AreEqual(null, extensionTemplate);
        }

        [TestMethod()]
        public void MyExtensionTemplateCreateInstanceNoAssembly()
        {
            Mock<Template> templateMock = Util.CreateTemplateMock(
                "Template without triggering assembly", 
                "This is an extension template without triggering assembly", 
                "C:\\Temp\\File.vstemplate", 
                "Template.vb", 
                "<VBMyExtensionTemplate Version=\"1.0.0.0\" ID=\"TemplateWithoutTriggeringAssembly\"/>");
            MyExtensionTemplate extensionTemplate = MyExtensionTemplate.CreateInstance(templateMock.Instance);
            Assert.IsNotNull(extensionTemplate, "Template without triggering assembly should be allowed!");
            Assert.AreEqual("TemplateWithoutTriggeringAssembly", extensionTemplate.ID, extensionTemplate.ID + " is not correct!");
            Assert.AreEqual(Util.VERSION_1_0_0_0, extensionTemplate.Version, extensionTemplate.Version.ToString() + " is not correct!");
        }

        [TestMethod]
        public void MyExtensionTemplateCreateInstanceNullTemplate()
        {
            MyExtensionTemplate extensionTemplate = MyExtensionTemplate.CreateInstance(null);
            Assert.AreEqual(null, extensionTemplate);
        }

        [TestMethod]
        public void MyExtensionTemplateCreateInstanceTemplateWithoutFilePath1()
        {
            MyExtensionTemplate extensionTemplate = MyExtensionTemplate.CreateInstance(CreateTemplateMock(null).Instance);
            Assert.AreEqual(null, extensionTemplate);
        }

        [TestMethod]
        public void MyExtensionTemplateCreateInstanceTemplateWithoutFilePath2()
        {
            MyExtensionTemplate extensionTemplate = MyExtensionTemplate.CreateInstance(CreateTemplateMock(string.Empty).Instance);
            Assert.AreEqual(null, extensionTemplate);
        }

        [TestMethod]
        public void MyExtensionTemplateCreateInstanceTemplateWithoutFilePath3()
        {
            MyExtensionTemplate extensionTemplate = MyExtensionTemplate.CreateInstance(CreateTemplateMock("     ").Instance);
            Assert.AreEqual(null, extensionTemplate);
        }

        /// <summary>
        /// Test the whole MyExtensionTemplate with template name.
        /// </summary>
        [TestMethod]
        public void MyExtensionTemplateTest1()
        {
            MyExtensionTemplate extensionTemplate = MyExtensionTemplate.CreateInstance(Util.CreateTemplateMockSample1().Instance);
            Util.VerifyExtensionTemplate(extensionTemplate, Util.ASMNAME_VBRUN9_NORMALIZED, Util.TEMPLATE_BASENAME_1, 
                Util.TEMPLATE_DESCRIPTION_1, Util.TEMPLATE_NAME_1, 
                Util.TEMPLATE_FILEPATH_1, Util.EXTENSION_ID_1, Util.VERSION_1_0_0_0);
        }

        /// <summary>
        /// Test the whole MyExtensionTemplate without template name, display name should be ID.
        /// </summary>
        [TestMethod]
        public void MyExtensionTemplateTest2()
        {
            MyExtensionTemplate extensionTemplate = MyExtensionTemplate.CreateInstance(CreateTemplateMockSample1WithoutName().Instance);
            Util.VerifyExtensionTemplate(extensionTemplate, Util.ASMNAME_VBRUN9_NORMALIZED, Util.TEMPLATE_BASENAME_1,
                Util.TEMPLATE_DESCRIPTION_1, Util.EXTENSION_ID_1,
                Util.TEMPLATE_FILEPATH_1, Util.EXTENSION_ID_1, Util.VERSION_1_0_0_0);
        }


        private static Mock<Template> CreateTemplateMock(string filePath)
        {
            Mock<Template> templateMock = new Mock<Template>();
            templateMock.Implement("get_FilePath", (string)filePath);
            return templateMock;
        }

        private static Mock<Template> CreateTemplateMockSample1WithoutName()
        {
            return Util.CreateTemplateMock("    ", Util.TEMPLATE_DESCRIPTION_1, Util.TEMPLATE_FILEPATH_1, 
                Util.TEMPLATE_BASENAME_1, Util.TEMPLATE_CUSTOMDATA_1);
        }
    }
}
