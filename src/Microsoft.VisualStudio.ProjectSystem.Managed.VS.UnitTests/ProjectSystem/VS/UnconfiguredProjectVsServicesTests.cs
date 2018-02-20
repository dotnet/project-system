// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.Shell.Interop;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Trait("UnitTest", "ProjectSystem")]
    public class UnconfiguredProjectVsServicesTests
    {
        [Fact]
        public void Constructor_NullAsCommonSevices_ThrowsArgumentNull()
        {
            var project = UnconfiguredProjectFactory.Create();

            Assert.Throws<ArgumentNullException>("commonServices", () =>
            {
                new UnconfiguredProjectVsServices((IUnconfiguredProjectCommonServices)null);
            });
        }

        [Fact]
        public void Constructor_ValueAsUnconfiguedProject_SetsVsHierarchyToHostObject()
        {
            var hierarchy = IVsHierarchyFactory.Create();
            var project = UnconfiguredProjectFactory.Create(hostObject: hierarchy);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var vsServices = CreateInstance(commonServices);

            Assert.Same(hierarchy, vsServices.VsHierarchy);
        }

        [Fact]
        public void Constructor_ValueAsUnconfiguedProject_SetsVsProjectToHostObject()
        {
            var hierarchy = IVsHierarchyFactory.Create();
            var project = UnconfiguredProjectFactory.Create(hostObject: hierarchy);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var vsServices = CreateInstance(commonServices);

            Assert.Same(hierarchy, vsServices.VsProject);
        }

        [Fact]
        public void Constructor_ValueAsCommonServices_SetsProjectToCommonServicesProject()
        {
            var project = UnconfiguredProjectFactory.Create();
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(project: project);

            var vsServices = CreateInstance(commonServices);

            Assert.Same(project, vsServices.Project);
        }

        [Fact]
        public void Constructor_ValueAsCommonServices_SetsProjectTreeToCommonServicesProjectTree()
        {
            var projectTree = IPhysicalProjectTreeFactory.Create();
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(projectTree: projectTree);

            var vsServices = CreateInstance(commonServices);

            Assert.Same(projectTree, vsServices.ProjectTree);
        }

        [Fact]
        public void Constructor_ValueAsCommonServices_SetsThreadingServiceToCommonServicesThreadingService()
        {
            var threadingService = IProjectThreadingServiceFactory.Create();
            var project = UnconfiguredProjectFactory.Create();
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(threadingService: threadingService);

            var vsServices = CreateInstance(commonServices);

            Assert.Same(threadingService, vsServices.ThreadingService);
        }

        [Fact]
        public void Constructor_ValueAsCommonServices_SetsActiveConfiguredProjectProjectToCommonServicesActiveConfiguredProject()
        {
            var project = UnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(configuredProject: projectProperties.ConfiguredProject);

            var vsServices = CreateInstance(commonServices);

            Assert.Same(projectProperties.ConfiguredProject, vsServices.ActiveConfiguredProject);
        }

        [Fact]
        public void Constructor_ValueAsCommonServices_SetsActiveConfiguredProjectPropertiesToCommonServicesActiveConfiguredProjectProperties()
        {
            var project = UnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var commonServices = IUnconfiguredProjectCommonServicesFactory.Create(projectProperties: projectProperties);

            var vsServices = CreateInstance(commonServices);

            Assert.Same(projectProperties, vsServices.ActiveConfiguredProjectProperties);
        }

        private static UnconfiguredProjectVsServices CreateInstance(IUnconfiguredProjectCommonServices commonServices)
        {
            return new UnconfiguredProjectVsServices(commonServices);
        }
    }
}
