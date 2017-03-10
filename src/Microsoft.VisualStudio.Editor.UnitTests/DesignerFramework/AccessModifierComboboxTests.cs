// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Editors;
using SD = Microsoft.VisualStudio.Editors.SettingsDesigner;
using RE = Microsoft.VisualStudio.Editors.ResourceEditor;

using Microsoft.VisualStudio.TestTools.MockObjects;
using Microsoft.VisualStudio.Shell.Interop;

using System.ComponentModel.Design;
using Microsoft.VisualStudio.Editors.DesignerFramework;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;
using System.CodeDom.Compiler;
using Microsoft.VisualStudio.Designer.Interfaces;

namespace Microsoft.VisualStudio.Editors.UnitTests.DesignerFramework
{

    [TestClass]
    public class AccessModifierComboboxTests
    {
        public static IServiceProvider CreateServiceProviderWithIVSMDCodeDomProvider(CodeDomProvider codeProvider)
        {
            Mock<IVSMDCodeDomProvider> vsmdCodeDomProviderMock = new Mock<IVSMDCodeDomProvider>();
            vsmdCodeDomProviderMock.Implement("get_CodeDomProvider", codeProvider);

            ServiceProviderMock spMock = new ServiceProviderMock();
            if (codeProvider != null)
            {
                spMock.Fake_AddService(typeof(IVSMDCodeDomProvider), vsmdCodeDomProviderMock.Instance);
            }
            else
            {
                spMock.Fake_AddService(typeof(IVSMDCodeDomProvider), null);
            }

            return spMock.Instance;
        }



        [TestMethod]
        public void DisabledWhenNotEditable()
        {
            SD.SettingsDesigner designer = new SD.SettingsDesigner();
            SubclassedAccessModifierCombobox combo =
                new SubclassedAccessModifierCombobox(
                    designer,
                    designer,
                    new ProjectItemWithCustomToolFake(""),
                    false,
                    null);

            Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor accessor =
                new Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor(combo);

            Assert.IsFalse(accessor.ShouldBeEnabled());
        }

        [TestMethod]
        public void DisabledWhenCustomToolNotSupported()
        {
            SD.SettingsDesigner designer = new SD.SettingsDesigner();
            SubclassedAccessModifierCombobox combo =
                new SubclassedAccessModifierCombobox(
                    designer,
                    designer,
                    new ProjectItemFake(),
                    true,
                    null);

            Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor accessor =
                new Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor(combo);

            Assert.IsFalse(accessor.ShouldBeEnabled());
        }

        [TestMethod]
        public void DisabledWhenCustomToolNotRecognizedAndNonEmpty()
        {
            SD.SettingsDesigner designer = new SD.SettingsDesigner();
            SubclassedAccessModifierCombobox combo =
                new SubclassedAccessModifierCombobox(
                    designer,
                    designer,
                    new ProjectItemWithCustomToolFake("WhoAmI"),
                    true,
                    null);

            Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor accessor =
                new Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor(combo);

            Assert.IsFalse(accessor.ShouldBeEnabled());
        }

        [TestMethod]
        public void EnabledWhenCustomToolRecognizedButNotInDropdownList()
        {
            SD.SettingsDesigner designer = new SD.SettingsDesigner();
            SubclassedAccessModifierCombobox combo =
                new SubclassedAccessModifierCombobox(
                    designer,
                    designer,
                    new ProjectItemWithCustomToolFake("ResXFileCodeGenerator"),
                    true,
                    null);
            combo.AddCodeGeneratorEntry("VbMyResourcesResXFileCodeGenerator", "Expected generator");
            combo.AddRecognizedCustomToolValue("ResXFileCodeGenerator");

            Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor accessor =
                new Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor(combo);

            Assert.IsTrue(accessor.ShouldBeEnabled());
        }

        [TestMethod]
        public void EnabledWhenCustomToolEmpty()
        {
            SD.SettingsDesigner designer = new SD.SettingsDesigner();
            SubclassedAccessModifierCombobox combo =
                new SubclassedAccessModifierCombobox(
                    designer,
                    designer,
                    new ProjectItemWithCustomToolFake(""),
                    true,
                    null);

            Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor accessor =
                new Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor(combo);

            Assert.IsTrue(accessor.ShouldBeEnabled());
        }

        [TestMethod]
        public void EnabledWhenCustomToolRecognized()
        {
            SD.SettingsDesigner designer = new SD.SettingsDesigner();
            SubclassedAccessModifierCombobox combo =
                new SubclassedAccessModifierCombobox(
                    designer,
                    designer,
                    new ProjectItemWithCustomToolFake("Recognized generator"),
                    true,
                    null);
            combo.AddCodeGeneratorEntry("Hi, Mom", "Recognized generator");

            Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor accessor =
                new Microsoft_VisualStudio_Editors_DesignerFramework_AccessModifierComboboxAccessor(combo);

            Assert.IsTrue(accessor.ShouldBeEnabled());
        }

    }






    #region private class SubclassedAccessModifierCombobox
    class SubclassedAccessModifierCombobox : AccessModifierCombobox
    {
        bool isDesignerEditable;
        public SubclassedAccessModifierCombobox(BaseRootDesigner rootDesigner, IServiceProvider serviceProvider, EnvDTE.ProjectItem projectItem, bool isDesignerEditable, string namespaceToOverrideIfCustomToolIsEmpty)
            : base(rootDesigner, serviceProvider, projectItem, namespaceToOverrideIfCustomToolIsEmpty)
        {
            this.isDesignerEditable = isDesignerEditable;
        }

        protected override bool IsDesignerEditable()
        {
            return isDesignerEditable;
        }

        protected override bool CustomToolRegistered
        {
            get
            {
                return true;
            }
        }
    }
    #endregion

    #region
    class HierarchyWithDTEProject : IVsHierarchy
    {
        private EnvDTE.Project m_project = new Mock<EnvDTE.Project>().Instance;

        #region IVsHierarchy Members

        int IVsHierarchy.AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out uint pdwCookie)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.Close()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.GetCanonicalName(uint itemid, out string pbstrName)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.GetGuidProperty(uint itemid, int propid, out Guid pguid)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.GetNestedHierarchy(uint itemid, ref Guid iidHierarchyNested, out IntPtr ppHierarchyNested, out uint pitemidNested)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.GetProperty(uint itemid, int propid, out object pvar)
        {
            if (itemid == (uint)VSITEMID.ROOT && propid == (int)__VSHPROPID.VSHPROPID_ExtObject)
            {
                pvar = m_project;
                return VSConstants.S_OK;
            }

            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.GetSite(out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.ParseCanonicalName(string pszName, out uint pitemid)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.QueryClose(out int pfCanClose)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.SetGuidProperty(uint itemid, int propid, ref Guid rguid)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.SetProperty(uint itemid, int propid, object var)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.UnadviseHierarchyEvents(uint dwCookie)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.Unused0()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.Unused1()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.Unused2()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.Unused3()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsHierarchy.Unused4()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
    #endregion

}
