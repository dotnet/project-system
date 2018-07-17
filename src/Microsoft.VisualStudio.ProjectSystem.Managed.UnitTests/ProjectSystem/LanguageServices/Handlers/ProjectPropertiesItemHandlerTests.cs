using System;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    [Trait("UnitTest", "ProjectSystem")]
    public class ProjectPropertiesItemHandlerTests
    {
        [Fact]
        public void Constructor_NullAsProject_ThrowsArgumentNull()
        {
            var context = IWorkspaceProjectContextFactory.Create();

            Assert.Throws<ArgumentNullException>("project", () =>
            {
                new ProjectPropertiesItemHandler((UnconfiguredProject)null, context);
            });
        }

        [Fact]
        public void Handle_NullAsVersion_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var projectChange = IProjectChangeDescriptionFactory.Create();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<ArgumentNullException>("version", () =>
            {
                handler.Handle((IComparable)null, projectChange, true, logger);
            });
        }

        [Fact]
        public void Handle_NullAsProjectChange_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<ArgumentNullException>("projectChange", () =>
            {
                handler.Handle(10, (IProjectChangeDescription)null, true, logger);
            });
        }

        [Fact]
        public void Handle_NullAsLogger_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var projectChange = IProjectChangeDescriptionFactory.Create();

            Assert.Throws<ArgumentNullException>("logger", () =>
            {
                handler.Handle(10, projectChange, true, (IProjectLogger)null);
            });
        }

        [Fact]
        public void Handle_WhenNotInitialized_ThrowsInvalidOperation()
        {
            var handler = CreateInstance();
            var projectChange = IProjectChangeDescriptionFactory.Create();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<InvalidOperationException>(() =>
            {
                handler.Handle(10, projectChange, true, logger);
            });
        }

        [Fact]
        public void Initialize_WhenAlreadyInitialized_ThrowsInvalidOperation()
        {
            var handler = CreateInstance();

            var workspaceContext = IWorkspaceProjectContextFactory.Create();

            handler.Initialize(workspaceContext);

            Assert.Throws<InvalidOperationException>(() =>
            {
                handler.Initialize(workspaceContext);
            });
        }

        private ProjectPropertiesItemHandler CreateInstance(UnconfiguredProject project = null, IWorkspaceProjectContext context = null)
        {
            project = project ?? UnconfiguredProjectFactory.Create();

            return new ProjectPropertiesItemHandler(project, context);
        }
    }
}
