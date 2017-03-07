//-----------------------------------------------------------------------
// <copyright file="MyExtensibilityTestUtil.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
//     Information Contained Herein is Proprietary and Confidential.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.MockObjects;
using Microsoft.VisualStudio.Editors.MyExtensibility;
using Microsoft.VisualStudio.Editors.MyExtensibility.EnvDTE90Interop;

namespace Microsoft.VisualStudio.Editors.UnitTests.MyExtensibility
{
    /// <summary>
    /// Utility class for MyExtensibility unit testing.
    /// </summary>
    class MyExtensibilityTestUtil
    {
        public static Mock<Template> CreateTemplateMock(string name, string description,
            string filePath, string baseName, string customData)
        {
            Mock<Template> templateMock = new Mock<Template>();
            templateMock.Implement("get_Name", (string)name);
            templateMock.Implement("get_Description", (string)description);
            templateMock.Implement("get_FilePath", (string)filePath);
            templateMock.Implement("get_BaseName", (string)baseName);
            templateMock.Implement("get_CustomData", (string)customData);
            return templateMock;
        }

        public static Mock<Template> CreateTemplateMock(string name, string id, string version, string assembly)
        {
            string description = string.Format("{0} Description", name);
            string filePath = string.Format("C:\\Program Files\\Microsoft Visual Studio 9\\Common7\\IDE\\ItemTemplatesCache\\VisualBasic\\1033\\My{0}.zip", name);
            string baseName = string.Format("{0}BaseName", name);
            string customData = string.Format("<VBMyExtensionTemplate \n        ID=\"{0}\" \n        Version=\"{1}\"\n        AssemblyFullName=\"{2}\"\n    />", id, version, assembly);
            return CreateTemplateMock(name, description, filePath, baseName, customData);
        }

        /// <summary>
        /// Return a Template mock with valid data for Microsoft.VisualBasic.FX3.5Extensions.
        /// </summary>
        public static Mock<Template> CreateTemplateMockSample1()
        {
            return CreateTemplateMock(TEMPLATE_NAME_1, TEMPLATE_DESCRIPTION_1, TEMPLATE_FILEPATH_1, TEMPLATE_BASENAME_1, TEMPLATE_CUSTOMDATA_1);
        }

        /// <summary>
        /// Return a Template mock with valid data for Microsoft.VisualBasic.FX3.5Extensions.
        /// Custom data has random casing.
        /// </summary>
        public static Mock<Template> CreateTemplateMockSample1CaseInsensitive()
        {
            return CreateTemplateMock(TEMPLATE_NAME_1, TEMPLATE_DESCRIPTION_1, TEMPLATE_FILEPATH_1, TEMPLATE_BASENAME_1, TEMPLATE_CUSTOMDATA_1_CASE_INSENSITIVE);
        }

        /// <summary>
        /// Return a Template mock with invalid data.
        /// </summary>
        public static Mock<Template> CreateTemplateMockSample2()
        {
            return CreateTemplateMock(TEMPLATE_NAME_2, TEMPLATE_DESCRIPTION_2, TEMPLATE_FILEPATH_2, TEMPLATE_BASENAME_2, TEMPLATE_CUSTOMDATA_2);
        }

        /// <summary>
        /// Return a Template mock with valid data for System.Speech.
        /// </summary>
        public static Mock<Template> CreateTemplateMockSample3()
        {
            return CreateTemplateMock(TEMPLATE_NAME_3, TEMPLATE_DESCRIPTION_3, TEMPLATE_FILEPATH_3, TEMPLATE_BASENAME_3, TEMPLATE_CUSTOMDATA_3);
        }

        /// <summary>
        /// Return a Template mock with valid data for System.Speech.
        /// Custom data has random casing.
        /// </summary>
        public static Mock<Template> CreateTemplateMockSample3CaseInsensitive()
        {
            return CreateTemplateMock(TEMPLATE_NAME_3, TEMPLATE_DESCRIPTION_3, TEMPLATE_FILEPATH_3, TEMPLATE_BASENAME_3, TEMPLATE_CUSTOMDATA_3_CASE_INSENSITIVE);
        }

        /// <summary>
        /// Return a Temlate mock in which the triggering assembly does not have a version
        /// </summary>
        public static Mock<Template> CreateTemplateMockSample4()
        {
            return CreateTemplateMock(TEMPLATE_NAME_4, TEMPLATE_DESCRIPTION_4, TEMPLATE_FILEPATH_4, TEMPLATE_BASENAME_4, TEMPLATE_CUSTOMDATA_4);
        }

