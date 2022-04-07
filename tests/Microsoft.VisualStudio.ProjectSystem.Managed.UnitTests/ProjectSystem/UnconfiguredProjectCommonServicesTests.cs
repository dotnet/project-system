// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class UnconfiguredProjectCommonServicesTests
    {
        [Fact]
        public void Constructor_ValueAsProject_SetsProjectProperty()
        {
            var project = UnconfiguredProjectFactory.Create();
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = IActiveConfiguredValueFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredValueFactory.ImplementValue(() => projectProperties);
            var projectAccessor = new Lazy<IProjectAccessor>(() => IProjectAccessorFactory.Create());

            var services = new UnconfiguredProjectCommonServices(project, threadingService, activeConfiguredProject, activeConfiguredProjectProperties, projectAccessor);

            Assert.Same(project, services.Project);
        }

        [Fact]
        public void Constructor_ValueAsThreadingService_SetsThreadingServiceProperty()
        {
            var project = UnconfiguredProjectFactory.Create();
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = IActiveConfiguredValueFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredValueFactory.ImplementValue(() => projectProperties);
            var projectAccessor = new Lazy<IProjectAccessor>(() => IProjectAccessorFactory.Create());

            var services = new UnconfiguredProjectCommonServices(project, threadingService, activeConfiguredProject, activeConfiguredProjectProperties, projectAccessor);

            Assert.Same(threadingService.Value, services.ThreadingService);
        }

        [Fact]
        public void Constructor_ValueAsActiveConfiguredProject_SetsActiveConfiguredProjectProperty()
        {
            var project = UnconfiguredProjectFactory.Create();
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = IActiveConfiguredValueFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredValueFactory.ImplementValue(() => projectProperties);
            var projectAccessor = new Lazy<IProjectAccessor>(() => IProjectAccessorFactory.Create());

            var services = new UnconfiguredProjectCommonServices(project, threadingService, activeConfiguredProject, activeConfiguredProjectProperties, projectAccessor);

            Assert.Same(projectProperties.ConfiguredProject, services.ActiveConfiguredProject);
        }

        [Fact]
        public void Constructor_ValueAsActiveConfiguredProjectProperties_SetsActiveConfiguredProjectPropertiesProperty()
        {
            var project = UnconfiguredProjectFactory.Create();
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = IActiveConfiguredValueFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredValueFactory.ImplementValue(() => projectProperties);
            var projectAccessor = new Lazy<IProjectAccessor>(() => IProjectAccessorFactory.Create());

            var services = new UnconfiguredProjectCommonServices(project, threadingService, activeConfiguredProject, activeConfiguredProjectProperties, projectAccessor);

            Assert.Same(projectProperties, services.ActiveConfiguredProjectProperties);
        }

        [Fact]
        public void Constructor_ValueAsProjectAccessor_SetsProjectAccessorProperty()
        {
            var project = UnconfiguredProjectFactory.Create();
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var projectProperties = ProjectPropertiesFactory.Create(project);
            var activeConfiguredProject = IActiveConfiguredValueFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredValueFactory.ImplementValue(() => projectProperties);
            var projectAccessor = new Lazy<IProjectAccessor>(() => IProjectAccessorFactory.Create());

            var services = new UnconfiguredProjectCommonServices(project, threadingService, activeConfiguredProject, activeConfiguredProjectProperties, projectAccessor);

            Assert.Same(projectAccessor.Value, services.ProjectAccessor);
        }
    }
}
