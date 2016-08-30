using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    public class IDebugLaunchProviderMetadataViewFactory
    {
        public static IDebugLaunchProviderMetadataView CreateInstance()
        {
            var iDebugLaunchProviderMetadataView = new Mock<IDebugLaunchProviderMetadataView>();
            return iDebugLaunchProviderMetadataView.Object;
        }
    }
}
