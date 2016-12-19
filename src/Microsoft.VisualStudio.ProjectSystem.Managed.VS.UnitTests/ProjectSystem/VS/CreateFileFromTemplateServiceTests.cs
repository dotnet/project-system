// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemTrait]
    public class CreateFileFromTemplateServiceTests
    {
        [Fact]
        public void Constructor_NullAsProjectVsServices_ThrowsArgumentNull()
        {
            var dteServices = IDteServicesFactory.Create();
            var properties = ProjectPropertiesFactory.CreateEmpty();

            Assert.Throws<ArgumentNullException>("projectVsServices", () => {
                new CreateFileFromTemplateService((IUnconfiguredProjectVsServices)null, dteServices, properties);
            });
        }

        [Fact]
        public void Constructor_NullAsDteServices_ThrowsArgumentNull()
        {
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Create();
            var properties = ProjectPropertiesFactory.CreateEmpty();

            Assert.Throws<ArgumentNullException>("dteServices", () => {
                new CreateFileFromTemplateService(projectVsServices, (IDteServices)null, properties);
            });
        }

        [Fact]
        public void Constructor_NullAsProjectProperties_ThrowsArgumentNull()
        {
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Create();
            var dteServices = IDteServicesFactory.Create();

            Assert.Throws<ArgumentNullException>("properties", () => {
                new CreateFileFromTemplateService(projectVsServices, dteServices, (ProjectProperties)null);
            });
        }

        [Fact]
        public async Task CreateFile_NullTemplateFile_ThrowsArgumentNull()
        {
            var service = CreateInstance();

            await Assert.ThrowsAsync<ArgumentNullException>("templateFile", () =>
            {
                return service.CreateFileAsync(null, "ParentNode", "FileName");
            });
        }

        [Fact]
        public async Task CreateFile_NullAsParentNode_ThrowsArgumentNull()
        {
            var service = CreateInstance();

            await Assert.ThrowsAsync<ArgumentNullException>("parentDocumentMoniker", () =>
            {
                return service.CreateFileAsync("SomeFile", null, "FileName");
            });
        }

        [Fact]
        public async Task CreateFile_EmptyAsParentNode_ThrowsArgument()
        {
            var service = CreateInstance();

            await Assert.ThrowsAsync<ArgumentException>("parentDocumentMoniker", () =>
            {
                return service.CreateFileAsync("SomeFile", string.Empty, "FileName");
            });
        }

        [Fact]
        public async Task CreateFile_NullFileName_ThrowsArgumentNull()
        {
            var service = CreateInstance();

            await Assert.ThrowsAsync<ArgumentNullException>("fileName", async () =>
            {
                await service.CreateFileAsync("SomeFile", "ParentNode", null);
            });
        }


        [Theory]
        [InlineData(@"Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\""", @"C:\Path\To\TemplateFile", true)]
        [InlineData(@"Properties, FilePath: ""C:\Foo\""", @"C:\Path\To\TemplateFile", true)]
        [InlineData(@"Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\""", @"C:\Path\To\TemplateFile", false)]
        [InlineData(@"Root (flags: {ProjectRoot}), FilePath: ""C:\Foo\""", null, false)]
        public async Task CreateFile(string input, string templateFilePath, bool expectedResult)
        {
            string templateName = "SettingsInternal.zip";
            string fileName = "Settings.settings";

            var hierarchy = IVsHierarchyFactory.Create();
            var solution = SolutionFactory.CreateWithGetProjectItemTemplate((templateFile, language) => 
            {
                Assert.Equal(templateName, templateFile);
                return templateFilePath;
            });

            
            var vsProject = (IVsProject4)IVsHierarchyFactory.Create();
            vsProject.ImplementAddItemWithSpecific((itemId, itemOperation, itemName, cOpen, files, result) =>
            {
                Assert.Equal(VSADDITEMOPERATION.VSADDITEMOP_RUNWIZARD, itemOperation);
                Assert.Equal(fileName, itemName);
                Assert.Equal((uint)1, cOpen);
                Assert.Equal(new string[] { templateFilePath }, files);

                result[0] = expectedResult ? VSADDRESULT.ADDRESULT_Success : VSADDRESULT.ADDRESULT_Failure;

                return VSConstants.S_OK;
            });

            var dteServices = IDteServicesFactory.ImplementSolution(() => solution);
            
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy, () => vsProject);
            var properties = CreateProperties();
            var service = new CreateFileFromTemplateService(projectVsServices, dteServices, properties);

            bool returnValue = await service.CreateFileAsync(templateName, "Moniker", fileName);
            Assert.Equal(returnValue, expectedResult);
        }

        private CreateFileFromTemplateService CreateInstance()
        {
            return CreateInstance(null, null, null);
        }

        private CreateFileFromTemplateService CreateInstance(IUnconfiguredProjectVsServices projectVsServices, IDteServices dteServices, ProjectProperties properties)
        {
            projectVsServices = projectVsServices ?? IUnconfiguredProjectVsServicesFactory.Create();
            dteServices = dteServices ?? IDteServicesFactory.Create();
            properties = properties ?? ProjectPropertiesFactory.CreateEmpty();

            return new CreateFileFromTemplateService(projectVsServices, dteServices, properties);
        }

        private ProjectProperties CreateProperties()
        {
            var properties = ProjectPropertiesFactory.Create(IUnconfiguredProjectFactory.Create(),
                        new PropertyPageData()
                        {
                            Category = ConfigurationGeneral.SchemaName,
                            PropertyName = ConfigurationGeneral.TemplateLanguageProperty,
                            Value = "CSharp"
                        });

            return properties;
        }
    }
}
