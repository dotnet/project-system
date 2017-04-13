// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.VisualStudio.TestTools.MockObjects;
using Microsoft.VisualStudio.Designer.Interfaces;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Design;


namespace Microsoft.VisualStudio.Editors.UnitTests.ResxGenerator
{
    /// <summary>
    /// Summary description for ResxFileCodeGeneratorTest
    /// </summary>
    [TestClass]
    public class ResxFileCodeGeneratorTest
    {
        private static string ResxFileCodeGeneratorTypeNameFormatString = "Microsoft.VisualStudio.Design.Serialization.{0}";
        private static string ResxFileCodeGeneratorAssemblyName = "Microsoft.VisualStudio.Design, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

        private static IVsSingleFileGenerator CreatePublicResxFileCodeGenerator()
        {
            string className = string.Format(ResxFileCodeGeneratorTypeNameFormatString, "PublicResXFileCodeGenerator");
            System.Runtime.Remoting.ObjectHandle oh = (System.Runtime.Remoting.ObjectHandle) System.Activator.CreateInstance(ResxFileCodeGeneratorAssemblyName,
                                                                                                                             className);
            return (IVsSingleFileGenerator)oh.Unwrap();
        }

        private static IVsSingleFileGenerator CreatePublicMyVbResxFileCodeGenerator()
        {
            string className = string.Format(ResxFileCodeGeneratorTypeNameFormatString, "PublicVBMyResXFileCodeGenerator");
            System.Runtime.Remoting.ObjectHandle oh = (System.Runtime.Remoting.ObjectHandle)System.Activator.CreateInstance(ResxFileCodeGeneratorAssemblyName,
                                                                                                                             className);
            return (IVsSingleFileGenerator)oh.Unwrap();
        }

        private static IVsSingleFileGenerator CreateResxFileCodeGenerator()
        {
            string className = string.Format(ResxFileCodeGeneratorTypeNameFormatString, "ResXFileCodeGenerator");
            System.Runtime.Remoting.ObjectHandle oh = (System.Runtime.Remoting.ObjectHandle)System.Activator.CreateInstance(ResxFileCodeGeneratorAssemblyName,
                                                                                                                             className);
            return (IVsSingleFileGenerator)oh.Unwrap();
        }

        private static IVsSingleFileGenerator CreateVBMyResxFileCodeGenerator()
        {
            string className = string.Format(ResxFileCodeGeneratorTypeNameFormatString, "VBMyResXFileCodeGenerator");
            System.Runtime.Remoting.ObjectHandle oh = (System.Runtime.Remoting.ObjectHandle)System.Activator.CreateInstance(ResxFileCodeGeneratorAssemblyName,
                                                                                                                             className);
            return (IVsSingleFileGenerator)oh.Unwrap();
        }

        private static string CreateDummyResourceFile()
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            System.IO.StreamWriter sw = new System.IO.StreamWriter(ms, new System.Text.UTF8Encoding(false));
            System.Resources.ResXResourceWriter writer = new System.Resources.ResXResourceWriter(sw);
            writer.AddResource("MyLittleResource", "MyLittleResourceValue");
            writer.Generate();
            ms.Flush();
            ms.Seek(0, System.IO.SeekOrigin.Begin);
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GenerateCode(IVsSingleFileGenerator generator, string filePath) 
        {
            uint pcbOutput;
            IntPtr[] outputFileContents = new IntPtr[1];

            ((Microsoft.VisualStudio.OLE.Interop.IObjectWithSite) generator).SetSite(new ResxSingleFileGeneratorSite(EnvDTE.CodeModelLanguageConstants.vsCMLanguageVB));
            generator.Generate(filePath, CreateDummyResourceFile(), "", outputFileContents, out pcbOutput, null);

            byte[] data = new byte[pcbOutput];
            System.Runtime.InteropServices.Marshal.Copy(outputFileContents[0], data, 0, (int) pcbOutput);
            return System.Text.Encoding.UTF8.GetString(data);
        }


        public ResxFileCodeGeneratorTest()
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
        [Ignore] //UNDONE: getting assertion
        public void TestPublicResxFileCodeGeneratorMethod()
        {
            string filePath = @"C:\temp\dummy.resx";
            string expectedClassName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            IVsSingleFileGenerator generator = CreatePublicResxFileCodeGenerator();
            Assert.IsNotNull(generator);

            string generatedCode = GenerateCode(generator, filePath);
         
            Assert.IsTrue(generatedCode.Contains(String.Format("Public Class {0}", expectedClassName)));
        }

