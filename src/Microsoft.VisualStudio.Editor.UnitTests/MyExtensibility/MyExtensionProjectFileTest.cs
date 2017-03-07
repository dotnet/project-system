//-----------------------------------------------------------------------
// <copyright file="MyExtensionProjectFileTest.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
//     Information Contained Herein is Proprietary and Confidential.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Editors.MyExtensibility;

namespace Microsoft.VisualStudio.Editors.UnitTests.MyExtensibility
{
    /// <summary>
    /// Test class for Microsoft.VisualStudio.Editors.MyExtensibility.MyExtensionProjectFile.
    /// </summary>    
    [TestClass()]
    public class MyExtensionProjectItemGroupTest
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

        /// <summary>
        /// Test extension project file with extension name.
        /// </summary>
        [TestMethod()]
        public void MyExtensionProjectItemGroupTest1()
        {
            MyExtensionProjectItemGroup extProjItemGroup = new MyExtensionProjectItemGroup(
                EXTENSIONID_1, EXTENSIONVERSION_1, EXTENSIONNAME_1, EXTENSIONDESCRIPTION_1);
            Assert.AreEqual(EXTENSIONID_1, extProjItemGroup.ExtensionID, "Correct extension ID.");
            Assert.AreEqual(EXTENSIONVERSION_1, extProjItemGroup.ExtensionVersion, "Correct extension version.");
            Assert.AreEqual(EXTENSIONNAME_1, extProjItemGroup.DisplayName, "Correct display name.");
            Assert.AreEqual(EXTENSIONDESCRIPTION_1, extProjItemGroup.ExtensionDescription, "Correct description.");
        }

        /// <summary>
        /// Test extension project file without extension name. Display name should use file name.
        /// </summary>
        [TestMethod()]
        public void MyExtensionProjectItemGroupTest2()
        {
            MyExtensionProjectItemGroup extProjItemGroup = new MyExtensionProjectItemGroup(
                EXTENSIONID_1, EXTENSIONVERSION_1, null, null);
            Assert.AreEqual(EXTENSIONID_1, extProjItemGroup.ExtensionID, "Correct extension ID.");
            Assert.AreEqual(EXTENSIONVERSION_1, extProjItemGroup.ExtensionVersion, "Correct extension version.");
            Assert.AreEqual(EXTENSIONID_1, extProjItemGroup.DisplayName, "Correct display name.");
            Assert.AreEqual(null, extProjItemGroup.ExtensionDescription, "Correct description.");
        }

        private const string EXTENSIONID_1 = "Microsoft.VisualBasic.Media.MyMediaPlayer.MyExtension";
        private static Version EXTENSIONVERSION_1 = new Version(1, 2, 3, 4);
        private const string EXTENSIONNAME_1 = "My.Media extension";
        private const string EXTENSIONDESCRIPTION_1 = "Extending Visual Basic My namespace to include My.Media";        
    }
}
