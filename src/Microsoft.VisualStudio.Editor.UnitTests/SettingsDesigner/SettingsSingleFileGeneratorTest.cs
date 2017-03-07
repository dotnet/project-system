using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.VisualStudio.TestTools.MockObjects;

using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.Shell.Interop;

using SettingsSingleFileGenerator = Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator;
using PublicSettingsSingleFileGenerator = Microsoft.VisualStudio.Editors.SettingsDesigner.PublicSettingsSingleFileGenerator;

namespace Microsoft.VisualStudio.Editors.UnitTests.SettingsDesigner
{
    /// <summary>
    /// Summary description for SettingsSingleFileGeneratorTest
    /// </summary>
    [TestClass]
    public class SettingsSingleFileGeneratorTest
    {
        public SettingsSingleFileGeneratorTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

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

        [TestMethod]
        public void TestGenerateDefaultInternalVB()
        {
            int hr;
            System.CodeDom.CodeCompileUnit compileUnit;
            System.CodeDom.CodeTypeDeclaration typeDeclaration;

            TestGenerate<SettingsSingleFileGenerator>(EnvDTE.CodeModelLanguageConstants.vsCMLanguageVB,
                                                      out hr,
                                                      out compileUnit,
                                                      out typeDeclaration);
            Assert.AreEqual(hr, VSConstants.S_OK);
            Assert.IsNotNull(compileUnit);
            Assert.IsNotNull(typeDeclaration);
            Assert.AreEqual(System.Reflection.TypeAttributes.Sealed | System.Reflection.TypeAttributes.NestedAssembly, typeDeclaration.TypeAttributes);
        }

        [TestMethod]
        public void TestGenerateDefaultInternalCSharp()
        {
            int hr;
            System.CodeDom.CodeCompileUnit compileUnit;
            System.CodeDom.CodeTypeDeclaration typeDeclaration;

            TestGenerate<SettingsSingleFileGenerator>(EnvDTE.CodeModelLanguageConstants.vsCMLanguageCSharp,
                                                      out hr,
                                                      out compileUnit,
                                                      out typeDeclaration);
            Assert.AreEqual(hr, VSConstants.S_OK);
            Assert.IsNotNull(compileUnit);
            Assert.IsNotNull(typeDeclaration);
            Assert.AreEqual(System.Reflection.TypeAttributes.Sealed | System.Reflection.TypeAttributes.NestedAssembly, typeDeclaration.TypeAttributes);
        }

        [TestMethod]
        public void TestGenerateDefaultPublicVB()
        {
            int hr;
            System.CodeDom.CodeCompileUnit compileUnit;
            System.CodeDom.CodeTypeDeclaration typeDeclaration;

            TestGenerate<PublicSettingsSingleFileGenerator>(EnvDTE.CodeModelLanguageConstants.vsCMLanguageVB,
                                                      out hr,
                                                      out compileUnit,
                                                      out typeDeclaration);
            Assert.AreEqual(hr, VSConstants.S_OK);
            Assert.IsNotNull(compileUnit);
            Assert.IsNotNull(typeDeclaration);
            Assert.AreEqual(System.Reflection.TypeAttributes.Sealed | System.Reflection.TypeAttributes.Public, typeDeclaration.TypeAttributes);
        }

        [TestMethod]
        public void TestGenerateDefaultPublicCSharp()
        {
            int hr;
            System.CodeDom.CodeCompileUnit compileUnit;
            System.CodeDom.CodeTypeDeclaration typeDeclaration;

            TestGenerate<PublicSettingsSingleFileGenerator>(EnvDTE.CodeModelLanguageConstants.vsCMLanguageCSharp,
                                                      out hr,
                                                      out compileUnit,
                                                      out typeDeclaration);
            Assert.AreEqual(hr, VSConstants.S_OK);
            Assert.IsNotNull(compileUnit);
            Assert.IsNotNull(typeDeclaration);
            Assert.AreEqual(System.Reflection.TypeAttributes.Sealed | System.Reflection.TypeAttributes.Public, typeDeclaration.TypeAttributes);
        }

