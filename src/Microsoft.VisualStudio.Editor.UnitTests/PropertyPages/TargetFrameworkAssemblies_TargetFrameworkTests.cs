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
    public class TargetFrameworkAssemblies_TargetFrameworkTests
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
            TargetFrameworkAssemblies.TargetFramework tf = 
                new TargetFrameworkAssemblies.TargetFramework(1234, "My description");

            Assert.AreEqual(1234u, tf.Version);
            Assert.AreEqual("My description", tf.Description);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorNullDescription()
        {
            TargetFrameworkAssemblies.TargetFramework tf = 
                new TargetFrameworkAssemblies.TargetFramework(1234, null);
        }

        [TestMethod]
        public void ToStringReturnsDescription()
        {
            TargetFrameworkAssemblies.TargetFramework tf =
                new TargetFrameworkAssemblies.TargetFramework(1234, "My description");

            Assert.AreEqual("My description", tf.ToString());
            Assert.AreEqual(tf.Description, tf.ToString());
        }

    }
}
