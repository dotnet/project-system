using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public class AnalyzerConfigItemHandlerTests : CommandLineHandlerTestBase
    {
        internal override ICommandLineHandler CreateInstance()
        {
            return CreateInstance(null, null);
        }

        private AnalyzerConfigItemHandler CreateInstance(UnconfiguredProject project = null, IWorkspaceProjectContext context = null)
        {
            project ??= UnconfiguredProjectFactory.Create();

            var handler = new AnalyzerConfigItemHandler(project);
            if (context != null)
                handler.Initialize(context);

            return handler;
        }
    }
}
