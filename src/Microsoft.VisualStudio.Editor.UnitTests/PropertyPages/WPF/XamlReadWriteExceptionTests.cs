using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml;
using System.IO;
using System.Diagnostics;
using Microsoft.VisualStudio.Editors.UnitTests.PropertyPages;
using Microsoft.VisualStudio.Editors.PropertyPages;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;
using Microsoft.VisualStudio.Editors.PropertyPages.WPF;

namespace Microsoft.VisualStudio.Editors.PropertyPages.WPF
{
    [TestClass]
    [CLSCompliant(false)]
    public class XamlReadWriteExceptionTests
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
            XamlReadWriteException ex = new XamlReadWriteException("abc");

            Assert.AreEqual("abc", ex.Message);
            Assert.IsNull(ex.InnerException);
            Assert.AreEqual(0, ex.Data.Count);
        }

        [TestMethod]
        public void CanBeSerializedCorrectly()
        {
            XamlReadWriteException ex1 = new XamlReadWriteException("foo");

            MemoryStream ms = new MemoryStream();
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            bf.Serialize(ms, ex1);
            ms.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            Object o = bf.Deserialize(ms);

            Assert.IsInstanceOfType(o, typeof(XamlReadWriteException));
            XamlReadWriteException ex2 = (XamlReadWriteException)o;
            Assert.AreEqual(ex1.Data.Count, ex2.Data.Count);
            Assert.AreEqual(ex1.InnerException, ex2.InnerException);
            Assert.AreEqual(ex1.Message, ex2.Message);
            Assert.AreEqual(ex1.StackTrace, ex2.StackTrace);
            Assert.AreEqual(ex1.ToString(), ex2.ToString());
        }


    }
}
