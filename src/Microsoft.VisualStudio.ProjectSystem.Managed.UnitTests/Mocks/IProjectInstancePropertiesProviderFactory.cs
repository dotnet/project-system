using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem.Properties;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectInstancePropertiesProviderFactory
    {
        public static IProjectInstancePropertiesProvider Create(IProjectProperties itemProps = null, IProjectProperties commonProps = null)
        {
            var mock = new Mock<IProjectInstancePropertiesProvider>();

            if (itemProps != null)
            {
                mock.Setup(t => t.GetItemProperties(It.IsAny<ProjectInstance>(), It.IsAny<string>(), It.IsAny<string>()))
                                .Returns(itemProps);
                mock.Setup(t => t.GetItemProperties(It.IsAny<ITaskItem>()))
                                .Returns(itemProps);
            }

            if (commonProps != null)
            {
                mock.Setup(t => t.GetCommonProperties(It.IsAny<ProjectInstance>())).Returns(commonProps);
            }

            return mock.Object;
        }

            public static IProjectInstancePropertiesProvider ImplementsGetItemTypeProperties(IProjectProperties projectProperties = null)
        {
            var mock = new Mock<IProjectInstancePropertiesProvider>();

            mock.Setup(d => d.GetItemTypeProperties(It.IsAny<ProjectInstance>(), It.IsAny<string>()))
                .Returns(() => projectProperties ?? Mock.Of<IProjectProperties>());

            return mock.Object;
        }
    }
}
