using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
