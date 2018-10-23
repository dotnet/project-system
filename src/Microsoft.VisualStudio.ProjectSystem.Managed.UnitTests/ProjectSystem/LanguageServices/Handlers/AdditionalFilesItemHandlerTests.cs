using System;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public class AdditionalFilesItemHandlerTests : CommandLineHandlerTestBase
    {
        [Fact]
        public void Constructor_NullAsProject_ThrowsArgumentNull()
        {
            var context = IWorkspaceProjectContextMockFactory.Create();

            Assert.Throws<ArgumentNullException>("project", () =>
            {
                new AdditionalFilesItemHandler((UnconfiguredProject)null);
            });
        }

        internal override ICommandLineHandler CreateInstance()
        {
            return CreateInstance(null, null);
        }

        private AdditionalFilesItemHandler CreateInstance(UnconfiguredProject project = null, IWorkspaceProjectContext context = null)
        {
            project = project ?? UnconfiguredProjectFactory.Create();

            var handler = new AdditionalFilesItemHandler(project);
            if (context != null)
                handler.Initialize(context);

            return handler;
        }
    }
}
