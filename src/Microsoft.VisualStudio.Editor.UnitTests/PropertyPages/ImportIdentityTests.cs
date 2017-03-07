//------------------------------------------------------------------------------
// <copyright file="ImportIdentityTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
//     Information contained herein is proprietary and confidential.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Editors.PropertyPages;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages
{
    /// <summary>
    /// Unit-tests for the <see cref="Microsoft.VisualStudio.Editors.PropertyPages.ImportIdentity"/> helper class.
    /// </summary>
    [TestClass]
    public sealed class ImportIdentityTests
    {
        [TestMethod]
        [Description("Ensures design assumption that ImportIdentity class is internal holds true.")]
        public void NoPublicApiDesignAssumption()
        {
            Type type = typeof(ImportIdentity);
            Assert.IsFalse(type.IsPublic, "The class and tests were designed in assumption that ImportIdentity class is internal.");
        }

        [TestMethod]
        [Description("Verifies that constructor just works on all inputs.")]
        public void ConstructorTest()
        {
            // empty string
            new ImportIdentity("");

            // valid imports
            new ImportIdentity("System");
            new ImportIdentity("System.Xml");
            new ImportIdentity("File=System.IO.FileInfo");
            new ImportIdentity("<xmlns:name='http://www.url.foo'>");
            new ImportIdentity("IntList=System.Collection.Generic.List(Of Integer)");

            // syntax errors
            new ImportIdentity("Imports System");
            new ImportIdentity("Imports System.Xml");
            new ImportIdentity("Imports File=System.Io.FileInfo");
            new ImportIdentity("Imports <xmlns:name='http://foo'>");
            new ImportIdentity("$#^(PQ#@$\"");
            new ImportIdentity("System.");
            new ImportIdentity("File-=System.IO.FileInfo");
            new ImportIdentity("<xml");
            new ImportIdentity("<xmlns:");
            new ImportIdentity("<xmlns:name=");
            new ImportIdentity("<xmlns:name='http://foo");
            new ImportIdentity("<xmlns:name='http://foo'");
        }

        [TestMethod]
        [Description("Verifies functionality of the Equals method.")]
        public void EqualsTest()
        {
            // empty tests
            CheckEquals(true, "", "");
            CheckEquals(false, "System", "");
            CheckEquals(false, "System.Xml", "");
            CheckEquals(false, "File=System.IO.FileInfo", "");
            CheckEquals(false, "<xmlns:name=\"http://url\"", "");

            // simple identity and spacing tests
            CheckEquals(true, "System", "System");
            CheckEquals(true, "System", "System ");
            CheckEquals(true, "System", " System");
            CheckEquals(true, "System", " System ");
            CheckEquals(true, "System", "  SYSTEM  ");

            CheckEquals(true, "System.Xml", "System.Xml");
            CheckEquals(true, "System.Xml", "System.Xml ");
            CheckEquals(true, "System.Xml", " System.Xml");
            CheckEquals(true, "System.Xml", " System.Xml ");
            CheckEquals(true, "System.Xml", "  SYSTEM.XML  ");

            CheckEquals(true, "File=System.IO.FileInfo", "File=System.IO.FileInfo");
            CheckEquals(true, "File=System.IO.FileInfo", " File=System.IO.FileInfo");
            CheckEquals(true, "File=System.IO.FileInfo", "File =System.IO.FileInfo");
            CheckEquals(true, "File=System.IO.FileInfo", "File= System.IO.FileInfo");
            CheckEquals(true, "File=System.IO.FileInfo", "File=System.IO.FileInfo ");
            CheckEquals(true, "File=System.IO.FileInfo", "  FILE  =  SYSTEM.IO.FILEINFO  ");

            CheckEquals(true, "<xmlns='test'>", "<xmlns='test'>");
            CheckEquals(true, "<xmlns='test'>", " <xmlns='test'>");
            CheckEquals(true, "<xmlns='test'>", "< xmlns='test'>");
            CheckEquals(true, "<xmlns='test'>", "<xmlns ='test'>");
            CheckEquals(true, "<xmlns='test'>", "<xmlns= 'test'>");
            CheckEquals(true, "<xmlns='test'>", "<xmlns='test' >");
            CheckEquals(true, "<xmlns='test'>", "<xmlns='test'> ");
            CheckEquals(true, "<xmlns='test'>", "    <   XMLNS  =  'TEST'  >   ");

            CheckEquals(true, "<xmlns:name='test'>", "<xmlns:name='test'>");
            CheckEquals(true, "<xmlns:name='test'>", " <xmlns:name='test'>");
            CheckEquals(true, "<xmlns:name='test'>", "<xmlns :name='test'>");
            CheckEquals(true, "<xmlns:name='test'>", "<xmlns: name='test'>");
            CheckEquals(true, "<xmlns:name='test'>", "<xmlns:name ='test'>");
            CheckEquals(true, "<xmlns:name='test'>", "<xmlns:name= 'test'>");
            CheckEquals(true, "<xmlns:name='test'>", "<xmlns:name='test' >");
            CheckEquals(true, "<xmlns:name='test'>", "<xmlns:name='test'> ");
            CheckEquals(true, "<xmlns:name='test'>", "  <  XMLNS  :  name  =  'TEST'  >  ");

            // VB-specific tests
            CheckEquals(false, "System", "System.Xml");
            CheckEquals(false, "File", "File=System.IO.FileInfo");
            CheckEquals(false, "File", " File =System.IO.FileInfo");
            CheckEquals(false, " File ", "File=System.IO.FileInfo");
            CheckEquals(false, "System", "File=System.IO.FileInfo");
            CheckEquals(false, "System.IO.FileInfo", "File=System.IO.FileInfo");
            CheckEquals(true, "File=System.Xml.XmlElement", "File=System.IO.FileInfo");
            CheckEquals(true, "File=System.Xml.XmlElement", " File =System.IO.FileInfo");
            CheckEquals(false, "Life=System.IO.FileInfo", "File=System.IO.FileInfo");
            CheckEquals(true, " FILE =System.IO.FileInfo", "File=System.IO.FileInfo");
            CheckEquals(true, "_File=System.Xml", "_File=System.IO.FileInfo");

            // XML-specific tests
            CheckEquals(true, "<xmlns='test'>", "<XMLNS='test'>");
            CheckEquals(true, "<xmlns='test'>", "<xmlns=\"test\">");
            CheckEquals(true, "<xmlns='test'>", "<xmlns='anotherUrl'>");
            CheckEquals(false, "<xmlns='test'>", "<xmlns:name='test'>");
            CheckEquals(true, "<xmlns:name='test'>", "<xmlns:name='anotherUrl'>");
            CheckEquals(false, "<xmlns:name='test'>", "<xmlns:Name='test'>");
            CheckEquals(false, "<xmlns:name='test'>", "<xmlns:Name='TEST'>");
            CheckEquals(false, "<xmlns:name='test'>", "<xmlns:foo='test'>");
            CheckEquals(true, "<xmlns:name='test'>", "<xmlns:name=\"test\">");
            CheckEquals(true, "<xmlns:name=\"test\">", "<xmlns:name=\"test\">");

            // VB/XML tests
            CheckEquals(false, "System", "<xmlns:System='test'>");
            CheckEquals(false, "System=File", "<xmlns:System='test'>");
            CheckEquals(false, "xmlns", "<xmlns='test'>");
            CheckEquals(false, "SYSTEM", "<xmlns:system=\"SYSTEM\">");
        }

        private static void CheckEquals(bool expectedResult, string import1, string import2)
        {
            ImportIdentity id1 = new ImportIdentity(import1);
            ImportIdentity id2 = new ImportIdentity(import2);
            Assert.AreEqual<bool>(expectedResult, id1.Equals(id2), import1 + ".Equals(" + import2 + ") result is incorrect.");
            Assert.AreEqual<bool>(expectedResult, id2.Equals(id1), import2 + ".Equals(" + import1 + ") result is incorrect");
        }
    }
}
