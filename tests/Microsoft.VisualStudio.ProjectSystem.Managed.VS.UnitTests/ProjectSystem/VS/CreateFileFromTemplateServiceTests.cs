// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public class CreateFileFromTemplateServiceTests
    {
        [Fact]
        public async Task CreateFile_NullTemplateFile_ThrowsArgumentNull()
        {
            var service = CreateInstance();

            await Assert.ThrowsAsync<ArgumentNullException>("templateFile", () =>
            {
                return service.CreateFileAsync(null!, "Path");
            });
        }

        [Fact]
        public async Task CreateFile_NullAsPath_ThrowsArgumentNull()
        {
            var service = CreateInstance();

            await Assert.ThrowsAsync<ArgumentNullException>("path", () =>
            {
                return service.CreateFileAsync("SomeFile", null!);
            });
        }

        [Fact]
        public async Task CreateFile_EmptyAsPath_ThrowsArgument()
        {
            var service = CreateInstance();

            await Assert.ThrowsAsync<ArgumentException>("path", () =>
            {
                return service.CreateFileAsync("SomeFile", string.Empty);
            });
        }

        [Theory]
        [InlineData(@"C:\Path\To\TemplateFile", true)]
        [InlineData(@"C:\Path\To\TemplateFile", false)]
        [InlineData(null, false)]
        public async Task CreateFile(string templateFilePath, bool expectedResult)
        {
            string templateName = "SettingsInternal.zip";
            string fileName = "Settings.settings";

            var hierarchy = IVsHierarchyFactory.Create();
            var solution = (Solution)SolutionFactory.CreateWithGetProjectItemTemplate((templateFile, language) =>
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

            var dte = DTEFactory.ImplementSolution(() => solution);

            var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(() => hierarchy, () => vsProject);
            var properties = CreateProperties();
            var service = CreateInstance(projectVsServices, dte, properties);

            bool result = await service.CreateFileAsync(templateName, Path.Combine("Moniker", fileName));
            Assert.Equal(expectedResult, result);
        }

        private static CreateFileFromTemplateService CreateInstance()
        {
            return CreateInstance(null, null, null);
        }

        private static CreateFileFromTemplateService CreateInstance(IUnconfiguredProjectVsServices? projectVsServices, DTE2? dte, ProjectProperties? properties)
        {
            projectVsServices ??= IUnconfiguredProjectVsServicesFactory.Create();
            dte ??= DTEFactory.Create();
            properties ??= ProjectPropertiesFactory.CreateEmpty();

            return new CreateFileFromTemplateService(projectVsServices, IVsUIServiceFactory.Create<SDTE, DTE2>(dte), properties);
        }

        private static ProjectProperties CreateProperties()
        {
            var properties = ProjectPropertiesFactory.Create(UnconfiguredProjectFactory.Create(),
                        new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TemplateLanguageProperty, "CSharp"));

            return properties;
        }
    }
}
