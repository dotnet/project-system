// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IUnconfiguredProjectFactory
    {
        public static UnconfiguredProject Create(object hostObject = null, IEnumerable<string> capabilities = null, string filePath = null, IProjectConfigurationsService projectConfigurationsService = null)
        {
            capabilities = capabilities ?? Enumerable.Empty<string>();

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

            return unconfiguredProject.Object;
        }
    }
}