        /// <summary>
        /// Return a Template mock for the same triggering assembly above but version 1
        /// </summary>
        public static Mock<Template> CreateTemplateMockSample5()
        {
            return CreateTemplateMock(TEMPLATE_NAME_5, TEMPLATE_DESCRIPTION_5, TEMPLATE_FILEPATH_5, TEMPLATE_BASENAME_5, TEMPLATE_CUSTOMDATA_5);
        }

        /// <summary>
        /// Return a Template mock for the same triggering assembly above but version 2
        /// </summary>
        public static Mock<Template> CreateTemplateMockSample6()
        {
            return CreateTemplateMock(TEMPLATE_NAME_6, TEMPLATE_DESCRIPTION_6, TEMPLATE_FILEPATH_6, TEMPLATE_BASENAME_6, TEMPLATE_CUSTOMDATA_6);
        }

        /// <summary>
        /// Return another Template mock for the same triggering assembly above but version 1
        /// </summary>
        public static Mock<Template> CreateTemplateMockSample7()
        {
            return CreateTemplateMock(TEMPLATE_NAME_7, TEMPLATE_DESCRIPTION_7, TEMPLATE_FILEPATH_7, TEMPLATE_BASENAME_7, TEMPLATE_CUSTOMDATA_7);
        }

        /// <summary>
        /// Return another Template mock for the same triggering assembly above but version 2
        /// </summary>
        public static Mock<Template> CreateTemplateMockSample8()
        {
            return CreateTemplateMock(TEMPLATE_NAME_8, TEMPLATE_DESCRIPTION_8, TEMPLATE_FILEPATH_8, TEMPLATE_BASENAME_8, TEMPLATE_CUSTOMDATA_8);
        }

        public static void VerifyExtensionTemplate(MyExtensionTemplate extensionTemplate, 
            string assemblyFullName, string baseName, 
            string description, string displayName, 
            string filePath, string id, Version version)
        {
            Assert.AreEqual(assemblyFullName, extensionTemplate.AssemblyFullName);
            Assert.AreEqual(baseName, extensionTemplate.BaseName);
            Assert.AreEqual(description, extensionTemplate.Description);
            Assert.AreEqual(displayName, extensionTemplate.DisplayName);
            Assert.AreEqual(filePath, extensionTemplate.FilePath);
            Assert.AreEqual(id, extensionTemplate.ID);
            Assert.AreEqual(version, extensionTemplate.Version);
        }

        public static void VerifyExtensionTemplateSample1(MyExtensionTemplate extensionTemplate)
        {
            VerifyExtensionTemplate(extensionTemplate,
                ASMNAME_VBRUN9_NORMALIZED, 
                TEMPLATE_BASENAME_1, TEMPLATE_DESCRIPTION_1, 
                TEMPLATE_NAME_1, TEMPLATE_FILEPATH_1, 
                EXTENSION_ID_1, VERSION_1_0_0_0);
        }

        public static void VerifyExtensionTemplateSample1CaseInsensitive(MyExtensionTemplate extensionTemplate)
        {
            VerifyExtensionTemplate(extensionTemplate,
                ASMNAME_VBRUN9_CASE_INSENSITIVE_NORMALIZED,
                TEMPLATE_BASENAME_1, TEMPLATE_DESCRIPTION_1,
                TEMPLATE_NAME_1, TEMPLATE_FILEPATH_1,
                EXTENSION_ID_1, VERSION_1_0_0_0);
        }

        public static void VerifyExtensionTemplateSample3(MyExtensionTemplate extensionTemplate)
        {
            VerifyExtensionTemplate(extensionTemplate,
                ASMNAME_SYSTEM_SPEECH_NORMALIZED,
                TEMPLATE_BASENAME_3, TEMPLATE_DESCRIPTION_3,
                TEMPLATE_NAME_3, TEMPLATE_FILEPATH_3,
                EXTENSION_ID_3, VERSION_1_0_0_0);
        }

        public static void VerifyExtensionTemplateSample3CaseInsensitive(MyExtensionTemplate extensionTemplate)
        {
            VerifyExtensionTemplate(extensionTemplate,
                ASMNAME_SYSTEM_SPEECH_CASE_INSENSITIVE_NORMALIZED,
                TEMPLATE_BASENAME_3, TEMPLATE_DESCRIPTION_3,
                TEMPLATE_NAME_3, TEMPLATE_FILEPATH_3,
                EXTENSION_ID_3, VERSION_1_0_0_0);
        }

