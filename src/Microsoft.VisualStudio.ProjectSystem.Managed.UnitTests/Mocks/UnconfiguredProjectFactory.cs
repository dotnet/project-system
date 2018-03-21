// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class UnconfiguredProjectFactory
    {
        public static UnconfiguredProject ImplementFullPath(string fullPath)
        {
            return Create(filePath: fullPath);
        }

        public static UnconfiguredProject Create(object hostObject = null, IEnumerable<string> capabilities = null, string filePath = null,
            IProjectConfigurationsService projectConfigurationsService = null, ConfiguredProject configuredProject = null, Encoding projectEncoding = null,
            IProjectCapabilitiesScope scope = null)
        {
            capabilities = capabilities ?? Enumerable.Empty<string>();

            var service = IProjectServiceFactory.Create();

            var unconfiguredProjectServices = new Mock<IUnconfiguredProjectServices>();
            unconfiguredProjectServices.Setup(u => u.HostObject)
                                       .Returns(hostObject);

            unconfiguredProjectServices.Setup(u => u.ProjectConfigurationsService)
                                       .Returns(projectConfigurationsService);

            var project = new Mock<UnconfiguredProject>();
            project.Setup(u => u.ProjectService)
                               .Returns(service);

            project.Setup(u => u.Services)
                               .Returns(unconfiguredProjectServices.Object);

            project.SetupGet(u => u.FullPath)
                                .Returns(filePath);

            project.Setup(u => u.Capabilities)
                               .Returns(scope);

            project.Setup(u => u.GetSuggestedConfiguredProjectAsync()).Returns(Task.FromResult(configuredProject));

            project.Setup(u => u.GetFileEncodingAsync()).Returns(Task.FromResult(projectEncoding));

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
