using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Microsoft.VisualStudio.ProjectSystem;

using Moq;

namespace Microsoft.VisualStudio
{
    internal static class IProjectSnapshotFactory
    {
        internal static IProjectSnapshot FromProjectXml(string xmlString)
        {
            var project = new ProjectInstance(ProjectRootElementFactory.Create(xmlString));

            var mock = new Mock<IProjectSnapshot>();
            mock.Setup(p => p.ProjectInstance).Returns(project);
            return mock.Object;
        }
    }
}
