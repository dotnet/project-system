using System;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    [Trait("UnitTest", "ProjectSystem")]
    public class AdditionalFilesItemHandlerTests
    {
        [Fact]
        public void Constructor_NullAsProject_ThrowsArgumentNull()
        {
            var context = IWorkspaceProjectContextFactory.Create();

            Assert.Throws<ArgumentNullException>("project", () =>
            {
                new AdditionalFilesItemHandler((UnconfiguredProject)null, context);
            });
        }

        [Fact]
        public void Handle_NullAsVersion_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var added = BuildOptionsFactory.CreateEmpty();
            var removed = BuildOptionsFactory.CreateEmpty();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<ArgumentNullException>("version", () =>
            {
                handler.Handle((IComparable)null, added, removed, true, logger);
            });
        }

        [Fact]
        public void Handle_NullAsAdded_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var removed = BuildOptionsFactory.CreateEmpty();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<ArgumentNullException>("added", () =>
            {
                handler.Handle(10, (BuildOptions)null, removed, true, logger);
            });
        }

        [Fact]
        public void Handle_NullAsRemoved_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var added = BuildOptionsFactory.CreateEmpty();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<ArgumentNullException>("removed", () =>
            {
                handler.Handle(10, added, (BuildOptions)null, true, logger);
            });
        }

        [Fact]
        public void Handle_NullAsLogger_ThrowsArgumentNull()
        {
            var handler = CreateInstance();
            var added = BuildOptionsFactory.CreateEmpty();
            var removed = BuildOptionsFactory.CreateEmpty();

            Assert.Throws<ArgumentNullException>("logger", () =>
            {
                handler.Handle(10, added, removed, true, (IProjectLogger)null);
            });
        }

        [Fact]
        public void Handle_WhenNotInitialized_ThrowsInvalidOperation()
        {
            var handler = CreateInstance();
            var added = BuildOptionsFactory.CreateEmpty();
            var removed = BuildOptionsFactory.CreateEmpty();
            var logger = Mock.Of<IProjectLogger>();

            Assert.Throws<InvalidOperationException>(() =>
            {
                handler.Handle(10, added, removed, true, logger);
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

        private MetadataReferenceItemHandler CreateInstance(UnconfiguredProject project = null, IWorkspaceProjectContext context = null)
        {
            project = project ?? UnconfiguredProjectFactory.Create();

            return new MetadataReferenceItemHandler(project, context);
        }
    }
}
