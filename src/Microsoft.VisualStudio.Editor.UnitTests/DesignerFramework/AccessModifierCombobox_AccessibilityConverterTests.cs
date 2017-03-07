using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Editors;
using SD = Microsoft.VisualStudio.Editors.SettingsDesigner;

using Microsoft.VisualStudio.TestTools.MockObjects;
using Microsoft.VisualStudio.Shell.Interop;

using System.CodeDom;
using System.CodeDom.Compiler;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Editors.DesignerFramework;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;
using Microsoft.VisualBasic;
using Microsoft.CSharp;
//using Microsoft.VJSharp;
using System.ComponentModel;

namespace Microsoft.VisualStudio.Editors.UnitTests.DesignerFramework
{

    [TestClass]
    public class AccessModifierCombobox_AccessModifierConverterTests
    {

        [TestMethod]
        public void VBPublic()
        {
            AccessModifierConverter converter = new AccessModifierConverter(new VBCodeProvider());
            Assert.AreEqual("Public", converter.ConvertToString(AccessModifierConverter.Access.Public));
        }

        [TestMethod]
        public void VBFriend()
        {
            AccessModifierConverter converter = new AccessModifierConverter(new VBCodeProvider());
            Assert.AreEqual("Friend", converter.ConvertToString(AccessModifierConverter.Access.Friend));
        }

        [TestMethod]
        public void CSharpPublic()
        {
            AccessModifierConverter converter = new AccessModifierConverter(new CSharpCodeProvider());
            Assert.AreEqual("Public", converter.ConvertToString(AccessModifierConverter.Access.Public));
        }

        [TestMethod]
        public void CSharpFriend()
        {
            AccessModifierConverter converter = new AccessModifierConverter(new CSharpCodeProvider());
            Assert.AreEqual("Internal", converter.ConvertToString(AccessModifierConverter.Access.Friend));
        }

#if false //JSharp no longer supported
        [TestMethod]
        public void JSharpPublic()
        {
            AccessModifierCombobox.AccessModifierConverter converter = new AccessModifierCombobox.AccessModifierConverter(new VJSharpCodeProvider());
            Assert.AreEqual("Public", converter.ConvertToString(AccessModifierCombobox.Access.Public));
        }

        [TestMethod]
        public void JSharpFriend()
        {
            AccessModifierCombobox.AccessModifierConverter converter = new AccessModifierCombobox.AccessModifierConverter(new VJSharpCodeProvider());
            Assert.AreEqual("Package", converter.ConvertToString(AccessModifierCombobox.Access.Friend));
        }
#endif

        [TestMethod]
        public void UnknownPublic()
        {
            Mock<CodeDomProvider> mock = new Mock<CodeDomProvider>();
            mock.Implement("GetConverter", new object[] { typeof(MemberAttributes) }, new TypeConverter());
            AccessModifierConverter converter = new AccessModifierConverter(mock.Instance);
            Assert.AreEqual("Public", converter.ConvertToString(AccessModifierConverter.Access.Public));
        }

        [TestMethod]
        public void UnknownFriend()
        {
            Mock<CodeDomProvider> mock = new Mock<CodeDomProvider>();
            mock.Implement("GetConverter", new object[] { typeof(MemberAttributes) }, new TypeConverter());
            AccessModifierConverter converter = new AccessModifierConverter(mock.Instance);
            Assert.AreEqual("Internal", converter.ConvertToString(AccessModifierConverter.Access.Friend));
        }

    }
}
