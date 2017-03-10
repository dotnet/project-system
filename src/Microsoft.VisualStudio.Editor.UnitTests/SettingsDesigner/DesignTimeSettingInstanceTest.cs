// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using DesignTimeSettingInstance = Microsoft.VisualStudio.Editors.SettingsDesigner.DesignTimeSettingInstance;
using IVsHierarchy = Microsoft.VisualStudio.Shell.Interop.IVsHierarchy;
using Microsoft.VisualStudio.TestTools.MockObjects;

namespace Microsoft.VisualStudio.Editors.UnitTests.SettingsDesigner
{
    /// <summary>
    /// Summary description for DesignTimeSettingInstanceTest
    /// </summary>
    [TestClass]
    public class DesignTimeSettingInstanceTest
    {
        public DesignTimeSettingInstanceTest()
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

        #region IsNameReadOnly
        [TestMethod]
        public void TestIsNameReadOnly_NoProvider()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            Assert.IsNull(instance.Provider);
            Assert.IsFalse(DesignTimeSettingInstance.IsNameReadOnly(instance));
        }

        [TestMethod]
        public void TestIsNameReadOnly_NullInstance()
        {
            Assert.IsFalse(DesignTimeSettingInstance.IsNameReadOnly(null));
        }

        [TestMethod]
        public void TestIsNameReadOnly_ProviderIsWebServiceProvider()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            instance.SetProvider(Microsoft.VisualStudio.Editors.PropertyPages.ServicesPropPageAppConfigHelper.ClientSettingsProviderName());
            Assert.IsTrue(DesignTimeSettingInstance.IsNameReadOnly(instance));
        }

        [TestMethod]
        public void TestIsNameReadOnly_RandomProvider()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            instance.SetProvider("ThisIsARandomProvider"); 
            Assert.IsFalse(DesignTimeSettingInstance.IsNameReadOnly(instance));
        }
#endregion

        #region IsTypeReadOnly

        [TestMethod]
        public void TestIsTypeReadOnly_NoProvider()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            Assert.IsNull(instance.Provider);
            Assert.IsFalse(DesignTimeSettingInstance.IsTypeReadOnly(instance));
        }


        [TestMethod]
        public void TestIsTypeReadOnly_NullInstance()
        {
            DesignTimeSettingInstance instance = null;
            Assert.IsFalse(DesignTimeSettingInstance.IsTypeReadOnly(instance));
        }

        [TestMethod]
        public void TestIsTypeReadOnly_WebProvider()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            instance.SetProvider(Microsoft.VisualStudio.Editors.PropertyPages.ServicesPropPageAppConfigHelper.ClientSettingsProviderName());
            Assert.IsTrue(DesignTimeSettingInstance.IsTypeReadOnly(instance));
        }

        [TestMethod]
        public void TestIsTypeReadOnly_RandomProvider()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            instance.SetProvider("ThisIsARandomProvider");
            Assert.IsFalse(DesignTimeSettingInstance.IsTypeReadOnly(instance));
        }
        #endregion

        #region IsRoamingReadOnly
        [TestMethod]
        public void TestIsRoamingReadOnly_NullInstance()
        {
            DesignTimeSettingInstance instance = null;
            Assert.IsFalse(DesignTimeSettingInstance.IsRoamingReadOnly(instance));
        }

        [TestMethod]
        public void TestIsRoamingReadOnly_WebProvider()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            instance.SetProvider(Microsoft.VisualStudio.Editors.PropertyPages.ServicesPropPageAppConfigHelper.ClientSettingsProviderName());
            Assert.IsTrue(DesignTimeSettingInstance.IsRoamingReadOnly(instance));
        }

        [TestMethod]
        public void TestIsRoamingReadOnly_ApplicationScope()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            instance.SetScope(DesignTimeSettingInstance.SettingScope.Application); 
            Assert.IsTrue(DesignTimeSettingInstance.IsRoamingReadOnly(instance));
        }

        [TestMethod]
        public void TestIsRoamingReadOnly_UserScope()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            instance.SetScope(DesignTimeSettingInstance.SettingScope.User);
            Assert.IsFalse(DesignTimeSettingInstance.IsRoamingReadOnly(instance));
        }

        [TestMethod]
        public void TestIsRoamingReadOnly_RandomProvider()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            instance.SetProvider("ThisIsARandomProvider");
            Assert.IsFalse(DesignTimeSettingInstance.IsRoamingReadOnly(instance));
        }
        #endregion

        #region ProjectSupportsUserScopedSettings
        [TestMethod]
        public void TestProjectSupportsUserScopedSettings_NullInstance()
        {
            DesignTimeSettingInstance instance = null;
            Assert.IsTrue(DesignTimeSettingInstance.ProjectSupportsUserScopedSettings(instance));
        }

        [TestMethod]
        public void TestProjectSupportsUserScopedSettings_InstanceNullSite()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            Assert.IsTrue(DesignTimeSettingInstance.ProjectSupportsUserScopedSettings(instance));
        }

        [TestMethod]
        public void TestProjectSupportsUserScopedSettings_InstanceSiteSupportingIVsHiearchy()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();

            Mock<System.ComponentModel.ISite> siteMock = new Mock<System.ComponentModel.ISite>();
            Mock<IVsHierarchy> hierarchyMock = new Mock<IVsHierarchy>();
            siteMock.Implement("GetService",
                               new object[] { typeof(IVsHierarchy) },
                               hierarchyMock.Instance);

            instance.Site = siteMock.Instance;
            Assert.IsTrue(DesignTimeSettingInstance.ProjectSupportsUserScopedSettings(instance));
        }

        
        [TestMethod]
        public void TestProjectSupportsUserScopedSettings_NullHierarchy()
        {
            Assert.IsTrue(DesignTimeSettingInstance.ProjectSupportsUserScopedSettings((IVsHierarchy) null));
        }
