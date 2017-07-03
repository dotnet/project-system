using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    internal sealed class FakeLogger : ILogger
    {
        private IBuildManager _buildManager;
        private ConfiguredProject _configuredProject;

        public LoggerVerbosity Verbosity { get => LoggerVerbosity.Quiet; set { } }

        public string Parameters { get => null; set { } }

        public FakeLogger(IBuildManager buildManager, ConfiguredProject configuredProject)
        {
            _buildManager = buildManager;
            _configuredProject = configuredProject;
        }

        public void Initialize(IEventSource eventSource)
        {
            _buildManager.NotifyBuildStarted(_configuredProject);
        }

        public void Shutdown()
        {
            _buildManager.NotifyBuildEnded(_configuredProject);
        }
    }
}
