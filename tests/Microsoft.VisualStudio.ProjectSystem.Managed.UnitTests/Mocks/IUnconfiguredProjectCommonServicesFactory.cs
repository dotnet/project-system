// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IUnconfiguredProjectCommonServicesFactory
    {
        public static IUnconfiguredProjectCommonServices Create(UnconfiguredProject? project = null, IProjectThreadingService? threadingService = null,
                                                                ConfiguredProject? configuredProject = null, ProjectProperties? projectProperties = null,
                                                                IProjectAccessor? projectAccessor = null)
        {
            var mock = new Mock<IUnconfiguredProjectCommonServices>();

            if (project is not null)
                mock.Setup(s => s.Project)
                    .Returns(project);

            if (threadingService is not null)
                mock.Setup(s => s.ThreadingService)
                    .Returns(threadingService);

            if (configuredProject is not null)
                mock.Setup(s => s.ActiveConfiguredProject)
                    .Returns(configuredProject);

            if (projectProperties is not null)
                mock.Setup(s => s.ActiveConfiguredProjectProperties)
                    .Returns(projectProperties);

            if (projectAccessor is not null)
                mock.Setup(s => s.ProjectAccessor)
                    .Returns(projectAccessor);

            return mock.Object;
        }
    }
}
