using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Mocks
{
    public class IDebugLaunchProviderFactory
    {
        public static IDebugLaunchProvider CreateInstance(bool debugs)
        {
            var iDebugLaunchProvider = new Mock<IDebugLaunchProvider>();

            iDebugLaunchProvider.Setup(d => d.CanLaunchAsync(It.IsAny<DebugLaunchOptions>()))
                                .Returns(() => Task.FromResult(debugs));

            return iDebugLaunchProvider.Object;
        }
    }
}
