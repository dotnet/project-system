// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class UnconfiguredProjectFactory
    {
        public static UnconfiguredProject ImplementFullPath(string? fullPath)
        {
            return Create(filePath: fullPath);
        }

        public static UnconfiguredProject Create(object? hostObject = null, string? filePath = null,
                                                 IProjectConfigurationsService? projectConfigurationsService = null,
                                                 ConfiguredProject? configuredProject = null, Encoding? projectEncoding = null,
                                                 IProjectAsynchronousTasksService? projectAsynchronousTasksService = null,
                                                 IProjectCapabilitiesScope? scope = null)
        {
            var service = IProjectServiceFactory.Create();

            var unconfiguredProjectServices = new Mock<UnconfiguredProjectServices>();
            unconfiguredProjectServices.SetupGet<object?>(u => u.HostObject)
                                       .Returns(hostObject);

            unconfiguredProjectServices.SetupGet<IProjectConfigurationsService?>(u => u.ProjectConfigurationsService)
                                       .Returns(projectConfigurationsService);

            var activeConfiguredProjectProvider = IActiveConfiguredProjectProviderFactory.Create(getActiveConfiguredProject: () => configuredProject);
            unconfiguredProjectServices.Setup(u => u.ActiveConfiguredProjectProvider)
                                       .Returns(activeConfiguredProjectProvider);

            unconfiguredProjectServices.Setup(u => u.ProjectAsynchronousTasks)
                                       .Returns(projectAsynchronousTasksService!);

            var project = new Mock<UnconfiguredProject>();
            project.Setup(u => u.ProjectService)
                               .Returns(service);

            project.Setup(u => u.Services)
                               .Returns(unconfiguredProjectServices.Object);

            project.SetupGet<string?>(u => u.FullPath)
                                .Returns(filePath);

            project.Setup(u => u.Capabilities)
                               .Returns(scope!);

            project.Setup(u => u.GetSuggestedConfiguredProjectAsync()).ReturnsAsync(configuredProject);

            if (projectEncoding != null)
            {
                project.Setup(u => u.GetFileEncodingAsync()).ReturnsAsync(projectEncoding);
            }

            return project.Object;
        }

        public static UnconfiguredProject CreateWithUnconfiguredProjectAdvanced()
        {
            var mock = new Mock<UnconfiguredProject>();
            mock.As<UnconfiguredProjectAdvanced>();
            return mock.Object;
        }

        public static UnconfiguredProject ImplementGetEncodingAsync(Func<Task<Encoding>> encoding)
        {
            var mock = new Mock<UnconfiguredProject>();
            mock.Setup(u => u.GetFileEncodingAsync()).Returns(encoding);
            return mock.Object;
        }

        public static UnconfiguredProject ImplementLoadConfiguredProjectAsync(Func<ProjectConfiguration, Task<ConfiguredProject>> action)
        {
            var mock = new Mock<UnconfiguredProject>();
            mock.Setup(p => p.LoadConfiguredProjectAsync(It.IsAny<ProjectConfiguration>()))
                .Returns(action);

            return mock.Object;
        }
    }
}
