// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [ProjectSystemTrait]
    public class UnconfiguredProjectCommonServicesTests
    {
        [Fact]
        public void Constructor_NullAsProject_ThrowsArgumentNull()
        {
            var project = UnconfiguredProjectFactory.Create();
            var projectTree = new Lazy<IPhysicalProjectTree>(() => IPhysicalProjectTreeFactory.Create());
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);
            var projectLockService = new Lazy<IProjectLockService>(() => IProjectLockServiceFactory.Create());

            Assert.Throws<ArgumentNullException>("project", () => {
                new UnconfiguredProjectCommonServices((UnconfiguredProject)null, projectTree, threadingService, activeConfiguredProject, activeConfiguredProjectProperties, projectLockService);
            });
        }

        [Fact]
        public void Constructor_NullAsProjectTree_ThrowsArgumentNull()
        {
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var project = UnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);
            var projectLockService = new Lazy<IProjectLockService>(() => IProjectLockServiceFactory.Create());

            Assert.Throws<ArgumentNullException>("projectTree", () => {
                new UnconfiguredProjectCommonServices(project, (Lazy<IPhysicalProjectTree>)null, threadingService, activeConfiguredProject, activeConfiguredProjectProperties, projectLockService);
            });
        }

        [Fact]
        public void Constructor_NullAsThreadingService_ThrowsArgumentNull()
        {
            var project = UnconfiguredProjectFactory.Create();
            var projectTree = new Lazy<IPhysicalProjectTree>(() => IPhysicalProjectTreeFactory.Create());
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);
            var projectLockService = new Lazy<IProjectLockService>(() => IProjectLockServiceFactory.Create());

            Assert.Throws<ArgumentNullException>("threadingService", () => {
                new UnconfiguredProjectCommonServices(project, projectTree, (Lazy<IProjectThreadingService>)null, activeConfiguredProject, activeConfiguredProjectProperties, projectLockService);
            });
        }

        [Fact]
        public void Constructor_NullAsActiveConfiguredProject_ThrowsArgumentNull()
        {
            var project = UnconfiguredProjectFactory.Create();
            var projectTree = new Lazy<IPhysicalProjectTree>(() => IPhysicalProjectTreeFactory.Create());
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProjectProperties = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);
            var projectLockService = new Lazy<IProjectLockService>(() => IProjectLockServiceFactory.Create());

            Assert.Throws<ArgumentNullException>("activeConfiguredProject", () => {
                new UnconfiguredProjectCommonServices(project, projectTree, threadingService, (ActiveConfiguredProject<ConfiguredProject>)null, activeConfiguredProjectProperties, projectLockService);
            });
        }

        [Fact]
        public void Constructor_NullAsActiveConfguredProjectProperties_ThrowsArgumentNull()
        {
            var project = UnconfiguredProjectFactory.Create();
            var projectTree = new Lazy<IPhysicalProjectTree>(() => IPhysicalProjectTreeFactory.Create());
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var projectLockService = new Lazy<IProjectLockService>(() => IProjectLockServiceFactory.Create());

            Assert.Throws<ArgumentNullException>("activeConfiguredProjectProperties", () => {
                new UnconfiguredProjectCommonServices(project, projectTree, threadingService, activeConfiguredProject, (ActiveConfiguredProject<ProjectProperties>)null, projectLockService);
            });
        }

        [Fact]
        public void Constructor_NullAsProjectLockService_ThrowsArgumentNull()
        {
            var project = UnconfiguredProjectFactory.Create();
            var projectTree = new Lazy<IPhysicalProjectTree>(() => IPhysicalProjectTreeFactory.Create());
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            Assert.Throws<ArgumentNullException>("projectLockService", () => {
                new UnconfiguredProjectCommonServices(project, projectTree, threadingService, activeConfiguredProject, activeConfiguredProjectProperties, null);
            });
        }

        [Fact]
        public void Constructor_ValueAsProject_SetsProjectProperty()
        {
            var project = UnconfiguredProjectFactory.Create();
            var projectTree = new Lazy<IPhysicalProjectTree>(() => IPhysicalProjectTreeFactory.Create());
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);
            var projectLockService = new Lazy<IProjectLockService>(() => IProjectLockServiceFactory.Create());

            var services = new UnconfiguredProjectCommonServices(project, projectTree, threadingService, activeConfiguredProject, activeConfiguredProjectProperties, projectLockService);

            Assert.Same(project, services.Project);
        }

        [Fact]
        public void Constructor_ValueAsProjectTree_SetsProjectTreeProperty()
        {
            var project = UnconfiguredProjectFactory.Create();
            var projectTree = new Lazy<IPhysicalProjectTree>(() => IPhysicalProjectTreeFactory.Create());
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);
            var projectLockService = new Lazy<IProjectLockService>(() => IProjectLockServiceFactory.Create());

            var services = new UnconfiguredProjectCommonServices(project, projectTree, threadingService, activeConfiguredProject, activeConfiguredProjectProperties, projectLockService);

            Assert.Same(projectTree.Value, services.ProjectTree);
        }

        [Fact]
        public void Constructor_ValueAsThreadingService_SetsThreadingServiceProperty()
        {
            var project = UnconfiguredProjectFactory.Create();
            var projectTree = new Lazy<IPhysicalProjectTree>(() => IPhysicalProjectTreeFactory.Create());
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);
            var projectLockService = new Lazy<IProjectLockService>(() => IProjectLockServiceFactory.Create());

            var services = new UnconfiguredProjectCommonServices(project, projectTree, threadingService, activeConfiguredProject, activeConfiguredProjectProperties, projectLockService);

            Assert.Same(threadingService.Value, services.ThreadingService);
        }

        [Fact]
        public void Constructor_ValueAsActiveConfiguredProject_SetsActiveConfiguredProjectProperty()
        {
            var project = UnconfiguredProjectFactory.Create();
            var projectTree = new Lazy<IPhysicalProjectTree>(() => IPhysicalProjectTreeFactory.Create());
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);
            var projectLockService = new Lazy<IProjectLockService>(() => IProjectLockServiceFactory.Create());

            var services = new UnconfiguredProjectCommonServices(project, projectTree, threadingService, activeConfiguredProject, activeConfiguredProjectProperties, projectLockService);

            Assert.Same(projectProperties.ConfiguredProject, services.ActiveConfiguredProject);
        }

        [Fact]
        public void Constructor_ValueAsActiveConfiguredProjectProperties_SetsActiveConfiguredProjectPropertiesProperty()
        {
            var project = UnconfiguredProjectFactory.Create();
            var projectTree = new Lazy<IPhysicalProjectTree>(() => IPhysicalProjectTreeFactory.Create());
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);
            var projectLockService = new Lazy<IProjectLockService>(() => IProjectLockServiceFactory.Create());

            var services = new UnconfiguredProjectCommonServices(project, projectTree, threadingService, activeConfiguredProject, activeConfiguredProjectProperties, projectLockService);

            Assert.Same(projectProperties, services.ActiveConfiguredProjectProperties);
        }
    }
}
