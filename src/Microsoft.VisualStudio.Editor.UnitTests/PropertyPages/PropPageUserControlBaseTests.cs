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
    public class PropPageUserControlBaseTests
    {

#region GetRelativeDirectoryPath

        private void TestGetRelativeDirectoryPath(string basePath, string directoryPath, string expectedPath)
        {
            // This function should have been static, but it can't be changed now.
            PropPageUserControlBase page = new PropPageUserControlBase();
            Microsoft_VisualStudio_Editors_PropertyPages_PropPageUserControlBaseAccessor accessor = new Microsoft_VisualStudio_Editors_PropertyPages_PropPageUserControlBaseAccessor(page);
            string result = accessor.GetRelativeDirectoryPath(basePath, directoryPath);
            Assert.AreEqual(expectedPath, result);
        }

        
        [TestMethod]
        public void GetRelativeDirectoryPath_Nulls()
        {
            TestGetRelativeDirectoryPath(null, null, "");
        }

        [TestMethod]
        public void GetRelativeDirectoryPath_EmptyBase()
        {
            TestGetRelativeDirectoryPath("", "foo", @"foo\");
            TestGetRelativeDirectoryPath("", @"foo\", @"foo\");
            TestGetRelativeDirectoryPath("", @"..\foo", @"..\foo\");
            TestGetRelativeDirectoryPath("", @".\foo", @".\foo\");
            TestGetRelativeDirectoryPath("", @".\foo", @".\foo\");
            TestGetRelativeDirectoryPath("", @".\foo\bar", @".\foo\bar\");
            TestGetRelativeDirectoryPath("", @".\foo\bar\", @".\foo\bar\");
        }

        [TestMethod]
        public void GetRelativeDirectoryPath()
        {
            TestGetRelativeDirectoryPath(@"e:\", @"foo", @"foo\");
            TestGetRelativeDirectoryPath(@"e:\", @"e:\foo\", @"foo\");
            TestGetRelativeDirectoryPath(@"e:\foo", @"e:\foo\", @"");
            TestGetRelativeDirectoryPath(@"e:\foo\", @"e:\foo\", @"");
            TestGetRelativeDirectoryPath(@"e:\foo\bar\", @"e:\foo\", @"e:\foo\");
            TestGetRelativeDirectoryPath(@"e:\foo\bar\", @"e:\foo\bar", @"");
            TestGetRelativeDirectoryPath(@"e:\foo\bar\", @"e:\foo\bar\boo", @"boo\");
            TestGetRelativeDirectoryPath(@"e:\foo\bar\", @"e:\foo\bar\boo\hoo", @"boo\hoo\");
            TestGetRelativeDirectoryPath(@"e:\foo\bar\", @"e:\foo\bar\boo\hoo", @"boo\hoo\");
            TestGetRelativeDirectoryPath(@"e:\foo\bar\", @"f:\foo\bar\boo\hoo", @"f:\foo\bar\boo\hoo\");
        }

#endregion

    }

}
