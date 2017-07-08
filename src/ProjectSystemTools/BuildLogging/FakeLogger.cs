using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    internal sealed class FakeLogger : ILogger, IBuild
    {
        private readonly IBuildManager _buildManager;

        public LoggerVerbosity Verbosity { get => LoggerVerbosity.Quiet; set { } }

        public ConfiguredProject ConfiguredProject { get; }

        public IReadOnlyList<string> Targets { get; }

        public IImmutableDictionary<string, string> Properties { get; }

        public string Parameters { get => null; set { } }

        public FakeLogger(IBuildManager buildManager, ConfiguredProject configuredProject, IReadOnlyList<string> targets, IImmutableDictionary<string, string> properties)
        {
            _buildManager = buildManager;
            ConfiguredProject = configuredProject;
            Targets = targets;
            Properties = properties;
        }

        public void Initialize(IEventSource eventSource)
        {
            _buildManager.NotifyBuildStarted(this);
        }

        public void Shutdown()
        {
            _buildManager.NotifyBuildEnded(this);
        }
    }
}