        public const string TEMPLATE_NAME_1 = "My.Media extension";
        public const string TEMPLATE_DESCRIPTION_1 = "Extending Visual Basic My namespace to include My.Media";
        public const string TEMPLATE_FILEPATH_1 = "C:\\Program Files\\Microsoft Visual Studio 9\\Common7\\IDE\\ItemTemplatesCache\\VisualBasic\\1033\\MyMedia.zip";
        public const string TEMPLATE_BASENAME_1 = "MyMedia.vb";
        public const string TEMPLATE_CUSTOMDATA_1 = "<CustomData>\n    <VBMyExtensionTemplate \n        ID=\"Microsoft.VisualBasic.Media.MyMediaPlayer.MyExtension\" \n        Version=\"1.0.0.0\"\n        AssemblyFullName=\"Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\"\n    />\n</CustomData>";
        public const string TEMPLATE_CUSTOMDATA_1_CASE_INSENSITIVE = "<CustomData>\n    <VBMyExtensionTemplate \n        ID=\"Microsoft.VisualBasic.Media.MyMediaPlayer.MyExtension\" \n        Version=\"1.0.0.0\"\n        AssemblyFullName=\"MICROSOFT.visualbasic.FX3.5extensions, VERSION=8.0.0.0, culture=neutral, PUBLICKEYTOKEN=b03f5f7f11d50a3a\"\n    />\n</CustomData>";

        public const string TEMPLATE_NAME_2 = "Some name";
        public const string TEMPLATE_DESCRIPTION_2 = "Some description";
        public const string TEMPLATE_FILEPATH_2 = "Some file path";
        public const string TEMPLATE_BASENAME_2 = "Some base name";
        public const string TEMPLATE_CUSTOMDATA_2 = "Some custom data";

        public const string TEMPLATE_NAME_3 = "My.Speech extension";
        public const string TEMPLATE_DESCRIPTION_3 = "Extending Visual Basic My namespace for System.Speech.DLL (My.Speech)";
        public const string TEMPLATE_FILEPATH_3 = "c:\\Program Files\\Microsoft Visual Studio 8\\Common7\\IDE\\ItemTemplatesCache\\VisualBasic\\1033\\SystemSpeechExtension.zip\\MySpeech.vstemplate";
        public const string TEMPLATE_BASENAME_3 = "SystemSpeechExtension.vb";
        public const string TEMPLATE_CUSTOMDATA_3 = "<VBMyExtensionTemplate \n    ID=\"Microsoft.VisualBasic.Speech.MyExtension\" \n    Version=\"1.0.0.0\"\n    AssemblyFullName=\"System.Speech, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\"\n/>";
        public const string TEMPLATE_CUSTOMDATA_3_CASE_INSENSITIVE = "<VBMyExtensionTemplate \n    ID=\"Microsoft.VisualBasic.Speech.MyExtension\" \n    Version=\"1.0.0.0\"\n    AssemblyFullName=\"SyStEm.SpEeCh, VeRsIoN=3.0.0.0, CuLtUrE=neutral, PuBlIcKeYtOkEn=31bf3856ad364e35\"\n/>";

        public const string TEMPLATE_NAME_4 = "My.AssemblyWithoutVersion extension";
        public const string TEMPLATE_DESCRIPTION_4 = "Extending Visual Basic My namespace to include My.AssemblyWithoutVersion";
        public const string TEMPLATE_FILEPATH_4 = "C:\\Program Files\\Microsoft Visual Studio 9\\Common7\\IDE\\ItemTemplatesCache\\VisualBasic\\1033\\MyAssemblyWithoutVersion.zip";
        public const string TEMPLATE_BASENAME_4 = "MyAssemblyWithoutVersion.vb";
        public const string TEMPLATE_CUSTOMDATA_4 = "<CustomData>\n    <VBMyExtensionTemplate \n        ID=\"Company.Product.AssemblyWithoutVersion.MyExtension\" \n        Version=\"1.0.0.0\"\n        AssemblyFullName=\"Company.Product.AssemblyWithoutVersion\"\n    />\n</CustomData>";

        public const string TEMPLATE_NAME_5 = "My.AssemblyWithoutVersion extension 11";
        public const string TEMPLATE_DESCRIPTION_5 = "Extending Visual Basic My namespace to include My.AssemblyWithoutVersion 11";
        public const string TEMPLATE_FILEPATH_5 = "C:\\Program Files\\Microsoft Visual Studio 9\\Common7\\IDE\\ItemTemplatesCache\\VisualBasic\\1033\\MyAssemblyWithoutVersion11.zip";
        public const string TEMPLATE_BASENAME_5 = "MyAssemblyWithoutVersion11.vb";
        public const string TEMPLATE_CUSTOMDATA_5 = "<CustomData>\n    <VBMyExtensionTemplate \n        ID=\"Company.Product.AssemblyWithoutVersion.MyExtension11\" \n        Version=\"1.0.0.0\"\n        AssemblyFullName=\"Company.Product.AssemblyWithoutVersion, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\"\n    />\n</CustomData>";

