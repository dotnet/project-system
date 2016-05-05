// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [ProjectSystemTrait]
    public class UnconfiguredProjectCommonServicesTests
    {
        [Fact]
        public void Constructor_NullAsThreadingService_ThrowsArgumentNull()
        {
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            Assert.Throws<ArgumentNullException>("threadingService", () => {
                new UnconfiguredProjectCommonServices((Lazy<IProjectThreadingService>)null, activeConfiguredProject, activeConfiguredProjectProperties);
            });
        }

        [Fact]
        public void Constructor_NullAsActiveConfiguredProject_ThrowsArgumentNull()
        {
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            Assert.Throws<ArgumentNullException>("activeConfiguredProject", () => {
                new UnconfiguredProjectCommonServices(threadingService, (ActiveConfiguredProject<ConfiguredProject>)null, activeConfiguredProjectProperties);
            });
        }

        [Fact]
        public void Constructor_NullAsActiveConfguredProjectProperties_ThrowsArgumentNull()
        {
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);

            Assert.Throws<ArgumentNullException>("activeConfiguredProjectProperties", () => {
                new UnconfiguredProjectCommonServices(threadingService, activeConfiguredProject, (ActiveConfiguredProject<ProjectProperties>)null);
            });
        }

        [Fact]
        public void Constructor_ValueAsThreadingService_SetsThreadingServiceProperty()
        {
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var services = new UnconfiguredProjectCommonServices(ThreadingService, activeConfiguredProject, activeConfiguredProjectProperties);

            Assert.Same(threadingService.Value, services.ThreadingService);
        }

        [Fact]
        public void Constructor_ValueAsActiveConfiguredProject_SetsActiveConfiguredProjectProperty()
        {
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var services = new UnconfiguredProjectCommonServices(threadingService, activeConfiguredProject, activeConfiguredProjectProperties);

            Assert.Same(projectProperties.ConfiguredProject, services.ActiveConfiguredProject);
        }

        [Fact]
        public void Constructor_ValueAsActiveConfiguredProjectProperties_SetsActiveConfiguredProjectPropertiesProperty()
        {
            var threadingService = new Lazy<IProjectThreadingService>(() => IProjectThreadingServiceFactory.Create());
            var unconfiguredProject = IUnconfiguredProjectFactory.Create();
            var projectProperties = ProjectPropertiesFactory.Create(unconfiguredProject);
            var activeConfiguredProject = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties.ConfiguredProject);
            var activeConfiguredProjectProperties = IActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var services = new UnconfiguredProjectCommonServices(threadingService, activeConfiguredProject, activeConfiguredProjectProperties);

            Assert.Same(projectProperties, services.ActiveConfiguredProjectProperties);
        }
    }
}
