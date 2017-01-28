// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Moq;
using System;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class UnconfiguredProjectFactory
    {
        public static UnconfiguredProject Create(object hostObject = null, IEnumerable<string> capabilities = null, string filePath = null,
            IProjectConfigurationsService projectConfigurationsService = null, ConfiguredProject configuredProject = null, Encoding projectEncoding = null)
        {
            var service = IProjectServiceFactory.Create();

            var unconfiguredProjectServices = new Mock<IUnconfiguredProjectServices>();
            unconfiguredProjectServices.Setup(u => u.HostObject)
                                       .Returns(hostObject);

            unconfiguredProjectServices.Setup(u => u.ProjectConfigurationsService)
                                       .Returns(projectConfigurationsService);

            var unconfiguredProject = new Mock<UnconfiguredProject>();
            unconfiguredProject.Setup(u => u.ProjectService)
                               .Returns(service);

            unconfiguredProject.Setup(u => u.Services)
                               .Returns(unconfiguredProjectServices.Object);

            unconfiguredProject.SetupGet(u => u.FullPath)
                                .Returns(filePath);

            unconfiguredProject.Setup(u => u.GetSuggestedConfiguredProjectAsync()).Returns(Task.FromResult(configuredProject));

            unconfiguredProject.Setup(u => u.GetFileEncodingAsync()).Returns(Task.FromResult(projectEncoding));

            var capabilitiesScope = IProjectCapabilitiesScopeFactory.Create(capabilities);
            unconfiguredProject.Setup(u => u.Capabilities)
                                .Returns(capabilitiesScope);

            return unconfiguredProject.Object;
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
    }
}
