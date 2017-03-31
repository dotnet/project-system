// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.MockObjects;
using Microsoft.VisualStudio.Editors.ResourceEditor;


namespace Microsoft.VisualStudio.Editors.UnitTests.ResourceEditor
{
    [TestClass]
    public class CsvEncoderTest
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


        [TestMethod()]
        public void TestIsSimpleStringNull()
        {
            // Call method under test
            string simpleString = null;
            bool result = CsvEncoder.IsSimpleString(null, CsvEncoder.EncodingType.Csv, ref simpleString);
            Assert.IsFalse(result);
        }

        [TestMethod()]
        public void TestIsSimpleStringEmpty()
        {
            // Call method under test
            string simpleString = null;
            bool result = CsvEncoder.IsSimpleString("", CsvEncoder.EncodingType.Csv, ref simpleString);
            Assert.IsFalse(result);
        }

        [TestMethod()]
        public void TestIsSimpleStringSimple()
        {
            // Call method under test
            string simpleString = null;
            bool result = CsvEncoder.IsSimpleString("Simple entry", CsvEncoder.EncodingType.Csv, ref simpleString);
            Assert.IsTrue(result);
            Assert.AreEqual("Simple entry", simpleString);
        }

        [TestMethod()]
        public void TestIsSimpleStringCsvHasComma()
        {
            // Call method under test
            string simpleString = null;
            bool result = CsvEncoder.IsSimpleString("Comma, entry", CsvEncoder.EncodingType.Csv, ref simpleString);
            Assert.IsFalse(result);
        }

        [TestMethod()]
        public void TestIsSimpleStringTabDelimitedHasComma()
        {
            // Call method under test
            string simpleString = null;
            bool result = CsvEncoder.IsSimpleString("Comma, entry", CsvEncoder.EncodingType.TabDelimited, ref simpleString);
            Assert.IsTrue(result);
            Assert.AreEqual("Comma, entry", simpleString);
        }

        [TestMethod()]
        public void TestIsSimpleStringCsvHasTab()
        {
            // Call method under test
            string simpleString = null;
            bool result = CsvEncoder.IsSimpleString("Tab\tentry", CsvEncoder.EncodingType.Csv, ref simpleString);
            Assert.IsTrue(result);
            Assert.AreEqual("Tab\tentry", simpleString);
        }

        [TestMethod()]
        public void TestIsSimpleStringTabDelimitedHasTab()
        {
            // Call method under test
            string simpleString = null;
            bool result = CsvEncoder.IsSimpleString("Tab\tentry", CsvEncoder.EncodingType.TabDelimited, ref simpleString);
            Assert.IsFalse(result);
        }

        [TestMethod()]
        public void TestIsSimpleStringSimpleHasQuotes()
        {
            // Call method under test
            string simpleString = null;
            bool result = CsvEncoder.IsSimpleString("\"quotes, but still simple\"", CsvEncoder.EncodingType.Csv, ref simpleString);
            Assert.IsTrue(result);
            Assert.AreEqual("quotes, but still simple", simpleString); //Quotes get removed
        }

        [TestMethod()]
        public void TestIsSimpleStringComplexHasQuotes()
        {
            // Call method under test
            string simpleString = null;
            bool result = CsvEncoder.IsSimpleString("\"quotes\", complex", CsvEncoder.EncodingType.Csv, ref simpleString);
            Assert.IsFalse(result);
        }

    }
}
