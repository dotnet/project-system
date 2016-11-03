using Microsoft.VisualStudio.ProjectSystem;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Mocks
{
    public class IActiveConfiguredProjectSubscriptionServiceFactory
    {
        public static IActiveConfiguredProjectSubscriptionService CreateInstance()
        {
            var mock = new Mock<IActiveConfiguredProjectSubscriptionService>();

            mock.SetupGet(s => s.ProjectRuleSource)
                .Returns(() => IProjectValueDataSourceFactory.CreateInstance<IProjectSubscriptionUpdate>());

            return mock.Object;
        }
    }
}