        private void TestGenerate<G>(string language,
                                     out int hr,
                                     out System.CodeDom.CodeCompileUnit compileUnit,
                                     out System.CodeDom.CodeTypeDeclaration typeDeclaration)
                            where G : Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGeneratorBase, new()
        {
            IntPtr[] fileContents;
            uint bytesOutput;
            TestGenerate<G>(language, out hr, out fileContents, out bytesOutput, out compileUnit, out typeDeclaration);
        }


        private void TestGenerate<G>(string language,
                                     out int hr,
                                     out IntPtr[] fileContents,
                                     out uint bytesOutput,
                                     out System.CodeDom.CodeCompileUnit compileUnit,
                                     out System.CodeDom.CodeTypeDeclaration typeDeclaration)
                            where G : Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGeneratorBase, new()
        {
            SettingsSingleFileGeneratorSite site = new SettingsSingleFileGeneratorSite(language);
            string inputFile = site.DefaultSettingsFilePath;

            Mock<G> generatorMock = new Mock<G>();

            System.CodeDom.CodeCompileUnit actualCompileUnit = null;
            System.CodeDom.CodeTypeDeclaration actualTypeDeclaration = null;
            
            generatorMock.Implement("GetProjectRootNamespace", "MyLittleNamespace");
            generatorMock.Implement("AddRequiredReferences", new object[] { (Shell.Interop.IVsGeneratorProgress)null });
            generatorMock.Implement("OnCompileUnitCreated",
                                    new object[] { MockConstraint.IsAnything<System.CodeDom.CodeCompileUnit>(), MockConstraint.IsAnything<System.CodeDom.CodeTypeDeclaration>() },
                                    delegate(object obj, System.Reflection.MethodInfo method, object[] arguments)
                                    {
                                        actualCompileUnit = (System.CodeDom.CodeCompileUnit) arguments[0];
                                        actualTypeDeclaration = (System.CodeDom.CodeTypeDeclaration) arguments[1];
                                        return null;
                                    });

            IVsSingleFileGenerator generator = generatorMock.Instance;

            ((Microsoft.VisualStudio.OLE.Interop.IObjectWithSite)generator).SetSite(site);

            fileContents = new IntPtr[] { new IntPtr() };
            bytesOutput = 0;
            hr = generator.Generate(inputFile, "", "", fileContents, out bytesOutput, null);

            compileUnit = actualCompileUnit;
            typeDeclaration = actualTypeDeclaration;
        }

        private class SettingsSingleFileGeneratorSite : OLE.Interop.IServiceProvider
        {
            private string _defaultSettingsFilePath = @"C:\Temp\Settings.settings";

            private Mock<IVsHierarchy> _hierarchyMock;

            private System.CodeDom.Compiler.CodeDomProvider _codeDomProvider;

            private Mock<IVSMDCodeDomProvider> _mdCodeDomProvideMock;

            private Mock<EnvDTE.Project> _envDteProjectMock;

            private Mock<EnvDTE.CodeModel> _envDteCodeModelMock;

            private Mock<EnvDTE.ProjectItem> _envDteProjectItemMock;

            public SettingsSingleFileGeneratorSite(string language)
            {
                if (language == EnvDTE.CodeModelLanguageConstants.vsCMLanguageVB)
                {
                    _codeDomProvider = new Microsoft.VisualBasic.VBCodeProvider();
                }
                else if (language == EnvDTE.CodeModelLanguageConstants.vsCMLanguageCSharp)
                {
                    _codeDomProvider = new Microsoft.CSharp.CSharpCodeProvider();
                }
                else if (language == EnvDTE80.CodeModelLanguageConstants2.vsCMLanguageJSharp)
                {
                    System.Diagnostics.Debug.Fail("Not yet implemented!");
                }
            }

            public string DefaultSettingsFilePath
            {
                get
                {
                    return _defaultSettingsFilePath;
                }
            }

