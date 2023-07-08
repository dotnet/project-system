// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using IOleAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.COMAsyncServiceProvider.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    public class VsContainedLanguageComponentsFactoryTests
    {
        private const string LanguageServiceId = "{517FA117-46EB-4402-A0D5-D4B7D89FCC33}";

        [Fact]
        public void GetContainedLanguageFactoryForFile_WhenIsDocumentInProjectFails_ReturnE_FAIL()
        {
            var project = IVsProject_Factory.ImplementIsDocumentInProject(HResult.Fail);

            var factory = CreateInstance(project);

            var result = factory.GetContainedLanguageFactoryForFile("FilePath", out var hierarchyResult, out var itemIdResult, out var containedLanguageFactoryResult);

            AssertFailed(result, hierarchyResult, itemIdResult, containedLanguageFactoryResult);
        }

        [Fact]
        public void GetContainedLanguageFactoryForFile_WhenFilePathNotFound_ReturnE_FAIL()
        {
            var project = IVsProject_Factory.ImplementIsDocumentInProject(found: false);

            var factory = CreateInstance(project);

            var result = factory.GetContainedLanguageFactoryForFile("FilePath", out var hierarchyResult, out var itemIdResult, out var containedLanguageFactoryResult);

            AssertFailed(result, hierarchyResult, itemIdResult, containedLanguageFactoryResult);
        }

        [Theory]
        [InlineData("")]
        [InlineData("ABC")]
        [InlineData("ABCD-ABC")]
        public void GetContainedLanguageFactoryForFile_WhenLanguageServiceIdEmptyOrInvalid_ReturnE_FAIL(string languageServiceId)
        {
            var project = IVsProject_Factory.ImplementIsDocumentInProject(found: true);
            var properties = ProjectPropertiesFactory.Create(ConfigurationGeneral.SchemaName, ConfigurationGeneral.LanguageServiceIdProperty, languageServiceId);

            var factory = CreateInstance(project, properties: properties);

            var result = factory.GetContainedLanguageFactoryForFile("FilePath", out var hierarchyResult, out var itemIdResult, out var containedLanguageFactoryResult);

            AssertFailed(result, hierarchyResult, itemIdResult, containedLanguageFactoryResult);
        }

        [Fact]
        public void GetContainedLanguageFactoryForFile_WhenNoContainedLanguageFactory_ReturnE_FAIL()
        {
            var project = IVsProject_Factory.ImplementIsDocumentInProject(found: true);
            var properties = ProjectPropertiesFactory.Create(ConfigurationGeneral.SchemaName, ConfigurationGeneral.LanguageServiceIdProperty, LanguageServiceId);

            var factory = CreateInstance(project, containedLanguageFactory: null!, properties: properties);

            var result = factory.GetContainedLanguageFactoryForFile("FilePath", out var hierarchyResult, out var itemIdResult, out var containedLanguageFactoryResult);

            AssertFailed(result, hierarchyResult, itemIdResult, containedLanguageFactoryResult);
        }

        [Fact]
        public void GetContainedLanguageFactoryForFile_WhenReturnsResult_ReturnsS_OK()
        {
            var hierarchy = IVsHierarchyFactory.Create();
            var project = IVsProject_Factory.ImplementIsDocumentInProject(found: true, itemid: 1);
            var properties = ProjectPropertiesFactory.Create(ConfigurationGeneral.SchemaName, ConfigurationGeneral.LanguageServiceIdProperty, LanguageServiceId);
            var containedLanguageFactory = IVsContainedLanguageFactoryFactory.Create();

            var factory = CreateInstance(project, containedLanguageFactory: containedLanguageFactory, hierarchy: hierarchy, properties: properties);

            var result = factory.GetContainedLanguageFactoryForFile("FilePath", out var hierarchyResult, out var itemIdResult, out var containedLanguageFactoryResult);

            Assert.Equal(VSConstants.S_OK, result);
            Assert.Same(hierarchy, hierarchyResult);
            Assert.Same(containedLanguageFactory, containedLanguageFactoryResult);
            Assert.Equal(1u, itemIdResult);
        }

        private static void AssertFailed(int result, IVsHierarchy? hierarchy, uint itemid, IVsContainedLanguageFactory? containedLanguageFactory)
        {
            Assert.Equal(VSConstants.E_FAIL, result);
            Assert.Null(hierarchy);
            Assert.Null(containedLanguageFactory);
            Assert.Equal((uint)VSConstants.VSITEMID.Nil, itemid);
        }

        private static VsContainedLanguageComponentsFactory CreateInstance(
            IVsProject4 project,
            IVsContainedLanguageFactory? containedLanguageFactory = null,
            IVsHierarchy? hierarchy = null,
            ProjectProperties? properties = null,
            IWorkspaceWriter? workspaceWriter = null)
        {
            var serviceProvider = IOleAsyncServiceProviderFactory.ImplementQueryServiceAsync(containedLanguageFactory, new Guid(LanguageServiceId));

            var projectVsServices = new IUnconfiguredProjectVsServicesMock();
            projectVsServices.ImplementVsHierarchy(hierarchy);
            projectVsServices.ImplementVsProject(project);
            projectVsServices.ImplementThreadingService(IProjectThreadingServiceFactory.Create());
            projectVsServices.ImplementActiveConfiguredProjectProperties(properties);

            return CreateInstance(serviceProvider, projectVsServices.Object, workspaceWriter);
        }

        private static VsContainedLanguageComponentsFactory CreateInstance(
            IOleAsyncServiceProvider? serviceProvider = null,
            IUnconfiguredProjectVsServices? projectVsServices = null,
            IWorkspaceWriter? workspaceWriter = null)
        {
            projectVsServices ??= IUnconfiguredProjectVsServicesFactory.Create();
            workspaceWriter ??= IWorkspaceWriterFactory.Create();

            return new VsContainedLanguageComponentsFactory(IVsServiceFactory.Create<SAsyncServiceProvider, IOleAsyncServiceProvider>(serviceProvider!),
                                                            projectVsServices,
                                                            workspaceWriter);
        }
    }
}
