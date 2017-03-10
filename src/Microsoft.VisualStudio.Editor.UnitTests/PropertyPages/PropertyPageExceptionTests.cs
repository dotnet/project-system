// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

namespace Microsoft.VisualStudio.Editors.PropertyPages
{
    [TestClass]
    [CLSCompliant(false)]
    public class PropertyPageExceptionTests
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
            PropertyPageException ex1 = new PropertyPageException("abc");
            Assert.AreEqual("abc", ex1.Message);
            Assert.IsNull(ex1.InnerException);
            Assert.AreEqual(0, ex1.Data.Count);

            PropertyPageException ex2 = new PropertyPageException("def", ex1);
            Assert.AreEqual("def", ex2.Message);
            Assert.AreEqual(0, ex2.Data.Count);
            Assert.AreSame(ex1, ex2.InnerException);

            PropertyPageException ex3 = new PropertyPageException("ghi", "link", ex1);
            Assert.AreEqual("ghi", ex3.Message);
            Assert.AreEqual("link", ex3.HelpLink);
            Assert.AreEqual(0, ex3.Data.Count);
            Assert.AreSame(ex1, ex3.InnerException);

            PropertyPageException ex4 = new PropertyPageException("jkl", "link2");
            Assert.AreEqual("jkl", ex4.Message);
            Assert.AreEqual("link2", ex4.HelpLink);
            Assert.AreEqual(0, ex4.Data.Count);
            Assert.IsNull(ex4.InnerException);
        }

        [TestMethod]
        public void CanBeSerializedCorrectly()
        {
            PropertyPageException exInner = new PropertyPageException("inner");
            PropertyPageException ex1 = new PropertyPageException("foo","link",exInner);

            MemoryStream ms = new MemoryStream();
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            bf.Serialize(ms, ex1);
            ms.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            Object o = bf.Deserialize(ms);

            Assert.IsInstanceOfType(o, typeof(PropertyPageException));
            PropertyPageException ex2 = (PropertyPageException)o;
            Assert.AreEqual(ex1.Data.Count, ex2.Data.Count);
            Assert.AreEqual(ex1.InnerException != null, ex2.InnerException != null);
            Assert.AreEqual(ex1.Message, ex2.Message);
            Assert.AreEqual(ex1.HelpLink, ex2.HelpLink);
            Assert.AreEqual(ex1.StackTrace, ex2.StackTrace);
            Assert.AreEqual(ex1.ToString(), ex2.ToString());
        }


    }
}
