// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IUnconfiguredProjectVsServicesFactory
    {
        public static IUnconfiguredProjectVsServices Create()
        {
            return Mock.Of<IUnconfiguredProjectVsServices>();
        }

        public static IUnconfiguredProjectVsServices Implement(Func<IVsHierarchy>? hierarchyCreator = null,
                                                               Func<IVsProject4>? projectCreator = null,
                                                               Func<IProjectThreadingService>? threadingServiceCreator = null,
                                                               Func<ProjectProperties>? projectProperties = null,
                                                               Func<ConfiguredProject>? configuredProjectCreator = null,
                                                               Func<UnconfiguredProject>? unconfiguredProjectCreator = null)
        {
            var mock = new Mock<IUnconfiguredProjectVsServices>();
            if (hierarchyCreator is not null)
            {
                mock.SetupGet(h => h.VsHierarchy)
                    .Returns(hierarchyCreator);
            }

            var threadingService = threadingServiceCreator is null ? IProjectThreadingServiceFactory.Create() : threadingServiceCreator();

            mock.SetupGet(h => h.ThreadingService)
                .Returns(threadingService);

            if (projectCreator is not null)
            {
                mock.SetupGet(h => h.VsProject)
                    .Returns(projectCreator());
            }

            if (configuredProjectCreator is not null)
            {
                mock.SetupGet(h => h.ActiveConfiguredProject)
                    .Returns(configuredProjectCreator);
            }

            if (projectProperties is not null)
            {
                mock.SetupGet(h => h.ActiveConfiguredProjectProperties).Returns(projectProperties());
            }

            if (unconfiguredProjectCreator is not null)
            {
                mock.SetupGet(h => h.Project).Returns(unconfiguredProjectCreator());
            }

            return mock.Object;
        }
    }
}