            protected virtual IVsHierarchy Hierarchy
            {
                get
                {
                    if (_hierarchyMock == null)
                    {
                        _hierarchyMock = new Mock<IVsHierarchy>(new System.Type[] { typeof(IVsProjectSpecialFiles) });

                        Microsoft.VisualStudio.TestTools.MockObjects.MethodId methodId = new Microsoft.VisualStudio.TestTools.MockObjects.MethodId(typeof(Microsoft.VisualStudio.Shell.Interop.IVsProjectSpecialFiles), "GetFile");
                        _hierarchyMock.Implement(methodId,
                                        new object[] { (int)Microsoft.VisualStudio.Shell.Interop.__PSFFILEID2.PSFFILEID_AppSettings, (uint)__PSFFLAGS.PSFF_FullPath, 0u, null },
                                        new object[] { 0, 0u, 42u, _defaultSettingsFilePath }, 0);

                        _hierarchyMock.Implement("GetProperty",
                                        new object[] { MockConstraint.IsAnything<uint>(), (int)__VSHPROPID.VSHPROPID_ExtObject, null },
                                        new object[] { 0u, (int)__VSHPROPID.VSHPROPID_ExtObject, DTEProject },
                                        VSConstants.S_OK);

                    }

                    return _hierarchyMock.Instance;
                }

            }

            protected virtual EnvDTE.Project DTEProject
            {
                get
                {
                    if (_envDteProjectMock == null)
                    {
                        _envDteProjectMock = new Mock<EnvDTE.Project>();
                        _envDteProjectMock.Implement("get_CodeModel", CodeModel);
                        _envDteProjectMock.Implement("get_Object", (object)null);
                    }
                    return _envDteProjectMock.Instance;
                }
            }

            protected virtual EnvDTE.CodeModel CodeModel
            {
                get
                {
                    if (_envDteCodeModelMock == null)
                    {
                        _envDteCodeModelMock = new Mock<EnvDTE.CodeModel>();
                        _envDteCodeModelMock.Implement("get_Language", EnvDTE.CodeModelLanguageConstants.vsCMLanguageVB);
                    }
                    return _envDteCodeModelMock.Instance;
                }
            }

            protected virtual EnvDTE.ProjectItem ProjectItem
            {
                get
                {
                    if (_envDteProjectItemMock == null)
                    {
                        _envDteProjectItemMock = new Mock<EnvDTE.ProjectItem>();
                        _envDteProjectItemMock.Implement("get_ContainingProject", _envDteProjectMock.Instance);
                    }
                    return _envDteProjectItemMock.Instance;
                }
            }

            protected virtual IVSMDCodeDomProvider VSMDCodeDomProvider
            {
                get
                {
                    if (_mdCodeDomProvideMock == null)
                    {
                        _mdCodeDomProvideMock = new Mock<IVSMDCodeDomProvider>();
                        _mdCodeDomProvideMock.Implement("get_CodeDomProvider", CodeDomProvider);
                    }
                    return _mdCodeDomProvideMock.Instance;
                }
            }

            protected virtual System.CodeDom.Compiler.CodeDomProvider CodeDomProvider
            {
                get
                {
                    return _codeDomProvider;
                }
            }

            #region IServiceProvider Members

            int Microsoft.VisualStudio.OLE.Interop.IServiceProvider.QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
            {
                ppvObject = IntPtr.Zero;
                if (guidService.Equals(typeof(IVsHierarchy).GUID))
                {
                    ppvObject = System.Runtime.InteropServices.Marshal.GetIUnknownForObject(Hierarchy);
                    return VSConstants.S_OK;
                }
                else if (guidService.Equals(typeof(IVSMDCodeDomProvider).GUID))
                {
                    ppvObject = System.Runtime.InteropServices.Marshal.GetIUnknownForObject(VSMDCodeDomProvider);
                    return VSConstants.S_OK;
                }
                else if (guidService.Equals(typeof(EnvDTE.ProjectItem).GUID))
                {
                    ppvObject = System.Runtime.InteropServices.Marshal.GetIUnknownForObject(ProjectItem);
                    return VSConstants.S_OK;
                }


                return VSConstants.E_NOINTERFACE;
            }

            #endregion
        }
    }

}
