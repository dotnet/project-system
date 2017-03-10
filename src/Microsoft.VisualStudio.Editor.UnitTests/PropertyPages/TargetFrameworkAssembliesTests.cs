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

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages
{
    [TestClass]
    [CLSCompliant(false)]
    public class TargetFrameworkAssembliesTests
    {
        static VsTargetFrameworkAssembliesFake s_targetFrameworkAssembliesFake_1_2_3 = new VsTargetFrameworkAssembliesFake();

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

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            s_targetFrameworkAssembliesFake_1_2_3.Fake_AddVersions1_2_3();
        }

        #endregion

        [TestMethod]
        public void GetSupportedTargetFrameworkAssemblyVersions()
        {
            uint[] supportedVersions = 
                Microsoft_VisualStudio_Editors_PropertyPages_TargetFrameworkAssembliesAccessor.GetSupportedTargetFrameworkAssemblyVersions(
                    s_targetFrameworkAssembliesFake_1_2_3);

            CollectionAssert.AreEqual(new uint[] { 1, 2, 3 }, supportedVersions);
        }

        [TestMethod]
        public void GetSupportedTargetFrameworkAssemblyVersionsEmpty()
        {
            VsTargetFrameworkAssembliesFake targetFrameworkAssembliesFake = new VsTargetFrameworkAssembliesFake();

            uint[] supportedVersions = 
                Microsoft_VisualStudio_Editors_PropertyPages_TargetFrameworkAssembliesAccessor.GetSupportedTargetFrameworkAssemblyVersions(
                    targetFrameworkAssembliesFake);

            CollectionAssert.AreEqual(new uint[] { }, supportedVersions);
        }

        [TestMethod]
        public void GetSupportedTargetFrameworkAssemblies()
        {
            IEnumerable<TargetFrameworkAssemblies.TargetFramework> enumSupportedVersions = TargetFrameworkAssemblies.GetSupportedTargetFrameworkAssemblies(s_targetFrameworkAssembliesFake_1_2_3);
            List<TargetFrameworkAssemblies.TargetFramework> supportedVersions
                = new List<TargetFrameworkAssemblies.TargetFramework>(enumSupportedVersions);
            object[] actualResults = new object[] {
                supportedVersions[0].Version, supportedVersions[0].Description,
                supportedVersions[1].Version, supportedVersions[1].Description,
                supportedVersions[2].Version, supportedVersions[2].Description
            };

            CollectionAssert.AreEqual(
                new object[] { 
                    1u, "Version 1",
                    2u, "Version 2",
                    3u, "Version 3" },
                actualResults);
        }

    }
}
