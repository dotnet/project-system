// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.VisualStudio.TestTools.MockObjects;

using ProjectUtils = Microsoft.VisualStudio.Editors.SettingsDesigner.ProjectUtils.ProjectUtils;

namespace Microsoft.VisualStudio.Editors.UnitTests.SettingsDesigner
{
    /// <summary>
    /// Summary description for ProjectUtilsTest
    /// </summary>
    [TestClass]
    public class ProjectUtilsTest
    {
        public ProjectUtilsTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
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

        [TestMethod]
        public void CodeModelToCodeDomTypeAttributesTest_PublicSealed()
        {
            System.Reflection.TypeAttributes expected = System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Sealed;
            System.Reflection.TypeAttributes actual = 
                CodeModelToCodeDomTypeAttributesImpl(EnvDTE.vsCMAccess.vsCMAccessPublic, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CodeModelToCodeDomTypeAttributesTest_InternalSealed()
        {
            System.Reflection.TypeAttributes expected = System.Reflection.TypeAttributes.NestedAssembly| System.Reflection.TypeAttributes.Sealed;
            System.Reflection.TypeAttributes actual =
                CodeModelToCodeDomTypeAttributesImpl(EnvDTE.vsCMAccess.vsCMAccessProject, EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed);
            Assert.AreEqual(expected, actual);
        }

        public void CodeModelToCodeDomTypeAttributesTest_Public()
        {
            System.Reflection.TypeAttributes expected = System.Reflection.TypeAttributes.Public;
            System.Reflection.TypeAttributes actual =
                CodeModelToCodeDomTypeAttributesImpl(EnvDTE.vsCMAccess.vsCMAccessPublic, 0);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CodeModelToCodeDomTypeAttributesTest_Internal()
        {
            System.Reflection.TypeAttributes expected = System.Reflection.TypeAttributes.NestedAssembly;
            System.Reflection.TypeAttributes actual =
                CodeModelToCodeDomTypeAttributesImpl(EnvDTE.vsCMAccess.vsCMAccessProject, 0);
            Assert.AreEqual(expected, actual);
        }

        private System.Reflection.TypeAttributes CodeModelToCodeDomTypeAttributesImpl(EnvDTE.vsCMAccess access, EnvDTE80.vsCMInheritanceKind inheritanceKind)
        {
            SequenceMock<EnvDTE80.CodeClass2> cc2Mock = new SequenceMock<EnvDTE80.CodeClass2>();
            cc2Mock.AddExpectation("get_Access", access);
            cc2Mock.AddExpectation("get_InheritanceKind", inheritanceKind);

            System.Reflection.TypeAttributes result =
                ProjectUtils.CodeModelToCodeDomTypeAttributes(cc2Mock.Instance);

            cc2Mock.Verify();

            return result;
        }
    }
}
