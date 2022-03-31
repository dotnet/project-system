// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio
{
    internal static class IProjectServiceAccessorFactory
    {
        public static IProjectServiceAccessor Create(IProjectCapabilitiesScope? scope = null, ConfiguredProject? configuredProject = null)
        {
            var mock = new Mock<IProjectServiceAccessor>();
            mock.Setup(s => s.GetProjectService(It.IsAny<ProjectServiceThreadingModel>()))
                .Returns(() => IProjectServiceFactory.Create(scope: scope, configuredProject: configuredProject));
            return mock.Object;
        }

        public static IProjectServiceAccessor Create(IEnumerable<UnconfiguredProject> loadedUnconfiguredProjects, IProjectCapabilitiesScope? scope = null)
        {
            var mock = new Mock<IProjectServiceAccessor>();
            mock.Setup(s => s.GetProjectService(It.IsAny<ProjectServiceThreadingModel>()))
                .Returns(() => IProjectServiceFactory.Create(scope: scope, loadedUnconfiguredProjects: loadedUnconfiguredProjects));
            return mock.Object;
        }
    }
}