        [TestMethod]
        [Ignore] //UNDONE: getting assertion
        public void TestPublicVBMyResxFileCodeGeneratorMethod()
        {
            string filePath = @"C:\temp\dummy.resx";
            string expectedClassName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            IVsSingleFileGenerator generator = CreatePublicMyVbResxFileCodeGenerator();

            Assert.IsNotNull(generator);

            string generatedCode = GenerateCode(generator, filePath);
         
            Assert.IsTrue(generatedCode.Contains(String.Format("Public Module {0}", expectedClassName)));

            Assert.IsNotNull(generator);
        }

        [TestMethod]
        public void TestResxFileCodeGeneratorMethod()
        {
            string filePath = @"C:\temp\dummy.resx";
            string expectedClassName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            IVsSingleFileGenerator generator = CreateResxFileCodeGenerator();

            Assert.IsNotNull(generator);

            string generatedCode = GenerateCode(generator, filePath);
         
            Assert.IsTrue(generatedCode.Contains(String.Format("Friend Class {0}", expectedClassName)));

            Assert.IsNotNull(generator);
        }

        [TestMethod]
        public void TestResxVBMyFileCodeGeneratorMethod()
        {
            string filePath = @"C:\temp\dummy.resx";
            string expectedClassName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            IVsSingleFileGenerator generator = CreateVBMyResxFileCodeGenerator();

            Assert.IsNotNull(generator);

            string generatedCode = GenerateCode(generator, filePath);
         
            Assert.IsTrue(generatedCode.Contains(String.Format("Friend Module {0}", expectedClassName)));

            Assert.IsNotNull(generator);
        }
        
        private class ResxSingleFileGeneratorSite : Microsoft.VisualStudio.OLE.Interop.IServiceProvider, Microsoft.VisualStudio.Shell.Interop.IVsBrowseObject
        {
            private Mock<IVsHierarchy> _hierarchyMock;

            private System.CodeDom.Compiler.CodeDomProvider _codeDomProvider;

            private Mock<IVSMDCodeDomProvider> _mdCodeDomProvideMock;

            private Mock<GlobalObjectProviderFactory> _gopFactory;

            public ResxSingleFileGeneratorSite(string language)
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

            protected virtual IVsHierarchy Hierarchy
            {
                get
                {
                    if (_hierarchyMock == null)
                    {
                        _hierarchyMock = new Mock<IVsHierarchy>();

                        _hierarchyMock.Implement("GetProperty", 
                                        new object[] { MockConstraint.IsAnything<uint>(), (int)__VSHPROPID.VSHPROPID_DefaultNamespace, null },
                                        new object[] { 0u, (int)__VSHPROPID.VSHPROPID_DefaultNamespace, "ThisIsTheDefaultNamespace" },
                                        VSConstants.S_OK);

                        _hierarchyMock.Implement("GetSite",
                                        new object[] { MockConstraint.IsAnything<Microsoft.VisualStudio.OLE.Interop.IServiceProvider>() },
                                        new object[] { this },
                                        VSConstants.S_OK);
                        
                 }

                    return _hierarchyMock.Instance;
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

            protected virtual GlobalObjectProviderFactory GlobalObjectProviderFactoryObject
            {
                get
                {
                    if (_gopFactory == null)
                    {
                        _gopFactory = new Mock<GlobalObjectProviderFactory>();
                        _gopFactory.Implement("GetProviders", (object)(new GlobalObjectProvider[0]));
                    }
                    return _gopFactory.Instance;
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
                else if (guidService.Equals(typeof(GlobalObjectProviderFactory).GUID))
                {
                    ppvObject = System.Runtime.InteropServices.Marshal.GetIUnknownForObject(GlobalObjectProviderFactoryObject);
                    return VSConstants.S_OK;
                }


                return VSConstants.E_NOINTERFACE;
            }

            #endregion

            #region IVsBrowseObject Members

            int IVsBrowseObject.GetProjectItem(out IVsHierarchy pHier, out uint pItemid)
            {
                pHier = Hierarchy;
                pItemid = 4711;
                return VSConstants.S_OK;
            }

            #endregion
        }
}
}
