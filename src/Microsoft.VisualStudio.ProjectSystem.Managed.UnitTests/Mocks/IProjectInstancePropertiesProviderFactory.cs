using Moq;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.Build.Execution;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectInstancePropertiesProviderFactory
    {
        public static IProjectInstancePropertiesProvider Create()
            => Mock.Of<IProjectInstancePropertiesProvider>();

        public static IProjectInstancePropertiesProvider ImplementsGetItemTypeProperties(IProjectProperties projectProperties = null)
        {
            var mock = new Mock<IProjectInstancePropertiesProvider>();

            mock.Setup(d => d.GetItemTypeProperties(It.IsAny<ProjectInstance>(), It.IsAny<string>()))
                .Returns(() => projectProperties ?? Mock.Of<IProjectProperties>());

            return mock.Object;
        }
    }
}