        public const string TEMPLATE_NAME_7 = "My.AssemblyWithoutVersion extension 12";
        public const string TEMPLATE_DESCRIPTION_7 = "Extending Visual Basic My namespace to include My.AssemblyWithoutVersion 12";
        public const string TEMPLATE_FILEPATH_7 = "C:\\Program Files\\Microsoft Visual Studio 9\\Common7\\IDE\\ItemTemplatesCache\\VisualBasic\\1033\\MyAssemblyWithoutVersion12.zip";
        public const string TEMPLATE_BASENAME_7 = "MyAssemblyWithoutVersion12.vb";
        public const string TEMPLATE_CUSTOMDATA_7 = "<VBMyExtensionTemplate \n        ID=\"Company.Product.AssemblyWithoutVersion.MyExtension12\" \n        Version=\"1.0.0.0\"\n        AssemblyFullName=\"Company.Product.AssemblyWithoutVersion, Version=1.0.0.0\"\n/>";

        public const string TEMPLATE_NAME_6 = "My.AssemblyWithoutVersion extension 21";
        public const string TEMPLATE_DESCRIPTION_6 = "Extending Visual Basic My namespace to include My.AssemblyWithoutVersion 21";
        public const string TEMPLATE_FILEPATH_6 = "C:\\Program Files\\Microsoft Visual Studio 9\\Common7\\IDE\\ItemTemplatesCache\\VisualBasic\\1033\\MyAssemblyWithoutVersion21.zip";
        public const string TEMPLATE_BASENAME_6 = "MyAssemblyWithoutVersion21.vb";
        public const string TEMPLATE_CUSTOMDATA_6 = "<VBMyExtensionTemplate \n        ID=\"Company.Product.AssemblyWithoutVersion.MyExtension21\" \n        Version=\"1.0.0.0\"\n        AssemblyFullName=\"Company.Product.AssemblyWithoutVersion, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\"\n/>";

        public const string TEMPLATE_NAME_8 = "My.AssemblyWithoutVersion extension 22";
        public const string TEMPLATE_DESCRIPTION_8 = "Extending Visual Basic My namespace to include My.AssemblyWithoutVersion 22";
        public const string TEMPLATE_FILEPATH_8 = "C:\\Program Files\\Microsoft Visual Studio 9\\Common7\\IDE\\ItemTemplatesCache\\VisualBasic\\1033\\MyAssemblyWithoutVersion22.zip";
        public const string TEMPLATE_BASENAME_8 = "MyAssemblyWithoutVersion22.vb";
        public const string TEMPLATE_CUSTOMDATA_8 = "<CustomData>\n    <VBMyExtensionTemplate \n        ID=\"Company.Product.AssemblyWithoutVersion.MyExtension22\" \n        Version=\"2.0.0.0\"\n        AssemblyFullName=\"Company.Product.AssemblyWithoutVersion, Version=2.0.0.0\"\n    />\n</CustomData>";

        public const string ASMNAME_VBRUN9 = "Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
        public const string ASMNAME_VBRUN9_NORMALIZED = "Microsoft.VisualBasic.FX3.5Extensions, Version=8.0.0.0";
        public const string ASMNAME_VBRUN9_CASE_INSENSITIVE = "MICROSOFT.visualbasic.FX3.5extensions, VERSION=8.0.0.0, culture=neutral, PUBLICKEYTOKEN=b03f5f7f11d50a3a";
        public const string ASMNAME_VBRUN9_CASE_INSENSITIVE_NORMALIZED = "MICROSOFT.visualbasic.FX3.5extensions, Version=8.0.0.0";
        public const string ASMNAME_SYSTEM_SPEECH = "System.Speech, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
        public const string ASMNAME_SYSTEM_SPEECH_NORMALIZED = "System.Speech, Version=3.0.0.0";
        public const string ASMNAME_SYSTEM_SPEECH_CASE_INSENSITIVE = "SyStEm.SpEeCh, VeRsIoN=3.0.0.0, CuLtUrE=neutral, PuBlIcKeYtOkEn=31bf3856ad364e35";
        public const string ASMNAME_SYSTEM_SPEECH_CASE_INSENSITIVE_NORMALIZED = "SyStEm.SpEeCh, Version=3.0.0.0";
        public const string ASMNAME_ASMWTVER_1 = "Company.Product.AssemblyWithoutVersion, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
        public const string ASMNAME_ASMWTVER_2 = "Company.Product.AssemblyWithoutVersion, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";

        public const string EXTENSION_ID_1 = "Microsoft.VisualBasic.Media.MyMediaPlayer.MyExtension";
        public const string EXTENSION_ID_3 = "Microsoft.VisualBasic.Speech.MyExtension";

        public static Version VERSION_1_0_0_0 = new Version(1, 0, 0, 0);
    }
}