#endregion

        #region IsScopeReadOnly

        [TestMethod]
        public void TestIsScopeReadOnly_NullInstance()
        {
            DesignTimeSettingInstance instance = null;
            Assert.IsFalse(DesignTimeSettingInstance.IsScopeReadOnly(instance, true));
            Assert.IsTrue(DesignTimeSettingInstance.IsScopeReadOnly(instance, false));
        }

        [TestMethod]
        public void TestIsScopeReadOnly_WebProvider()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            instance.SetProvider(Microsoft.VisualStudio.Editors.PropertyPages.ServicesPropPageAppConfigHelper.ClientSettingsProviderName()); 
            Assert.IsTrue(DesignTimeSettingInstance.IsScopeReadOnly(instance, true));
            Assert.IsTrue(DesignTimeSettingInstance.IsScopeReadOnly(instance, false));
        }

        [TestMethod]
        public void TestIsScopeReadOnly_ConnectionString()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            instance.SetSettingTypeName("(Connection string)");
            instance.SetScope(DesignTimeSettingInstance.SettingScope.Application);
            Assert.IsTrue(DesignTimeSettingInstance.IsScopeReadOnly(instance, true));
            Assert.IsTrue(DesignTimeSettingInstance.IsScopeReadOnly(instance, false));
        }

        #endregion

        #region IsLocalFileSettingsProvider

        [TestMethod]
        public void TestIsLocalFileSettingsProvider_NullInstance()
        {
            DesignTimeSettingInstance instance = null;
            Assert.IsTrue(DesignTimeSettingInstance.IsLocalFileSettingsProvider(instance));
        }

        [TestMethod]
        public void TestIsLocalFileSettingsProvider_WebProvider()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            instance.SetProvider("WebProviderTypeName"); // UNDONE: Should use correct typename here!
            Assert.IsFalse(DesignTimeSettingInstance.IsLocalFileSettingsProvider(instance));
        }

        [TestMethod]
        public void TestIsLocalFileSettingsProvider_AllPermutationLocalFileSettingsProvider()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            instance.SetProvider(typeof(System.Configuration.LocalFileSettingsProvider).Name); 
            Assert.IsTrue(DesignTimeSettingInstance.IsLocalFileSettingsProvider(instance));
            instance.SetProvider(typeof(System.Configuration.LocalFileSettingsProvider).FullName);
            Assert.IsTrue(DesignTimeSettingInstance.IsLocalFileSettingsProvider(instance));
            instance.SetProvider("");
            Assert.IsTrue(DesignTimeSettingInstance.IsLocalFileSettingsProvider(instance));
        }

           #endregion

        #region IsConnectionString

        [TestMethod]
        public void TestIsConnectionString_NullInstance()
        {
            DesignTimeSettingInstance instance = null;
            Assert.IsFalse(DesignTimeSettingInstance.IsConnectionString(instance));
        }

        [TestMethod]
        public void TestIsConnectionString_True()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            instance.SetSettingTypeName("(Connection string)");
            Assert.IsTrue(DesignTimeSettingInstance.IsConnectionString(instance));
        }

        [TestMethod]
        public void TestIsConnectionString_False()
        {
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();
            instance.SetSettingTypeName("System.String");
            Assert.IsFalse(DesignTimeSettingInstance.IsConnectionString(instance));
        }


            #endregion

        #region ScopeConverter

        [TestMethod]
        public void TestScopeConverterCanConvertTo_StringAndOther()
        {
            DesignTimeSettingInstance.ScopeConverter converter = new DesignTimeSettingInstance.ScopeConverter();
            Assert.IsTrue(converter.CanConvertTo(null, typeof(string)));
            Assert.IsFalse(converter.CanConvertTo(null, typeof(System.ComponentModel.TypeDescriptor)));
        }

        [TestMethod]
        public void TestScopeConverterConvertTo_String()
        {
            DesignTimeSettingInstance.ScopeConverter converter = new DesignTimeSettingInstance.ScopeConverter();
            DesignTimeSettingInstance instance = new DesignTimeSettingInstance();

            Assert.AreEqual<string>("Application", (string) converter.ConvertTo(null, System.Globalization.CultureInfo.CurrentCulture, DesignTimeSettingInstance.SettingScope.Application, typeof(string)));
            Assert.AreEqual<string>("User", (string) converter.ConvertTo(null, System.Globalization.CultureInfo.CurrentCulture, DesignTimeSettingInstance.SettingScope.User, typeof(string)));

            Mock<System.ComponentModel.ITypeDescriptorContext> contextMock = new Mock<System.ComponentModel.ITypeDescriptorContext>();
            contextMock.Implement("get_Instance", instance);

            Assert.AreEqual<string>("Application", (string)converter.ConvertTo(contextMock.Instance, System.Globalization.CultureInfo.CurrentCulture, DesignTimeSettingInstance.SettingScope.Application, typeof(string)));
            Assert.AreEqual<string>("User", (string)converter.ConvertTo(contextMock.Instance, System.Globalization.CultureInfo.CurrentCulture, DesignTimeSettingInstance.SettingScope.User, typeof(string)));

            instance.SetProvider(Microsoft.VisualStudio.Editors.PropertyPages.ServicesPropPageAppConfigHelper.ClientSettingsProviderName());
            Assert.AreEqual<string>("Application (Web)", (string)converter.ConvertTo(contextMock.Instance, System.Globalization.CultureInfo.CurrentCulture, DesignTimeSettingInstance.SettingScope.Application, typeof(string)));
            Assert.AreEqual<string>("User (Web)", (string)converter.ConvertTo(contextMock.Instance, System.Globalization.CultureInfo.CurrentCulture, DesignTimeSettingInstance.SettingScope.User, typeof(string)));

            instance.SetProvider("Random provider"); 
            Assert.AreEqual<string>("Application", (string)converter.ConvertTo(contextMock.Instance, System.Globalization.CultureInfo.CurrentCulture, DesignTimeSettingInstance.SettingScope.Application, typeof(string)));
            Assert.AreEqual<string>("User", (string)converter.ConvertTo(contextMock.Instance, System.Globalization.CultureInfo.CurrentCulture, DesignTimeSettingInstance.SettingScope.User, typeof(string)));

        }
        
        [TestMethod]
        public void TestScopeConverterCanConvertFrom_StringAndOther()
        {
            DesignTimeSettingInstance.ScopeConverter converter = new DesignTimeSettingInstance.ScopeConverter();
            Assert.IsTrue(converter.CanConvertFrom(null, typeof(string)));
            Assert.IsFalse(converter.CanConvertFrom(null, typeof(System.ComponentModel.TypeDescriptor)));
        }

        #endregion
    }
}
