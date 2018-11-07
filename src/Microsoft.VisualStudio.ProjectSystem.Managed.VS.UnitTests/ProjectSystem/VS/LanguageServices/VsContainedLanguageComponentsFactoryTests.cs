// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

using Xunit;

using IOleAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    public class VsContainedLanguageComponentsFactoryTests
    {
        private const string LanguageServiceId = "{517FA117-46EB-4402-A0D5-D4B7D89FCC33}";

        [Fact]
        public void GetContainedLanguageFactoryForFile_WhenIsDocumentInProjectFails_ReturnE_FAIL()
        {
            var project = IVsProject_Factory.ImplementIsDocumentInProject(HResult.Fail);

            var factory = CreateInstance(project: project);

            var result = factory.GetContainedLanguageFactoryForFile("FilePath", out var hierarchyResult, out var itemIdResult, out var containedLanguageFactoryResult);

            AssertFailed(result, hierarchyResult, itemIdResult, containedLanguageFactoryResult);
        }

        [Fact]
        public void GetContainedLanguageFactoryForFile_WhenFilePathNotFound_ReturnE_FAIL()
        {
            var project = IVsProject_Factory.ImplementIsDocumentInProject(found: false);

            var factory = CreateInstance(project: project);

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
            var hostObject = IConfiguredProjectHostObjectFactory.Create();

            var factory = CreateInstance(hostObject: hostObject, project: project, properties: properties);

            var result = factory.GetContainedLanguageFactoryForFile("FilePath", out var hierarchyResult, out var itemIdResult, out var containedLanguageFactoryResult);

            AssertFailed(result, hierarchyResult, itemIdResult, containedLanguageFactoryResult);
        }

        [Fact]
        public void GetContainedLanguageFactoryForFile_WhenNoContainedLanguageFactory_ReturnE_FAIL()
        {
            var project = IVsProject_Factory.ImplementIsDocumentInProject(found: true);
            var properties = ProjectPropertiesFactory.Create(ConfigurationGeneral.SchemaName, ConfigurationGeneral.LanguageServiceIdProperty, LanguageServiceId);
            var hostObject = IConfiguredProjectHostObjectFactory.Create();

            var factory = CreateInstance((IVsContainedLanguageFactory)null, hostObject: hostObject, project: project, properties: properties);

            var result = factory.GetContainedLanguageFactoryForFile("FilePath", out var hierarchyResult, out var itemIdResult, out var containedLanguageFactoryResult);

            AssertFailed(result, hierarchyResult, itemIdResult, containedLanguageFactoryResult);
        }

        [Fact]
        public void GetContainedLanguageFactoryForFile_WhenNoActiveIntellisenseProjectHostObject_ReturnsE_FAIL()
        {
            var project = IVsProject_Factory.ImplementIsDocumentInProject(found: true);
            var properties = ProjectPropertiesFactory.Create(ConfigurationGeneral.SchemaName, ConfigurationGeneral.LanguageServiceIdProperty, LanguageServiceId);
            var containedLanguageFactory = IVsContainedLanguageFactoryFactory.Create();

            var factory = CreateInstance(containedLanguageFactory, hostObject: null, project: project, properties: properties);

            var result = factory.GetContainedLanguageFactoryForFile("FilePath", out var hierarchyResult, out var itemIdResult, out var containedLanguageFactoryResult);

            AssertFailed(result, hierarchyResult, itemIdResult, containedLanguageFactoryResult);
        }

        [Fact]
        public void GetContainedLanguageFactoryForFile_WhenReturnsResult_ReturnsS_OK()
        {
            var project = IVsProject_Factory.ImplementIsDocumentInProject(found: true, itemid: 1);
            var properties = ProjectPropertiesFactory.Create(ConfigurationGeneral.SchemaName, ConfigurationGeneral.LanguageServiceIdProperty, LanguageServiceId);
            var hostObject = IConfiguredProjectHostObjectFactory.Create();
            var containedLanguageFactory = IVsContainedLanguageFactoryFactory.Create();
            
            var factory = CreateInstance(containedLanguageFactory, hostObject: hostObject, project: project, properties: properties);

            var result = factory.GetContainedLanguageFactoryForFile("FilePath", out var hierarchyResult, out var itemIdResult, out var containedLanguageFactoryResult);

            Assert.Equal(VSConstants.S_OK, result);
            Assert.Same(hostObject, hierarchyResult);
            Assert.Same(containedLanguageFactory, containedLanguageFactoryResult);
            Assert.Equal(1u, itemIdResult);
        }

        private static void AssertFailed(int result, IVsHierarchy hierarchy, uint itemid, IVsContainedLanguageFactory containedLanguageFactory)
        {
            Assert.Equal(VSConstants.E_FAIL, result);
            Assert.Null(hierarchy);
            Assert.Null(containedLanguageFactory);
            Assert.Equal((uint)VSConstants.VSITEMID.Nil, itemid);
        }

        private static VsContainedLanguageComponentsFactory CreateInstance(IVsContainedLanguageFactory containedLanguageFactory = null, IVsProject4 project = null, ProjectProperties properties = null, IConfiguredProjectHostObject hostObject = null, IActiveWorkspaceProjectContextHost projectContextHost = null)
        {
            var hostProvider = IProjectHostProviderFactory.ImplementActiveIntellisenseProjectHostObject(hostObject);
            var serviceProvider = IOleAsyncServiceProviderFactory.ImplementQueryServiceAsync(containedLanguageFactory, new Guid(LanguageServiceId));

            var projectVsServices = new IUnconfiguredProjectVsServicesMock();
            projectVsServices.ImplementVsProject(project);
            projectVsServices.ImplementThreadingService(IProjectThreadingServiceFactory.Create());
            projectVsServices.ImplementActiveConfiguredProjectProperties(properties);

            return CreateInstance(serviceProvider, projectVsServices.Object, hostProvider, projectContextHost);
        }

        private static VsContainedLanguageComponentsFactory CreateInstance(IOleAsyncServiceProvider serviceProvider = null, IUnconfiguredProjectVsServices projectVsServices = null, IProjectHostProvider projectHostProvider = null, IActiveWorkspaceProjectContextHost projectContextHost = null)
        {
            projectVsServices = projectVsServices ?? IUnconfiguredProjectVsServicesFactory.Create();
            projectHostProvider = projectHostProvider ?? IProjectHostProviderFactory.Create();
            projectContextHost = projectContextHost ?? IActiveWorkspaceProjectContextHostFactory.Create();

            return new VsContainedLanguageComponentsFactory(IVsServiceFactory.Create<SAsyncServiceProvider, IOleAsyncServiceProvider>(serviceProvider),
                                                            projectVsServices,
                                                            projectHostProvider,
                                                            projectContextHost);
        }
    }
}
