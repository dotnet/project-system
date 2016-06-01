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
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            Assert.Throws<ArgumentNullException>("project", () => {
                new UnconfiguredProjectCommonServices((UnconfiguredProject)null, threadingService, activeConfiguredProject, activeConfiguredProjectProperties);
            });
        }

        [Fact]
        public void Constructor_NullAsThreadingService_ThrowsArgumentNull()
        {
            var project = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            Assert.Throws<ArgumentNullException>("threadingService", () => {
                new UnconfiguredProjectCommonServices(project, (Lazy<IProjectThreadingService>)null, activeConfiguredProject, activeConfiguredProjectProperties);
            });
        }

        [Fact]
        public void Constructor_NullAsActiveConfiguredProject_ThrowsArgumentNull()
        {
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var project = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            Assert.Throws<ArgumentNullException>("activeConfiguredProject", () => {
                new UnconfiguredProjectCommonServices(project, threadingService, (ActiveConfiguredProject<ConfiguredProject>)null, activeConfiguredProjectProperties);
            });
        }

        [Fact]
        public void Constructor_NullAsActiveConfguredProjectProperties_ThrowsArgumentNull()
        {
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var project = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);

            Assert.Throws<ArgumentNullException>("activeConfiguredProjectProperties", () => {
                new UnconfiguredProjectCommonServices(project, threadingService, activeConfiguredProject, (ActiveConfiguredProject<ProjectProperties>)null);
            });
        }

        [Fact]
        public void Constructor_ValueAsProjecte_SetsProjectProperty()
        {
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var project = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var services = new UnconfiguredProjectCommonServices(project, threadingService, activeConfiguredProject, activeConfiguredProjectProperties);

            Assert.Same(project, services.Project);
        }

        [Fact]
        public void Constructor_ValueAsThreadingService_SetsThreadingServiceProperty()
        {
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var project = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var services = new UnconfiguredProjectCommonServices(project, threadingService, activeConfiguredProject, activeConfiguredProjectProperties);

            Assert.Same(threadingService.Value, services.ThreadingService);
        }

        [Fact]
        public void Constructor_ValueAsActiveConfiguredProject_SetsActiveConfiguredProjectProperty()
        {
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var project = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var services = new UnconfiguredProjectCommonServices(project, threadingService, activeConfiguredProject, activeConfiguredProjectProperties);

            Assert.Same(projectProperties.ConfiguredProject, services.ActiveConfiguredProject);
        }

        [Fact]
        public void Constructor_ValueAsActiveConfiguredProjectProperties_SetsActiveConfiguredProjectPropertiesProperty()
        {
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var project = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var services = new UnconfiguredProjectCommonServices(project, threadingService, activeConfiguredProject, activeConfiguredProjectProperties);

            Assert.Same(projectProperties, services.ActiveConfiguredProjectProperties);
        }
    }
}
