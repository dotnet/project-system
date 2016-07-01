// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Mocks;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemTrait]
    public class CreateFileFromTemplateServiceTests
    {
        [Fact]
        public void Constructor_NullAsProjectVsSevices_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("projectVsServices", () => {
                new CreateFileFromTemplateService(null);
            });
        }

        [Fact]
        public async Task CreateFile_NullTemplateFile_ThrowsArgumentNull()
        {
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Create();
            var service = new CreateFileFromTemplateService(projectVsServices);

            await Assert.ThrowsAsync<ArgumentNullException>("templateFile", async () =>
            {
                await service.CreateFileAsync(null, null, null);
            });
        }

        [Fact]
        public async Task CreateFile_NullParentNode_ThrowsArgumentNull()
        {
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Create();
            var service = new CreateFileFromTemplateService(projectVsServices);

            await Assert.ThrowsAsync<ArgumentNullException>("parentNode", async () =>
            {
                await service.CreateFileAsync("SomeFile", null, null);
            });
        }

        [Fact]
        public async Task CreateFile_NullFileName_ThrowsArgumentNull()
        {
            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Create();
            var service = new CreateFileFromTemplateService(projectVsServices);

            await Assert.ThrowsAsync<ArgumentNullException>("fileName", async () =>
            {
                await service.CreateFileAsync("SomeFile", ProjectTreeParser.Parse("Properties"), null);
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
            var inputTree = ProjectTreeParser.Parse(input);

            var hierarchy = IVsHierarchyFactory.Create();
            var solution = SolutionFactory.CreateWithGetProjectItemTemplate((templateFile, language) => 
            {
                Assert.Equal(templateName, templateFile);
                return templateFilePath;
            });

            var project = ProjectFactory.CreateWithSolution(solution);
            ProjectFactory.ImplementCodeModelLanguage(project, CodeModelLanguageConstants.vsCMLanguageCSharp);

            hierarchy.ImplementGetProperty(Shell.VsHierarchyPropID.ExtObject, project);

            var vsProject = (IVsProject4)hierarchy;
            vsProject.ImplementAddItemWithSpecific((itemId, itemOperation, itemName, files, result) =>
            {
                Assert.Equal((uint)inputTree.GetHierarchyId(), itemId);
                Assert.Equal(VSADDITEMOPERATION.VSADDITEMOP_RUNWIZARD, itemOperation);
                Assert.Equal(fileName, itemName);
                Assert.Equal(new string[] { templateFilePath }, files);

                result[0] = expectedResult ? VSADDRESULT.ADDRESULT_Success : VSADDRESULT.ADDRESULT_Failure;

                return VSConstants.S_OK;
            });

            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy, () => vsProject);
            var service = new CreateFileFromTemplateService(projectVsServices);

            bool returnValue = await service.CreateFileAsync(templateName, inputTree, fileName);
            Assert.Equal(returnValue, expectedResult);
        }
    }
}
