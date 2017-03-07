//-----------------------------------------------------------------------
// <copyright file="MyExtensibilitySettingsTest.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
//     Information Contained Herein is Proprietary and Confidential.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.MockObjects;
using Microsoft.VisualStudio.Editors.MyExtensibility;
using Microsoft.VisualStudio.Editors.MyExtensibility.EnvDTE90Interop;
using Util = Microsoft.VisualStudio.Editors.UnitTests.MyExtensibility.MyExtensibilityTestUtil;

namespace Microsoft.VisualStudio.Editors.UnitTests.MyExtensibility
{
    /// <summary>
    /// Test class for Microsoft.VisualStudio.Editors.MyExtensibility.MyExtensibilitySettings.
    /// </summary>  
    [TestClass()]
    public class MyExtensibilitySettingsTest
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

        private TestContext testContextInstance;
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
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

        #region "Constructor tests"

        [TestMethod()]
        public void MyExtSettingsConstructorNull()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings(null);
        }

        [TestMethod()]
        public void MyExtSettingsConstructorEmptyString()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings(string.Empty);
        }

        [TestMethod()]
        public void MyExtSettingsConstructorBlank()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("          ");
        }

        [TestMethod()]
        public void MyExtSettingsConstructorInvalidChars()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("\"<>|");
        }

        #endregion

        #region "GetAssemblyAutoAdd/GetAssemblyAutoRemove"

        /// <summary>
        /// Test MyExtensionSettings GetAssemblyAutoAdd/GetAssemblyAutoRemove with a valid settings file
        /// in which assembly full names does NOT contain culture and public key token.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetAssemblyAutoAddAutoRemoveExistingFile1()
        {
            MyExtSettingsGetAssemblyAutoAddAutoRemoveExistingFile(SETTINGSFILE1);
        }

        /// <summary>
        /// Test MyExtensionSettings GetAssemblyAutoAdd/GetAssemblyAutoRemove with a settings file 
        /// in which assembly full names contain culture and public key token.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetAssemblyAutoAddAutoRemoveExistingFile2()
        {
            MyExtSettingsGetAssemblyAutoAddAutoRemoveExistingFile(SETTINGSFILE2);
        }

        /// <summary>
        /// Test MyExtensionSettings GetAssemblyAutoAdd/GetAssemblyAutoRemove with a settings file
        /// in which assembly option is case insensitive.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetAssemblyAutoAddAutoRemoveAssemblyOptionCaseInsensitive()
        {
            MyExtSettingsGetAssemblyAutoAddAutoRemoveExistingFile(SETTINGSFILE1_ASMOPTION_CASEINSENSITIVE);
        }

        /// <summary>
        /// Test MyExtensionSettings GetAssemblyAutoAdd/GetAssemblyAutoRemove with a settings file
        /// in which assembly option is numbers.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetAssemblyAutoAddAutoRemoveAssemblyOptionNumber()
        {
            MyExtSettingsGetAssemblyAutoAddAutoRemoveExistingFile(SETTINGSFILE1_ASMOPTION_NUMBER);
        }

        /// <summary>
        /// Helper methods for 2 tests above.
        /// </summary>
        private static void MyExtSettingsGetAssemblyAutoAddAutoRemoveExistingFile(string[] fileContent)
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);
            if (CreateFile(filePath, fileContent))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

                AssemblyOption vb9AutoAdd = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Yes == vb9AutoAdd, "AssemblyOption.Yes != vb9AutoAdd");

                AssemblyOption vb9AutoRemove = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9AutoRemove, "AssemblyOption.Prompt != vb9AutoRemove");

                AssemblyOption speechAutoAdd = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_SYSTEM_SPEECH);
                Assert.IsTrue(AssemblyOption.Prompt == speechAutoAdd, "AssemblyOption.Prompt != speechAutoAdd");

                AssemblyOption speechAutoRemove = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_SYSTEM_SPEECH);
                Assert.IsTrue(AssemblyOption.No == speechAutoRemove, "AssemblyOption.No != speechAutoRemove");

                File.Delete(filePath);
            }
            else
            {
                Assert.Fail("Could not create settings file.");
            }
        }

        /// <summary>
        /// Test MyExtensionsSettings GetAssemblyAutoAdd/GetAssemblyAutoRemove with non-existed settings file 
        /// and unknown assembly name.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetAssemblyAutoAddAutoRemove2()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");

            AssemblyOption bogusAutoAdd = extSettings.GetAssemblyAutoAdd("Bogus Assembly");
            Assert.IsTrue(AssemblyOption.Prompt == bogusAutoAdd, "AssemblyOption.Prompt != bogusAutoAdd");

            AssemblyOption bogusAutoRemove = extSettings.GetAssemblyAutoRemove("Another Bogus Assembly");
            Assert.IsTrue(AssemblyOption.Prompt == bogusAutoRemove, "AssemblyOption.Prompt != bogusAutoRemove");
        }

        /// <summary>
        /// Test MyExtensionSettings GetAssemblyAutoAdd/GetAssemblyAutoRemove case-insensitive.
        /// Settings file contains the Pascal casing, parameters have random casing.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetAssemblyAutoAddAutoRemoveCaseInsensitive1()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);
            if (CreateFile(filePath, SETTINGSFILE1))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);
                Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB, 
                    new Mock<Template>[] { Util.CreateTemplateMockSample1(), Util.CreateTemplateMockSample2() });

                string vb9Name1 = "microsoft.VISUALBASIC.fx3.5Extensions, VERSION=8.0.0.0";
                AssemblyOption vb9AutoAdd = extSettings.GetAssemblyAutoAdd(vb9Name1);
                Assert.IsTrue(AssemblyOption.Yes == vb9AutoAdd, string.Format("Failed to get \"{0}\" AutoAdd!", vb9Name1));

                string vb9Name2 = "MICROSOFT.VisualBasic.fx3.5extensions, VeRsIon=8.0.0.0";
                AssemblyOption vb9AutoRemove = extSettings.GetAssemblyAutoRemove(vb9Name2);
                Assert.IsTrue(AssemblyOption.Prompt == vb9AutoRemove, string.Format("Failed to get \"{0}\" AutoRemove!", vb9Name2));

                string speechName1 = "system.SPEECH, version=3.0.0.0, culture=NEUTRAL, publickeyTOKEN=31bf3856ad364e35";
                AssemblyOption speechAutoAdd = extSettings.GetAssemblyAutoAdd(speechName1);
                Assert.IsTrue(AssemblyOption.Prompt == speechAutoAdd, string.Format("Failed to get \"{0}\" AutoAdd!", speechName1));

                string speechName2 = "SyStEm.SpEeCh, VERSION=3.0.0.0, culture=ja-JP";
                AssemblyOption speechAutoRemove = extSettings.GetAssemblyAutoRemove(speechName2);
                Assert.IsTrue(AssemblyOption.No == speechAutoRemove, string.Format("Failed to get \"{0}\" AutoRemove!", speechName2));

                File.Delete(filePath);
            }
            else 
            {
                Assert.Fail("Could not create settings file.");
            }
        }

        /// <summary>
        /// Test MyExtensionSettings GetAssemblyAutoAdd/GetAssemblyAutoRemove case-insensitive.
        /// Settings file contains the random casing, parameters have Pascal casing.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetAssemblyAutoAddAutoRemoveCaseInsensitive2()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);
            if (CreateFile(filePath, SETTINGSFILE2_CASEINSENSITIVE))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);
                Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                    new Mock<Template>[] { Util.CreateTemplateMockSample1(), Util.CreateTemplateMockSample2() });

                AssemblyOption vb9AutoAdd = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Yes == vb9AutoAdd, string.Format("Failed to get \"{0}\" AutoAdd!", Util.ASMNAME_VBRUN9));

                AssemblyOption vb9AutoRemove = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9AutoRemove, string.Format("Failed to get \"{0}\" AutoRemove!", Util.ASMNAME_VBRUN9));

                AssemblyOption speechAutoAdd = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_SYSTEM_SPEECH);
                Assert.IsTrue(AssemblyOption.Prompt == speechAutoAdd, string.Format("Failed to get \"{0}\" AutoAdd!", Util.ASMNAME_SYSTEM_SPEECH));

                AssemblyOption speechAutoRemove = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_SYSTEM_SPEECH);
                Assert.IsTrue(AssemblyOption.No == speechAutoRemove, string.Format("Failed to get \"{0}\" AutoRemove!", Util.ASMNAME_SYSTEM_SPEECH));

                File.Delete(filePath);
            }
            else
            {
                Assert.Fail("Could not create settings file.");
            }
        }
        #endregion

        #region "SetAssemblyAutoAdd/SetAssemblyAutoRemove"

        /// <summary>
        /// Test MyExtensionSettings SetAssemblyAutoAdd with unexisted path and valid assembly.
        /// </summary>
        [TestMethod]
        public void MyExtSettingsSetAssemblyAutoAddWithNotExistedPath()
        {
            this.MyExtSettingsSetAssemblyWithNotExistedPath(true);
        }

        /// <summary>
        /// Test MyExtensionSettings SetAssemblyAutoRemove with unexisted path and valid assembly.
        /// </summary>
        [TestMethod]
        public void MyExtSettingsSetAssemblyAutoRemoveWithNotExistedPath()
        {
            this.MyExtSettingsSetAssemblyWithNotExistedPath(false);
        }

        /// <summary>
        /// Helper method for the 2 tests above.
        /// </summary>
        private void MyExtSettingsSetAssemblyWithNotExistedPath(bool autoAdd)
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");

            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { Util.CreateTemplateMockSample1() });

            if (autoAdd)
            {
                extSettings.SetAssemblyAutoAdd(Util.ASMNAME_VBRUN9, true);
            }
            else
            {
                extSettings.SetAssemblyAutoRemove(Util.ASMNAME_VBRUN9, true);
            }
        }

        /// <summary>
        /// Test MyExtensionSettings SetAssemblyAutoAdd with known path and uknown assembly.
        /// </summary>
        [TestMethod]
        public void MyExtSettingsSetAssemblyAutoAddWithUnknownAssembly()
        {
            this.MyExtSettingsSetAssemblyWithUnknownAssembly(true);
        }

        /// <summary>
        /// Test MyExtensionSettings SetAssemblyAutoRemove with known path and uknown assembly.
        /// </summary>
        [TestMethod]
        public void MyExtSettingsSetAssemblyAutoRemoveWithUnknownAssembly()
        {
            this.MyExtSettingsSetAssemblyWithUnknownAssembly(false);
        }

        /// <summary>
        /// Helper method for the 2 tests above.
        /// </summary>
        private void MyExtSettingsSetAssemblyWithUnknownAssembly(bool autoAdd)
        {
            string dirPath = Environment.CurrentDirectory;
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { Util.CreateTemplateMockSample1() });

            if (autoAdd)
            {
                extSettings.SetAssemblyAutoAdd(Util.ASMNAME_SYSTEM_SPEECH, true);
            }
            else 
            {
                extSettings.SetAssemblyAutoRemove(Util.ASMNAME_SYSTEM_SPEECH, true);
            }
            
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);
            bool settingsFileExist = File.Exists(filePath);
            Assert.IsTrue(settingsFileExist, "Even though the assembly is unknown, settings file should still be created.");

            if (settingsFileExist)
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Test MyExtensionSettings.SetAssemblyAutoAdd/AutoRemove with valid assembly name
        /// verify that a file is written out correctly.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsSetAssemblyAutoAddAutoRemove()
        {
            string dirPath = Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB, 
                new Mock<Template>[] { 
                    Util.CreateTemplateMockSample1(), 
                    Util.CreateTemplateMockSample2(), 
                    Util.CreateTemplateMockSample3()});

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

            extSettings.SetAssemblyAutoAdd(Util.ASMNAME_VBRUN9, true);
            extSettings.SetAssemblyAutoRemove(Util.ASMNAME_SYSTEM_SPEECH, false);

            bool settingsFileExist = File.Exists(filePath);
            Assert.IsTrue(settingsFileExist, "Settings file exists.");

            if (settingsFileExist)
            {
                VerifyFile(filePath, SETTINGSFILE1);
                File.Delete(filePath);
            }
        }

        ///// <summary>
        ///// Set assembly auto add / auto remove to the same current value (default value)
        ///// and verify that no file is written.
        ///// </summary>
        //[TestMethod()]
        //public void MyExtSettingsSetAssemblyAutoAddAutoRemoveNoFileWritten()
        //{
        //    string dirPath = Environment.CurrentDirectory;
        //    string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

        //    Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
        //        new Mock<Template>[] { 
        //            Util.CreateTemplateMockSample1(), 
        //            Util.CreateTemplateMockSample2(), 
        //            Util.CreateTemplateMockSample3()});

        //    MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

        //    extSettings.SetAssemblyAutoAdd(Util.ASMNAME_VBRUN9, false);
        //    extSettings.SetAssemblyAutoRemove(Util.ASMNAME_SYSTEM_SPEECH, false);

        //    bool settingsFileExist = File.Exists(filePath);
        //    Assert.IsFalse(settingsFileExist, "Setting file should not exist.");
        //}

        /// <summary>
        /// Test to verify that setting assembly auto add / auto remove is case-insensitive.
        /// Settings file is Pascal case. Parameters are random case.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsSetAssemblyAutoAddAutoRemoveCaseInsensitive1()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);
            if (CreateFile(filePath, SETTINGSFILE1))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);
                Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                    new Mock<Template>[] { Util.CreateTemplateMockSample1(), Util.CreateTemplateMockSample2() });

                string vb9Name1 = "microsoft.VISUALBASIC.fx3.5Extensions, VERSION=8.0.0.0";
                extSettings.SetAssemblyAutoAdd(vb9Name1, false);

                string vb9Name2 = "MICROSOFT.VisualBasic.fx3.5extensions, VeRsIon=8.0.0.0";
                extSettings.SetAssemblyAutoRemove(vb9Name2, true);

                string speechName1 = "system.SPEECH, version=3.0.0.0, culture=NEUTRAL, publickeyTOKEN=31bf3856ad364e35";
                extSettings.SetAssemblyAutoAdd(speechName1, true);

                string speechName2 = "SyStEm.SpEeCh, VERSION=3.0.0.0, culture=ja-JP";
                extSettings.SetAssemblyAutoRemove(speechName2, false);

                bool settingsFileExist = File.Exists(filePath);
                Assert.IsTrue(settingsFileExist, "Settings file does not exists.");
                if (settingsFileExist)
                {
                    VerifyFile(filePath, SETTINGSFILE1_AFTER);
                    File.Delete(filePath);
                }
            }
            else
            {
                Assert.Fail("Could not create settings file.");
            }
        }

        /// <summary>
        /// Test to verify that setting assembly auto add / auto remove is case-insensitive.
        /// Settings file is random case. Parameters are Pascal case.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsSetAssemblyAutoAddAutoRemoveCaseInsensitive2()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);
            if (CreateFile(filePath, SETTINGSFILE2_CASEINSENSITIVE))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);
                Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                    new Mock<Template>[] { Util.CreateTemplateMockSample1(), Util.CreateTemplateMockSample2() });

                extSettings.SetAssemblyAutoAdd(Util.ASMNAME_VBRUN9, false);

                extSettings.SetAssemblyAutoRemove(Util.ASMNAME_VBRUN9, true);

                extSettings.SetAssemblyAutoAdd(Util.ASMNAME_SYSTEM_SPEECH, true);

                extSettings.SetAssemblyAutoRemove(Util.ASMNAME_SYSTEM_SPEECH, false);

                bool settingsFileExist = File.Exists(filePath);
                Assert.IsTrue(settingsFileExist, "Settings file does not exists.");
                if (settingsFileExist)
                {
                    VerifyFile(filePath, SETTINGSFILE2_CASEINSENSITIVE_AFTER);
                    File.Delete(filePath);
                }
            }
            else
            {
                Assert.Fail("Could not create settings file.");
            }
        }

        #endregion

        #region "GetExtensionTemplates"

        /// <summary>
        /// GetExtensionTemplates(String,Project)
        /// Project mocks Solution3.GetProjectItemTemplates() returns null.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetExtensionTemplatesNoResult1()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB, null);
            List<MyExtensionTemplate> extensionTemplates = 
                extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);
            Assert.IsNull(extensionTemplates, "extensionTemplates should be NULL!");
        }

        /// <summary>
        /// GetExtensionTemplates(String,Project)
        /// Project mocks Solution3.GetProjectItemTemplates() returns empty collection.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetExtensionTemplatesNoResult2()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB, new Mock<Template>[] { });
            List<MyExtensionTemplate> extensionTemplates =
                extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);
            Assert.IsNull(extensionTemplates, "extensionTemplates should be NULL!");
        }

        /// <summary>
        /// GetExtensionTemplates(String,Project)
        /// Project mocks Solution3.GetProjectItemTemplates() throws System.IO.FileNotFoundException.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetExtensionTemplatesNoResult3()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            Mock<Project> projectMock = CreateProjectMockWithException(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB, 
                new System.IO.FileNotFoundException());
            List<MyExtensionTemplate> extensionTemplates = 
                extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);
            Assert.IsNull(extensionTemplates, "extensionTemplates should be NULL!");
        }

        /// <summary>
        /// GetExtensionTemplates(String,Project,String)
        /// Project mocks Solution3.GetProjectItemTemplates() returns null.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetExtensionTemplatesNoResult4()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB, null);
            List<MyExtensionTemplate> extensionTemplates =
                extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance, Util.ASMNAME_VBRUN9);
            Assert.IsNull(extensionTemplates, "extensionTemplates should be NULL!");
        }

        /// <summary>
        /// GetExtensionTemplates(String,Project,String)
        /// Project mocks Solution3.GetProjectItemTemplates() returns empty collection.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetExtensionTemplatesNoResult5()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB, new Mock<Template>[] { });
            List<MyExtensionTemplate> extensionTemplates =
                extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance, Util.ASMNAME_VBRUN9);
            Assert.IsNull(extensionTemplates, "extensionTemplates should be NULL!");
        }

        /// <summary>
        /// GetExtensionTemplates(String,Project,String)
        /// Project mocks Solution3.GetProjectItemTemplates() throws System.IO.FileNotFoundException.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetExtensionTemplatesNoResult6()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            Mock<Project> projectMock = CreateProjectMockWithException(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB, 
                new System.IO.FileNotFoundException());
            List<MyExtensionTemplate> extensionTemplates =
                extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance, Util.ASMNAME_VBRUN9);
            Assert.IsNull(extensionTemplates, "extensionTemplates should be NULL!");
        }

        /// <summary>
        /// Project mock returns 2 Template instance, 1 with valid custom data, 1 with invalid custom data.
        /// Verify that only 1 extension template is returned with correct details.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetExtensionTemplatesOneResult()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");

            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB, new Mock<Template>[] {
                Util.CreateTemplateMockSample1(),
                Util.CreateTemplateMockSample2()
            });
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsTrue(1 == extensionTemplates.Count, "Found 1 extension template.");
            if (1 == extensionTemplates.Count)
            {
                MyExtensionTemplate extensionTemplate = extensionTemplates[0];
                Util.VerifyExtensionTemplateSample1(extensionTemplate);
            }
        }

        /// <summary>
        /// Project mock returns 5 Templates triggerred by the same assembly, 1 without version, 2 with version 1,
        /// 2 with version 2. Verify that we can get the extension template back with different assembly version.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetExtensionTemplateMixAssemblyVersion()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");

            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB, 
                new Mock<Template>[] { 
                    Util.CreateTemplateMockSample4(),
                    Util.CreateTemplateMockSample5(), 
                    Util.CreateTemplateMockSample6(), 
                    Util.CreateTemplateMockSample7(), 
                    Util.CreateTemplateMockSample8()});

            List<MyExtensionTemplate> extensionTemplates1 = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance, Util.ASMNAME_ASMWTVER_1);
            Assert.IsTrue(null != extensionTemplates1 && 3 == extensionTemplates1.Count, "Could not find extension template for " + Util.ASMNAME_ASMWTVER_1);
            List<string> extensionIDs1 = new List<string>();
            extensionIDs1.Add("Company.Product.AssemblyWithoutVersion.MyExtension");
            extensionIDs1.Add("Company.Product.AssemblyWithoutVersion.MyExtension11");
            extensionIDs1.Add("Company.Product.AssemblyWithoutVersion.MyExtension12");
            foreach (MyExtensionTemplate extensionTemplate in extensionTemplates1)
            { 
                if (extensionIDs1.Contains(extensionTemplate.ID))
                {
                    extensionIDs1.Remove(extensionTemplate.ID);
                }
            }
            Assert.IsTrue(0 == extensionIDs1.Count, "First set of extension IDs do not match.");

            List<MyExtensionTemplate> extensionTemplates2 = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance, Util.ASMNAME_ASMWTVER_2);
            Assert.IsTrue(null != extensionTemplates2 && 3 == extensionTemplates2.Count, "Could not find extension template for " + Util.ASMNAME_ASMWTVER_2);
            List<string> extensionIDs2 = new List<string>();
            extensionIDs2.Add("Company.Product.AssemblyWithoutVersion.MyExtension");
            extensionIDs2.Add("Company.Product.AssemblyWithoutVersion.MyExtension21");
            extensionIDs2.Add("Company.Product.AssemblyWithoutVersion.MyExtension22");
            foreach (MyExtensionTemplate extensionTemplate in extensionTemplates2)
            {
                if (extensionIDs2.Contains(extensionTemplate.ID))
                {
                    extensionIDs2.Remove(extensionTemplate.ID);
                }
            }
            Assert.IsTrue(0 == extensionIDs2.Count, "Second set of extension IDs do not match.");
        }

        /// <summary>
        /// Project mock returns a Template instance in which triggering assembly does not have version.
        /// Verify that we can get the extension template back with different assembly version.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetExtensionTemplateNoAssemblyVersion()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");

            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { Util.CreateTemplateMockSample4() });

            List<MyExtensionTemplate> extensionTemplates1 = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance, Util.ASMNAME_ASMWTVER_1);
            Assert.IsTrue(null != extensionTemplates1 && 1 == extensionTemplates1.Count, "Could not find extension template for " + Util.ASMNAME_ASMWTVER_1);

            List<MyExtensionTemplate> extensionTemplates2 = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance, Util.ASMNAME_ASMWTVER_2);
            Assert.IsTrue(null != extensionTemplates2 && 1 == extensionTemplates2.Count, "Could not find extension template for " + Util.ASMNAME_ASMWTVER_2);
        }

        /// <summary>
        /// Test GetExtensiontTemplates case insensitive. 
        /// Template custom data is Pascal case. Assembly parameter name is random case.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetExtensionTemplatesCaseInsensitive1()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");

            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB, new Mock<Template>[] {
                Util.CreateTemplateMockSample1(),
                Util.CreateTemplateMockSample3()
            });

            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(
                PROJECTKIND_VB, projectMock.Instance, Util.ASMNAME_VBRUN9_CASE_INSENSITIVE);
            if (null != extensionTemplates)
            {
                if (1 == extensionTemplates.Count)
                {
                    Util.VerifyExtensionTemplateSample1(extensionTemplates[0]);
                }
                else 
                {
                    Assert.Fail(string.Format(
                        "Expect 1 extension for {0}. Result is {1}.", 
                        Util.ASMNAME_VBRUN9_CASE_INSENSITIVE, extensionTemplates.Count));
                }
            }
            else 
            {
                Assert.Fail("GetExtensionTemplates return null for " + Util.ASMNAME_VBRUN9_CASE_INSENSITIVE);
            }

            extensionTemplates = extSettings.GetExtensionTemplates(
                PROJECTKIND_VB, projectMock.Instance, Util.ASMNAME_SYSTEM_SPEECH_CASE_INSENSITIVE);
            if (null != extensionTemplates)
            {
                if (1 == extensionTemplates.Count)
                {
                    Util.VerifyExtensionTemplateSample3(extensionTemplates[0]);
                }
                else
                {
                    Assert.Fail(string.Format(
                        "Expect 1 extension for {0}. Result is {1}.",
                        Util.ASMNAME_SYSTEM_SPEECH_CASE_INSENSITIVE, extensionTemplates.Count));
                }
            }
            else
            {
                Assert.Fail("GetExtensionTemplates return null for " + Util.ASMNAME_SYSTEM_SPEECH_CASE_INSENSITIVE);
            }
        }

        /// <summary>
        /// Test GetExtensiontTemplates case insensitive. 
        /// Template custom data is random case. Assembly parameter name is Pascal case.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetExtensionTemplatesCaseInsensitive2()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");

            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB, new Mock<Template>[] {
                Util.CreateTemplateMockSample1CaseInsensitive(),
                Util.CreateTemplateMockSample3CaseInsensitive()
            });

            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(
                PROJECTKIND_VB, projectMock.Instance, Util.ASMNAME_VBRUN9);
            if (null != extensionTemplates)
            {
                if (1 == extensionTemplates.Count)
                {
                    Util.VerifyExtensionTemplateSample1CaseInsensitive(extensionTemplates[0]);
                }
                else
                {
                    Assert.Fail(string.Format(
                        "Expect 1 extension for {0}. Result is {1}.",
                        Util.ASMNAME_VBRUN9, extensionTemplates.Count));
                }
            }
            else
            {
                Assert.Fail("GetExtensionTemplates return null for " + Util.ASMNAME_VBRUN9);
            }

            extensionTemplates = extSettings.GetExtensionTemplates(
                PROJECTKIND_VB, projectMock.Instance, Util.ASMNAME_SYSTEM_SPEECH);
            if (null != extensionTemplates)
            {
                if (1 == extensionTemplates.Count)
                {
                    Util.VerifyExtensionTemplateSample3CaseInsensitive(extensionTemplates[0]);
                }
                else
                {
                    Assert.Fail(string.Format(
                        "Expect 1 extension for {0}. Result is {1}.",
                        Util.ASMNAME_SYSTEM_SPEECH, extensionTemplates.Count));
                }
            }
            else
            {
                Assert.Fail("GetExtensionTemplates return null for " + Util.ASMNAME_SYSTEM_SPEECH);
            }
        }

        /// <summary>
        /// GetExtensionTemplates with assembly name should only return the latest version of the template.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetExtensionTemplatesBasedOnTemplateVersion01()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");

            string id = "My.BasedOnTemplateVersion01";
            Mock<Template> templateMock1 = Util.CreateTemplateMock("MyTemplateVersion1", id, "1", "System.Drawing");
            Mock<Template> templateMock2 = Util.CreateTemplateMock("MyTemplateVersion1.0", id, "1.0", "System.Drawing");
            Mock<Template> templateMock3 = Util.CreateTemplateMock("MyTemplateVersion1.5", id, "1.5", "System.Drawing");
            Mock<Template> templateMock4 = Util.CreateTemplateMock("MyTemplateVersion1.5.0", id, "1.5.0", "System.Drawing");
            Mock<Template> templateMock5 = Util.CreateTemplateMock("MyTemplateVersion1.5.0.6", id, "1.5.0.6", "System.Drawing");
            Mock<Template> templateMock6 = Util.CreateTemplateMock("MyTemplateVersion1.5.0.7", id, "1.5.0.7", "System.Drawing");

            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB, 
                new Mock<Template>[] { templateMock1, templateMock2, templateMock3, templateMock4, templateMock5, templateMock6});

            List<MyExtensionTemplate> extTemplates = extSettings.GetExtensionTemplates(
                PROJECTKIND_VB, projectMock.Instance, "System.Drawing, Version=2.0.0.0");
            if (null == extTemplates)
            {
                Assert.Fail("Expect 1 extension. Got NULL.");
            }
            else
            {
                Assert.IsTrue(1 == extTemplates.Count, string.Format("Expect 1 extension. Got {0}.", extTemplates.Count));
                MyExtensionTemplate extTemplate = extTemplates[0];
                Assert.IsTrue(extTemplate.Version.Equals(new Version(1,5,0,7)), 
                    string.Format("Expect version 1.5.0.7. Got {0}.", extTemplate.Version.ToString()));
            }
        }

        /// <summary>
        /// GetExtensionTemplates with assembly name should only return the latest version of the template.
        /// </summary>
        [TestMethod()]
        public void MyExtSettingsGetExtensionTemplatesBasedOnTemplateVersion02()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");

            string id = "My.BasedOnTemplateVersion01";
            Mock<Template> templateMock1 = Util.CreateTemplateMock("MyTemplateVersion1", id, "1", "System.Drawing");
            Mock<Template> templateMock2 = Util.CreateTemplateMock("MyTemplateVersion1.0", id, "1.0", "System.Drawing");
            Mock<Template> templateMock3 = Util.CreateTemplateMock("MyTemplateVersion1.5", id, "1.5", "System.Drawing, Version=1.0.0.0");
            Mock<Template> templateMock4 = Util.CreateTemplateMock("MyTemplateVersion1.5.0", id, "1.5.0", "System.Drawing, Version=1.0.0.0");
            Mock<Template> templateMock5 = Util.CreateTemplateMock("MyTemplateVersion1.5.0.6", id, "1.5.0.6", "System.Drawing, Version=2.0.0.0");
            Mock<Template> templateMock6 = Util.CreateTemplateMock("MyTemplateVersion1.5.0.7", id, "1.5.0.7", "System.Drawing, Version=2.0.0.0");

            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { templateMock1, templateMock2, templateMock3, templateMock4, templateMock5, templateMock6 });

            List<MyExtensionTemplate> extTemplates = extSettings.GetExtensionTemplates(
                PROJECTKIND_VB, projectMock.Instance, "System.Drawing, Version=2.0.0.0");
            if (null == extTemplates)
            {
                Assert.Fail("Expect 1 extension. Got NULL.");
            }
            else
            {
                Assert.IsTrue(1 == extTemplates.Count, string.Format("Expect 1 extension. Got {0}.", extTemplates.Count));
                MyExtensionTemplate extTemplate = extTemplates[0];
                Assert.IsTrue(extTemplate.Version.Equals(new Version(1, 5, 0, 7)),
                    string.Format("Expect version 1.5.0.7. Got {0}.", extTemplate.Version.ToString()));
            }
        }

        [TestMethod()]
        public void MyExtSettingsGetExtensionTemplatesInvalidAssemblyName01()
        {
            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            Mock<Template> templateMock = Util.CreateTemplateMock("MyInvalidAsmName", "My.InvalidAsmName", 
                "1.0.0.0", "System.Drawing, Version=Invalid.Version, AND SOMETHING ELSE");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB, 
                new Mock<Template>[] { templateMock });

            List<MyExtensionTemplate> extTemplates = extSettings.GetExtensionTemplates(
                PROJECTKIND_VB, projectMock.Instance, "System.Drawing, Version=1.0.0.0");
            Assert.IsNull(extTemplates, "Expect no extension. Got some.");

            extTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);
            if (null == extTemplates)
            {
                Assert.Fail("Expect 1 extension. Got NULL.");
            }
            else
            {
                Assert.IsTrue(1 == extTemplates.Count, string.Format("Expect 1 extension. Got {0}.", extTemplates.Count));
                MyExtensionTemplate extTemplate = extTemplates[0];
                Assert.IsNull(extTemplate.AssemblyFullName, string.Format("Expect NULL assembly name. Got {0}.", extTemplate.AssemblyFullName));
            }
        }

        #endregion

        #region "QA MADDOG test case: MyExtEditCustomDataXml"

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 1 - 1: File contains garbage (random bits).
        /// If the file is a binary file, VSCore's LoadTextFile method (textfile.cpp) will return an error
        /// and the custom data field will be NULL.
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario1_1()
        {
            Mock<Template> templateWithBinaryCustomDataMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXMLScenario1Binary",
                "A template with a binary .customdata file will has CustomData field as Nothing.",
                "C:\\Program Files\\Microsoft Visual Studio 11.0\\Common7\\Ide\\ItemTemplatesCache\\VisualBasic\\1033\\MyExtEditCustomDataXmlScenario1.zip\\MyExtEditCustomDataXmlScenario1.vstemplate",
                "MyExtEditCustomDataXmlScenario1.vb",
                null);
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { templateWithBinaryCustomDataMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNull(extensionTemplates, "Should not get any extension templates!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 1 - 2: File contains garbage (random characters).
        /// Custom data field is a random string.
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario1_2()
        {
            Mock<Template> templateWithGarbageStringCustomDataMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario1GarbageString",
                "Extension template for MyExtEditCustomDataXml test case, Scenario1. CustomData file contains garbage string.",
                "C:\\Program Files\\Microsoft Visual Studio 11.0\\Common7\\IDE\\ItemTemplatesCache\\VisualBasic\\1033\\MyExtEditCustomDataXmlScenario1GarbageString.zip\\MyExtEditCustomDataXmlScenario1GarbageString.vstemplate",
                "MyExtEditCustomDataXmlScenario1GarbageString.vb",
                string.Format("{0}\n{1}", "zmsccvrurpsypnkzhvuozkbwcrbyayrrovuclmcqlatqpgctkkgljspplvalusutybkuthnuseczqkgv",
                "phoqsfhwferpmrwsgasnwiacwlsfpqzbpewnedastoooqxibhjsprszghmugncgpfqaivi"));
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { templateWithGarbageStringCustomDataMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNull(extensionTemplates, "Should not get any extension templates!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 2: Custom data file missing.
        /// Again, in this case, CustomData field is NULL (Nothing).
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario2()
        {
            Mock<Template> templateWithMissingCustomDataMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario2 template",
                "Extension template for MyExtEditCustomDataXml test case, Scenario 2. CustomData file missing.",
                "C:\\Program Files\\Microsoft Visual Studio 11.0\\Common7\\IDE\\ItemTemplatesCache\\VisualBasic\\1033\\MyExtEditCustomDataXmlScenario2.zip\\MyExtEditCustomDataXmlScenario2.vstemplate",
                "MyExtEditCustomDataXmlScenario2.vb",
                null);
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { templateWithMissingCustomDataMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNull(extensionTemplates, "Should not get any extension templates!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 3: Custom data file empty.
        /// Again, in this case, CustomData field is NULL (Nothing).
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario3()
        {
            Mock<Template> myExtEditCustomDataXmlScenario3TemplateMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario3",
                "Extension template for MyExtEditCustomDataXml test case, Scenario 3. CustomData file is empty.",
                "C:\\Program Files\\Microsoft Visual Studio 11.0\\Common7\\IDE\\ItemTemplatesCache\\VisualBasic\\1033\\MyExtEditCustomDataXmlScenario3.zip\\MyExtEditCustomDataXmlScenario3.vstemplate",
                "MyExtEditCustomDataXmlScenario3.vb",
                null);
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { myExtEditCustomDataXmlScenario3TemplateMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNull(extensionTemplates, "Should not get any extension templates!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 3: Custom data file empty.
        /// Although this is not likely to happen on VSCore code, let's try with an empty string.
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario3EmptyString()
        {
            Mock<Template> myExtEditCustomDataXmlScenario3TemplateMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario3",
                "Extension template for MyExtEditCustomDataXml test case, Scenario 3. CustomData file is empty.",
                "C:\\Program Files\\Microsoft Visual Studio 11.0\\Common7\\IDE\\ItemTemplatesCache\\VisualBasic\\1033\\MyExtEditCustomDataXmlScenario3.zip\\MyExtEditCustomDataXmlScenario3.vstemplate",
                "MyExtEditCustomDataXmlScenario3.vb",
                string.Empty);
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { myExtEditCustomDataXmlScenario3TemplateMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNull(extensionTemplates, "Should not get any extension templates!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 3: Custom data file empty.
        /// Although this is not likely to happen on VSCore code, let's try with all-spaces string.
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario3AllSpaces()
        {
            Mock<Template> myExtEditCustomDataXmlScenario3TemplateMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario3",
                "Extension template for MyExtEditCustomDataXml test case, Scenario 3. CustomData file is empty.",
                "C:\\Program Files\\Microsoft Visual Studio 11.0\\Common7\\IDE\\ItemTemplatesCache\\VisualBasic\\1033\\MyExtEditCustomDataXmlScenario3.zip\\MyExtEditCustomDataXmlScenario3.vstemplate",
                "MyExtEditCustomDataXmlScenario3.vb",
                "                             ");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { myExtEditCustomDataXmlScenario3TemplateMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNull(extensionTemplates, "Should not get any extension templates!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 4: ID contains quotation marks.
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario4()
        {
            Mock<Template> myExtEditCustomDataXmlScenario4TemplateMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario4",
                "Extension template for MyExtEditCustomDataXml test case, Scenario 4. ID contains quotation marks.",
                "C:\\Program Files\\Microsoft Visual Studio 11.0\\Common7\\IDE\\ItemTemplatesCache\\VisualBasic\\1033\\MyExtEditCustomDataXmlScenario4.zip\\MyExtEditCustomDataXmlScenario4.vstemplate",
                "MyExtEditCustomDataXmlScenario4.vb",
                "<VBMyExtensionTemplate ID=\"My.MyExtEditCustom\"DataXmlScenario4\" Version=\"1.0.0.0\" AssemblyFullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"/>");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { myExtEditCustomDataXmlScenario4TemplateMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNull(extensionTemplates, "Should not get any extension templates!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 5: ID contains &lt;.
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario5()
        {
            Mock<Template> myExtEditCustomDataXmlScenario5TemplateMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario5",
                "Extension template for MyExtEditCustomDataXml test case, Scenario 5. ID contains <.",
                "C:\\Program Files\\Microsoft Visual Studio 11.0\\Common7\\IDE\\ItemTemplatesCache\\VisualBasic\\1033\\MyExtEditCustomDataXmlScenario5.zip\\MyExtEditCustomDataXmlScenario5.vstemplate",
                "MyExtEditCustomDataXmlScenario5.vb",
                "<VBMyExtensionTemplate ID=\"My.MyExtEditCustom<DataXmlScenario5\" Version=\"1.0.0.0\" AssemblyFullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"/>");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { myExtEditCustomDataXmlScenario5TemplateMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNull(extensionTemplates, "Should not get any extension templates!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 6: ID and Version are missing.
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario6()
        {
            Mock<Template> myExtEditCustomDataXmlScenario6TemplateMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario6",
                "Extension template for MyExtEditCustomDataXml test case, Scenario 6. ID and Version are missing.",
                "C:\\Documents and Settings\\huyn\\Application Data\\Microsoft\\VisualStudio\\11.0\\ItemTemplatesCache\\Visual Basic\\MyExtEditCustomDataXmlScenario6.zip\\MyExtEditCustomDataXmlScenario6.vstemplate",
                "MyExtEditCustomDataXmlScenario6.vb",
                "<VBMyExtensionTemplate AssemblyFullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"/>");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { myExtEditCustomDataXmlScenario6TemplateMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNull(extensionTemplates, "Should not get any extension templates!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 7: Version is missing.
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario7()
        {
            Mock<Template> myExtEditCustomDataXmlScenario7TemplateMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario7",
                "Extension template for MyExtEditCustomDataXml test case, Scenario 7. Version is missing",
                "C:\\Documents and Settings\\huyn\\Application Data\\Microsoft\\VisualStudio\\11.0\\ItemTemplatesCache\\Visual Basic\\MyExtEditCustomDataXmlScenario7.zip\\MyExtEditCustomDataXmlScenario7.vstemplate",
                "MyExtEditCustomDataXmlScenario7.vb",
                "<VBMyExtensionTemplate ID=\"My.MyExtEditCustomDataXmlScenario7\" AssemblyFullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"/>");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { myExtEditCustomDataXmlScenario7TemplateMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNull(extensionTemplates, "Should not get any extension templates!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 8: AssemblyName is missing.
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario8()
        {
            Mock<Template> myExtEditCustomDataXmlScenario8TemplateMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario8",
                "Extension template for MyExtEditCustomDataXml test case, Scenario 8. AssemblyFullName is missing.",
                "C:\\Documents and Settings\\huyn\\Application Data\\Microsoft\\VisualStudio\\11.0\\ItemTemplatesCache\\Visual Basic\\MyExtEditCustomDataXmlScenario8.zip\\MyExtEditCustomDataXmlScenario8.vstemplate",
                "MyExtEditCustomDataXmlScenario8.vb",
                "<VBMyExtensionTemplate ID=\"My.MyExtEditCustomDataXmlScenario8\" Version=\"1.0.0.0\"/>");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { myExtEditCustomDataXmlScenario8TemplateMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNotNull(extensionTemplates, "Should get a list of extension templates!");
            Assert.IsTrue(extensionTemplates.Count == 1, "The list of extension templates should contain 1 template!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 9: AssemblyName refers to inexistent assembly.
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario9()
        {
            Mock<Template> myExtEditCustomDataXmlScenario9TemplateMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario9",
                "Extension template for MyExtEditCustomDataXml test case, Scenario 9. AssemblyFullName refers to inexistent assembly.",
                "C:\\Documents and Settings\\huyn\\Application Data\\Microsoft\\VisualStudio\\11.0\\ItemTemplatesCache\\Visual Basic\\MyExtEditCustomDataXmlScenario9.zip\\MyExtEditCustomDataXmlScenario9.vstemplate",
                "MyExtEditCustomDataXmlScenario9.vb",
                "<VBMyExtensionTemplate ID=\"My.MyExtEditCustomDataXmlScenario9\" Version=\"1.0.0.0\" AssemblyFullName=\"InExistent.Assembly.MyExtEditCustomDataXmlScenario9, Version=9.12.3.16, Culture=neutral\"/>");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { myExtEditCustomDataXmlScenario9TemplateMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNotNull(extensionTemplates, "Should get a list of extension templates!");
            Assert.IsTrue(extensionTemplates.Count == 1, "The list of extension templates should contain 1 template!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 10: Version string is syntactically incorrect.
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario10()
        {
            Mock<Template> myExtEditCustomDataXmlScenario10TemplateMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario10",
                "Extension template for MyExtEditCustomDataXml test case, Scenario 10. Version string is syntactically incorrect.",
                "C:\\Documents and Settings\\huyn\\Application Data\\Microsoft\\VisualStudio\\11.0\\ItemTemplatesCache\\Visual Basic\\MyExtEditCustomDataXmlScenario10.zip\\MyExtEditCustomDataXmlScenario10.vstemplate",
                "MyExtEditCustomDataXmlScenario10.vb",
                "<VBMyExtensionTemplate ID=\"My.MyExtEditCustomDataXmlScenario10\" Version=\"1.0.0.0.100\" AssemblyFullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"/>");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { myExtEditCustomDataXmlScenario10TemplateMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNotNull(extensionTemplates, "Should get a list of extension templates!");
            Assert.IsTrue(extensionTemplates.Count == 1, "The list of extension templates should contain 1 template!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 11: Two AssemblyFullName attributes.
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario11()
        {
            Mock<Template> myExtEditCustomDataXmlScenario11TemplateMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario11",
                "Extension template for MyExtEditCustomDataXml test case, Scenario 11. Two AssemblyFullName attributes.",
                "C:\\Documents and Settings\\huyn\\Application Data\\Microsoft\\VisualStudio\\11.0\\ItemTemplatesCache\\Visual Basic\\MyExtEditCustomDataXmlScenario11.zip\\MyExtEditCustomDataXmlScenario11.vstemplate",
                "MyExtEditCustomDataXmlScenario11.vb",
                "<VBMyExtensionTemplate ID=\"My.MyExtEditCustomDataXmlScenario11\" Version=\"1.0.0.0\" AssemblyFullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" AssemblyFullName=\"Microsoft.VisualBasic, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"/>");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { myExtEditCustomDataXmlScenario11TemplateMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNull(extensionTemplates, "Should not get any extension templates!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 12: Two ID attributes.
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario12()
        {
            Mock<Template> myExtEditCustomDataXmlScenario12TemplateMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario12",
                "Extension template for MyExtEditCustomDataXml test case, Scenario 12. Two ID attributes.",
                "C:\\Documents and Settings\\huyn\\Application Data\\Microsoft\\VisualStudio\\11.0\\ItemTemplatesCache\\Visual Basic\\MyExtEditCustomDataXmlScenario12.zip\\MyExtEditCustomDataXmlScenario12.vstemplate",
                "MyExtEditCustomDataXmlScenario12.vb",
                "<VBMyExtensionTemplate ID=\"My.MyExtEditCustomDataXmlScenario12\" ID=\"My.MyExtEditCustomDataXmlScenario12a\" Version=\"1.0.0.0\" AssemblyFullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"/>");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { myExtEditCustomDataXmlScenario12TemplateMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNull(extensionTemplates, "Should not get any extension templates!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 13: Version does not have quotation marks around its value.
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario13()
        {
            Mock<Template> myExtEditCustomDataXmlScenario13TemplateMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario13",
                "Extension template for MyExtEditCustomDataXml test case, Scenario 13. Version does not have quotation marks around its value.",
                "C:\\Documents and Settings\\huyn\\Application Data\\Microsoft\\VisualStudio\\11.0\\ItemTemplatesCache\\Visual Basic\\MyExtEditCustomDataXmlScenario13.zip\\MyExtEditCustomDataXmlScenario13.vstemplate",
                "MyExtEditCustomDataXmlScenario13.vb",
                "<VBMyExtensionTemplate ID=\"My.MyExtEditCustomDataXmlScenario13\" Version=1.0.0.0 AssemblyFullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"/>");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { myExtEditCustomDataXmlScenario13TemplateMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNull(extensionTemplates, "Should not get any extension templates!");
        }

        /// <summary>
        /// MyExtEditCustomDataXml - Scenario 14: Two / instead of 1 at the end.
        /// </summary>
        [TestMethod()]
        public void MyExtEditCustomDataXmlScenario14()
        {
            Mock<Template> myExtEditCustomDataXmlScenario14TemplateMock = Util.CreateTemplateMock(
                "MyExtEditCustomDataXmlScenario14",
                "Extension template for MyExtEditCustomDataXml test case, Scenario 14. Two forward-slashes at the end.",
                "C:\\Documents and Settings\\huyn\\Application Data\\Microsoft\\VisualStudio\\11.0\\ItemTemplatesCache\\Visual Basic\\MyExtEditCustomDataXmlScenario14.zip\\MyExtEditCustomDataXmlScenario14.vstemplate",
                "MyExtEditCustomDataXmlScenario14.vb",
                "<VBMyExtensionTemplate ID=\"My.MyExtEditCustomDataXmlScenario14\" Version=\"1.0.0.0\" AssemblyFullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"//>");
            Mock<Project> projectMock = CreateProjectMock(PROJECTKIND_VB, CUSTOMDATASIGNATURE_VB,
                new Mock<Template>[] { myExtEditCustomDataXmlScenario14TemplateMock });

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings("NotExistedPath");
            List<MyExtensionTemplate> extensionTemplates = extSettings.GetExtensionTemplates(PROJECTKIND_VB, projectMock.Instance);

            Assert.IsNull(extensionTemplates, "Should not get any extension templates!");
        }

        #endregion

        #region "QA MADDOG test case: MyExtEditMyVBExtSetXml"

        /// <summary>
        /// MyExtEditMyVBExtSetXml scenario 1: File contains garbage (random bits).
        /// Use a resource Zapotec BMP file in this case.
        /// </summary>
        [TestMethod()]
        public void MyExtEditMyVBExtSetXml01()
        { 
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

            MyExtensibilityTestRes.Zapotec.Save(filePath);

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

            AssemblyOption vb9Option = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
            Assert.IsTrue(AssemblyOption.Prompt == vb9Option, "AssemblyOption.Prompt != vb9Option");
        }

        /// <summary>
        /// MyExtEditMyVBExtSetXml scenario 1: File contains garbage (random bits).
        /// Use a random text file in this case.
        /// </summary>
        [TestMethod()]
        public void MyExtEditMyVBExtSetXml01RandomText()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

            if (CreateFile(filePath, SETTINGSFILE_MYEXTEDITMYVBEXTSETXML01))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

                AssemblyOption vb9AddOption = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9AddOption, "AssemblyOption.Prompt != vb9AddOption");
                AssemblyOption vb9RemoveOption = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9RemoveOption, "AssemblyOption.Prompt != vb9RemoveOption");

                File.Delete(filePath);
            }
            else
            {
                Assert.Fail("Could not create settings file!");
            }
        }

        /// <summary>
        /// MyExtEditMyVBExtSetXml scenario 2: File is blank.
        /// </summary>
        [TestMethod()]
        public void MyExtEditMyVBExtSetXml02()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

            try 
            {
                StreamWriter sw = File.CreateText(filePath);
                sw.Close();
            }
            catch { }

            Assert.IsTrue(File.Exists(filePath), "Could not create file!");
            Assert.IsTrue((new FileInfo(filePath)).Length == 0, "File is not empty!");

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

            AssemblyOption vb9Option = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
            Assert.IsTrue(AssemblyOption.Prompt == vb9Option, "AssemblyOption.Prompt != vb9Option");

            File.Delete(filePath);
        }

        /// <summary>
        /// MyExtEditMyVBExtSetXml scenario 3: File is missing.
        /// </summary>
        [TestMethod()]
        public void MyExtEditMyVBExtSetXml03()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

            Assert.IsFalse(File.Exists(filePath), "File exists!");

            MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

            AssemblyOption vb9Option = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
            Assert.IsTrue(AssemblyOption.Prompt == vb9Option, "AssemblyOption.Prompt != vb9Option");
        }

        /// <summary>
        /// MyExtEditMyVBExtSetXml scenario 4: The xmlns attribute is incorrect.
        /// </summary>
        [TestMethod()]
        public void MyExtEditMyVBExtSetXml04()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

            if (CreateFile(filePath, SETTINGSFILE_MYEXTEDITMYVBEXTSETXML04))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

                AssemblyOption vb9AddOption = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.No == vb9AddOption, "AssemblyOption.No != vb9AddOption");
                AssemblyOption vb9RemoveOption = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Yes == vb9RemoveOption, "AssemblyOption.Yes != vb9RemoveOption");

                File.Delete(filePath);
            }
            else
            {
                Assert.Fail("Could not create settings file!");
            }
        }

        /// <summary>
        /// MyExtEditMyVBExtSetXml scenario 5: 
        /// The FullName for one of the assemblies is syntactically correct, 
        /// but refers to an assembly that doesn't exist
        /// </summary>
        [TestMethod()]
        public void MyExtEditMyVBExtSetXml05()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

            if (CreateFile(filePath, SETTINGSFILE_MYEXTEDITMYVBEXTSETXML05))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

                AssemblyOption vb9AddOption = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Yes == vb9AddOption, "AssemblyOption.Yes != vb9AddOption");
                AssemblyOption vb9RemoveOption = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9RemoveOption, "AssemblyOption.Prompt != vb9RemoveOption");

                File.Delete(filePath);
            }
            else
            {
                Assert.Fail("Could not create settings file!");
            }
        }

        /// <summary>
        /// MyExtEditMyVBExtSetXml scenario 6 and 8: 
        /// AutoAdd equals a value other than Yes, No or Prompt
        /// UNDONE: update Maddog
        /// </summary>
        [TestMethod()]
        public void MyExtEditMyVBExtSetXml06()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

            if (CreateFile(filePath, SETTINGSFILE_MYEXTEDITMYVBEXTSETXML06))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

                AssemblyOption vb9AddOption = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9AddOption, "AssemblyOption.Prompt != vb9AddOption");
                AssemblyOption vb9RemoveOption = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9RemoveOption, "AssemblyOption.Prompt != vb9RemoveOption");

                File.Delete(filePath);
            }
            else
            {
                Assert.Fail("Could not create settings file!");
            }
        }

        /// <summary>
        /// MyExtEditMyVBExtSetXml scenario 7: 
        /// AutoAdd is missing for one element
        /// </summary>
        [TestMethod()]
        public void MyExtEditMyVBExtSetXml07()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

            if (CreateFile(filePath, SETTINGSFILE_MYEXTEDITMYVBEXTSETXML07))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

                AssemblyOption vb9AddOption = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9AddOption, "AssemblyOption.Prompt != vb9AddOption");
                AssemblyOption vb9RemoveOption = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Yes == vb9RemoveOption, "AssemblyOption.Yes != vb9RemoveOption");

                File.Delete(filePath);
            }
            else
            {
                Assert.Fail("Could not create settings file!");
            }
        }

        /// <summary>
        /// MyExtEditMyVBExtSetXml scenario 9: 
        /// The FullName attribute for one of the Assembly elements is missing
        /// </summary>
        [TestMethod()]
        public void MyExtEditMyVBExtSetXml09()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

            if (CreateFile(filePath, SETTINGSFILE_MYEXTEDITMYVBEXTSETXML09))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

                AssemblyOption vb9AddOption = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Yes == vb9AddOption, "AssemblyOption.Yes != vb9AddOption");
                AssemblyOption vb9RemoveOption = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9RemoveOption, "AssemblyOption.Prompt != vb9RemoveOption");

                File.Delete(filePath);
            }
            else
            {
                Assert.Fail("Could not create settings file!");
            }
        }

        /// <summary>
        /// MyExtEditMyVBExtSetXml scenario 10: 
        /// The FullName attribute is present, but the AutoAdd and AutoRemove attributes are missing
        /// </summary>
        [TestMethod()]
        public void MyExtEditMyVBExtSetXml10()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

            if (CreateFile(filePath, SETTINGSFILE_MYEXTEDITMYVBEXTSETXML10))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

                AssemblyOption vb9AddOption = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9AddOption, "AssemblyOption.Prompt != vb9AddOption");
                AssemblyOption vb9RemoveOption = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9RemoveOption, "AssemblyOption.Prompt != vb9RemoveOption");

                File.Delete(filePath);
            }
            else
            {
                Assert.Fail("Could not create settings file!");
            }
        }

        /// <summary>
        /// MyExtEditMyVBExtSetXml scenario 11: 
        /// There are two AutoAdd attributes for one element
        /// </summary>
        [TestMethod()]
        public void MyExtEditMyVBExtSetXml11()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

            if (CreateFile(filePath, SETTINGSFILE_MYEXTEDITMYVBEXTSETXML11))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

                AssemblyOption vb9AddOption = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9AddOption, "AssemblyOption.Prompt != vb9AddOption");
                AssemblyOption vb9RemoveOption = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9RemoveOption, "AssemblyOption.Prompt != vb9RemoveOption");
                AssemblyOption speechAutoAdd = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_SYSTEM_SPEECH);
                Assert.IsTrue(AssemblyOption.Prompt == speechAutoAdd, "AssemblyOption.Prompt != speechAutoAdd");
                AssemblyOption speechAutoRemove = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_SYSTEM_SPEECH);
                Assert.IsTrue(AssemblyOption.Prompt == speechAutoRemove, "AssemblyOption.Prompt != speechAutoRemove");

                File.Delete(filePath);
            }
            else
            {
                Assert.Fail("Could not create settings file!");
            }
        }

        /// <summary>
        /// MyExtEditMyVBExtSetXml scenario 12: 
        /// Two Assembly elements refer to the same assembly, and have conflicting AutoAdd and AutoRemove values
        /// </summary>
        [TestMethod()]
        public void MyExtEditMyVBExtSetXml12()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

            if (CreateFile(filePath, SETTINGSFILE_MYEXTEDITMYVBEXTSETXML12))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

                AssemblyOption vb9AddOption = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Yes == vb9AddOption, "AssemblyOption.Yes != vb9AddOption");
                AssemblyOption vb9RemoveOption = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.No == vb9RemoveOption, "AssemblyOption.No != vb9RemoveOption");

                File.Delete(filePath);
            }
            else
            {
                Assert.Fail("Could not create settings file!");
            }
        }

        /// <summary>
        /// MyExtEditMyVBExtSetXml scenario 13: 
        /// For one element, the element name "Assembly" is missing, but the attributes are present.
        /// </summary>
        [TestMethod()]
        public void MyExtEditMyVBExtSetXml13()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

            if (CreateFile(filePath, SETTINGSFILE_MYEXTEDITMYVBEXTSETXML13))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

                AssemblyOption vb9AddOption = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9AddOption, "AssemblyOption.Prompt != vb9AddOption");
                AssemblyOption vb9RemoveOption = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9RemoveOption, "AssemblyOption.Prompt != vb9RemoveOption");
                AssemblyOption speechAutoAdd = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_SYSTEM_SPEECH);
                Assert.IsTrue(AssemblyOption.Prompt == speechAutoAdd, "AssemblyOption.Prompt != speechAutoAdd");
                AssemblyOption speechAutoRemove = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_SYSTEM_SPEECH);
                Assert.IsTrue(AssemblyOption.Prompt == speechAutoRemove, "AssemblyOption.Prompt != speechAutoRemove");

                File.Delete(filePath);
            }
            else
            {
                Assert.Fail("Could not create settings file!");
            }
        }

        /// <summary>
        /// MyExtEditMyVBExtSetXml scenario 14: 
        /// One Assembly element closes with //> instead of /> 
        /// </summary>
        [TestMethod()]
        public void MyExtEditMyVBExtSetXml14()
        {
            string dirPath = System.Environment.CurrentDirectory;
            string filePath = Path.Combine(dirPath, ASM_SETTINGS_FILE_NAME);

            if (CreateFile(filePath, SETTINGSFILE_MYEXTEDITMYVBEXTSETXML14))
            {
                MyExtensibilitySettings extSettings = new MyExtensibilitySettings(dirPath);

                AssemblyOption vb9AddOption = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9AddOption, "AssemblyOption.Prompt != vb9AddOption");
                AssemblyOption vb9RemoveOption = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_VBRUN9);
                Assert.IsTrue(AssemblyOption.Prompt == vb9RemoveOption, "AssemblyOption.Prompt != vb9RemoveOption");
                AssemblyOption speechAutoAdd = extSettings.GetAssemblyAutoAdd(Util.ASMNAME_SYSTEM_SPEECH);
                Assert.IsTrue(AssemblyOption.Prompt == speechAutoAdd, "AssemblyOption.Prompt != speechAutoAdd");
                AssemblyOption speechAutoRemove = extSettings.GetAssemblyAutoRemove(Util.ASMNAME_SYSTEM_SPEECH);
                Assert.IsTrue(AssemblyOption.Prompt == speechAutoRemove, "AssemblyOption.Prompt != speechAutoRemove");                

                File.Delete(filePath);
            }
            else
            {
                Assert.Fail("Could not create settings file!");
            }
        }

        #endregion

        #region "Private static support methods"

        /// <summary>
        /// Create a Project mock. Project.DTE.Solution will be a Solution3 and
        /// GetProjectTemplatesWithCustomData will return the Templates mock containing the 
        /// specified Template mocks.
        /// </summary>
        private static Mock<Project> CreateProjectMock(string projectKind, string customDataSignature, Mock<Template>[] templateMocks)
        {
            // Mock of <Templates>
            Mock<Templates> templatesMock = null; 
            if (null != templateMocks)
            {
                templatesMock = new Mock<Templates>(typeof(IEnumerable));
                templatesMock.Implement("get_Count", templateMocks.Length);
                // Mock of Templates.GetEnumerator
                SequenceMock<IEnumerator> templatesEnumeratorMock = new SequenceMock<IEnumerator>();
                foreach (Mock<Template> templateMock in templateMocks)
                {
                    templatesEnumeratorMock.AddExpectation("MoveNext", (bool)true);
                    templatesEnumeratorMock.AddExpectation("get_Current", (object)templateMock.Instance);
                }
                templatesEnumeratorMock.AddExpectation("MoveNext", (bool)false);
                // Templates.GetEnumerator implementation.
                templatesMock.Implement("GetEnumerator", (IEnumerator)templatesEnumeratorMock.Instance); 
            }

            // Mock of DTE.Solution
            Mock<Solution3> solution3Mock = new Mock<Solution3>(typeof(Solution));
            // Solution.GetProjectItemTemplates implementation
            solution3Mock.Implement("GetProjectItemTemplates",
                new object[] { projectKind, customDataSignature }, 
                ( null==templateMocks ? null : (Templates)templatesMock.Instance));

            return CreateProjectMock(solution3Mock);
        }

        /// <summary>
        /// Create a project mock. GetProjectItemTemplates will throw the given exception.
        /// </summary>
        private static Mock<Project> CreateProjectMockWithException(
            string projectKind, string customDataSignature, Exception ex)
        {
            // Mock of DTE.Solution
            Mock<Solution3> solution3Mock = new Mock<Solution3>(typeof(Solution));
            // Solution.GetProjectItemTemplates implementation, throwing specified exception
            solution3Mock.Implement("GetProjectItemTemplates", new object[] { projectKind, customDataSignature }, ex);

            return CreateProjectMock(solution3Mock);
        }

        /// <summary>
        /// Support method for 2 methods above.
        /// </summary>
        private static Mock<Project> CreateProjectMock(Mock<Solution3> solution3Mock)
        {
            // Mock of Project.DTE.
            Mock<DTE> dteMock = new Mock<DTE>();
            // DTE.Solution implementation
            dteMock.Implement("get_Solution", (Solution)solution3Mock.Instance);

            // Mock of EnvDTE.Project
            Mock<Project> projectMock = new Mock<Project>();
            // Project.DTE implementation.
            projectMock.Implement("get_DTE", (DTE)dteMock.Instance);

            return projectMock;
        }

        /// <summary>
        /// Create a text file with the given content at the given path.
        /// </summary>
        private static bool CreateFile(string filePath, string[] fileContent)
        {
            bool result = false;
            StreamWriter strWriter = null;
            try
            {
                strWriter = File.CreateText(filePath);
                foreach (string line in fileContent)
                {
                    strWriter.WriteLine(line);
                }
                result = true;
            }
            catch 
            {
            }
            finally
            {
                if (null != strWriter)
                {
                    strWriter.Close();
                }
            }

            return result;
        }
        
        /// <summary>
        /// Verify a file content on disk with the expected content.
        /// </summary>
        private static void VerifyFile(string filePath, string[] fileContent)
        {
            StreamReader strReader = File.OpenText(filePath);
            int i = 0;
            while (!strReader.EndOfStream)
            {
                string line = strReader.ReadLine();
                Assert.IsTrue(string.Equals(line, fileContent[i], StringComparison.OrdinalIgnoreCase),
                    string.Format("'{0}' is correct.", line));
                i++;
            }
            strReader.Close();
        }

        #endregion

        private const string ASM_SETTINGS_FILE_NAME = "VBMyExtensionSettings.xml";

        private const string PROJECTKIND_VB = "VisualBasic";
        private const string CUSTOMDATASIGNATURE_VB = "Microsoft.VisualBasic.MyExtension";
        
        private static string[] SETTINGSFILE1 = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0\" AutoAdd=\"Yes\" AutoRemove=\"Prompt\" />",
            "  <Assembly FullName=\"System.Speech, Version=3.0.0.0\" AutoAdd=\"Prompt\" AutoRemove=\"No\" />",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE1_AFTER = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0\" AutoAdd=\"No\" AutoRemove=\"Yes\" />",
            "  <Assembly FullName=\"System.Speech, Version=3.0.0.0\" AutoAdd=\"Yes\" AutoRemove=\"No\" />",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE1_ASMOPTION_CASEINSENSITIVE = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0\" AutoAdd=\"yes\" AutoRemove=\"PROMPT\" />",
            "  <Assembly FullName=\"System.Speech, Version=3.0.0.0\" AutoAdd=\"pRoMpT\" AutoRemove=\"nO\" />",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE1_ASMOPTION_NUMBER = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0\" AutoAdd=\"1\" AutoRemove=\"2\" />",
            "  <Assembly FullName=\"System.Speech, Version=3.0.0.0\" AutoAdd=\"2\" AutoRemove=\"0\" />",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE2 = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" AutoAdd=\"Yes\" AutoRemove=\"Prompt\" />",
            "  <Assembly FullName=\"System.Speech, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\" AutoAdd=\"Prompt\" AutoRemove=\"No\"/>",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE2_CASEINSENSITIVE = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"MICROSOFT.visualbasic.fx3.5EXTENSIONS, VERSION=8.0.0.0, culture=neutral, PUBLICKEYTOKEN=b03f5f7f11d50a3a\" AutoAdd=\"Yes\" AutoRemove=\"Prompt\" />",
            "  <Assembly FullName=\"SyStEm.SpEeCh, VeRsIoN=3.0.0.0, CuLtUrE=neutral, PuBlIcKeYtOkEn=31bf3856ad364e35\" AutoAdd=\"Prompt\" AutoRemove=\"No\"/>",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE2_CASEINSENSITIVE_AFTER = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"MICROSOFT.visualbasic.fx3.5EXTENSIONS, Version=8.0.0.0\" AutoAdd=\"No\" AutoRemove=\"Yes\" />",
            "  <Assembly FullName=\"SyStEm.SpEeCh, Version=3.0.0.0\" AutoAdd=\"Yes\" AutoRemove=\"No\" />",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE_MYEXTEDITMYVBEXTSETXML01 = {
            "lajsdfasodfiasldjfa;sldgj f;laskjdfasiuropiweqfkjsad'lf kaslfka;oierpo[eiwr;asklf ';lkasgfjghioreghpiwer;alsdf",
            "askjdflasjfdqowiur lsdkn,mnboeitrq osaiovaspof;asjp[witie asdfa;sklbjhlkwquroiwqutyti[psgadbmjf p[iwqrp[",
            "lkjm,dspiw[tr[] asgfpo ][paskgf;amsdf asodirw[q]ei r][psoaf'lp bmpoiwer][pqiw[]rpoqw[eroq\\er]pqwer;"
        };

        private static string[] SETTINGSFILE_MYEXTEDITMYVBEXTSETXML04 = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensionsAndSomeGarbagelasjdfaoiewurlkajsdfagj\">",
            "  <Assembly FullName=\"MICROSOFT.visualbasic.fx3.5EXTENSIONS, Version=8.0.0.0\" AutoAdd=\"No\" AutoRemove=\"Yes\" />",
            "  <Assembly FullName=\"SyStEm.SpEeCh, Version=3.0.0.0\" AutoAdd=\"Yes\" AutoRemove=\"No\" />",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE_MYEXTEDITMYVBEXTSETXML05 = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0\" AutoAdd=\"Yes\" AutoRemove=\"Prompt\" />",
            "  <Assembly FullName=\"YOU.will.NOT.find.ME, Version=3.0.0.0\" AutoAdd=\"Prompt\" AutoRemove=\"No\" />",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE_MYEXTEDITMYVBEXTSETXML06 = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0\" AutoAdd=\"Foo\" AutoRemove=\"Bar\" />",
            "  <Assembly FullName=\"YOU.will.NOT.find.ME, Version=3.0.0.0\" AutoAdd=\"Prompt\" AutoRemove=\"No\" />",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE_MYEXTEDITMYVBEXTSETXML07 = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0\" AutoRemove=\"Yes\" />",
            "  <Assembly FullName=\"YOU.will.NOT.find.ME, Version=3.0.0.0\" AutoAdd=\"Prompt\" AutoRemove=\"No\" />",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE_MYEXTEDITMYVBEXTSETXML09 = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0\" AutoAdd=\"Yes\" AutoRemove=\"Prompt\" />",
            "  <Assembly AutoAdd=\"Prompt\" AutoRemove=\"No\" />",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE_MYEXTEDITMYVBEXTSETXML10 = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0\"/>",
            "  <Assembly FullName=\"System.Speech, Version=3.0.0.0\" AutoAdd=\"Prompt\" AutoRemove=\"No\" />",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE_MYEXTEDITMYVBEXTSETXML11 = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0\" AutoAdd=\"Yes\" AutoAdd=\"No\" AutoRemove=\"Prompt\" />",
            "  <Assembly FullName=\"System.Speech, Version=3.0.0.0\" AutoAdd=\"Prompt\" AutoRemove=\"No\" />",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE_MYEXTEDITMYVBEXTSETXML12 = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0\" AutoAdd=\"Yes\" AutoRemove=\"No\" />",
            "  <Assembly FullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0\" AutoAdd=\"No\" AutoRemove=\"Yes\" />",
            "  <Assembly FullName=\"System.Speech, Version=3.0.0.0\" AutoAdd=\"Prompt\" AutoRemove=\"No\" />",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE_MYEXTEDITMYVBEXTSETXML13 = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0\" AutoAdd=\"Yes\" AutoRemove=\"Prompt\" />",
            "  <FullName=\"System.Speech, Version=3.0.0.0\" AutoAdd=\"Prompt\" AutoRemove=\"No\" />",
            "</VBMyExtensions>"
        };

        private static string[] SETTINGSFILE_MYEXTEDITMYVBEXTSETXML14 = {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<VBMyExtensions xmlns=\"urn:schemas-microsoft-com:xml-msvbmyextensions\">",
            "  <Assembly FullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0\" AutoAdd=\"Yes\" AutoRemove=\"Prompt\" //>",
            "  <Assembly FullName=\"System.Speech, Version=3.0.0.0\" AutoAdd=\"Prompt\" AutoRemove=\"No\" />",
            "</VBMyExtensions>"
        };
    }
}

