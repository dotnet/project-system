// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IUnconfiguredProjectCommonServicesFactory
    {
        public static IUnconfiguredProjectCommonServices Create(UnconfiguredProject? project = null, IProjectThreadingService? threadingService = null,
                                                                ConfiguredProject? configuredProject = null, ProjectProperties? projectProperties = null,
                                                                IProjectAccessor? projectAccessor = null)
        {
            var mock = new Mock<IUnconfiguredProjectCommonServices>();

            if (project != null)
                mock.Setup(s => s.Project)
                    .Returns(project);

            if (threadingService != null)
                mock.Setup(s => s.ThreadingService)
                    .Returns(threadingService);

            if (configuredProject != null)
                mock.Setup(s => s.ActiveConfiguredProject)
                    .Returns(configuredProject);

            if (projectProperties != null)
                mock.Setup(s => s.ActiveConfiguredProjectProperties)
                    .Returns(projectProperties);

            if (projectAccessor != null)
                mock.Setup(s => s.ProjectAccessor)
                    .Returns(projectAccessor);

            return mock.Object;
        }
    }
}
