using Moq;
using System;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IVsUnconfiguredProjectIntegrationServiceFactory
    {
        public static IVsUnconfiguredProjectIntegrationService Create() => Mock.Of<IVsUnconfiguredProjectIntegrationService>();

        public static IVsUnconfiguredProjectIntegrationService ImplementProjectTypeGuid(Guid projectType)
        {
            var mock = new Mock<IVsUnconfiguredProjectIntegrationService>();
            mock.SetupGet(u => u.ProjectTypeGuid).Returns(projectType);

            return mock.Object;
        }
    }
}
