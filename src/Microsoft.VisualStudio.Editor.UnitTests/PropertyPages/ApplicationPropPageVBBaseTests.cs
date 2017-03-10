// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;
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
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.Editors.PropertyPages;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Editors.UnitTests.PropertyPages;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPageTests
{
    [TestClass]
    [CLSCompliant(false)]
    public class ApplicationPropPageVBBaseTests 
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

        #region "Per-test goo"
        ApplicationPropPageVBWinForms _page;
        Microsoft_VisualStudio_Editors_PropertyPages_ApplicationPropPageVBWinFormsAccessor _accessor;

        [TestInitialize]
        public void TestInitialize()
        {
            _page = new ApplicationPropPageVBWinForms();
            _accessor = new Microsoft_VisualStudio_Editors_PropertyPages_ApplicationPropPageVBWinFormsAccessor(_page);
        }

        #endregion


        [TestMethod]
        public void Constructor()
        {
            ApplicationPropPageVBBase page = new ApplicationPropPageVBBase();
        }



    }
}

