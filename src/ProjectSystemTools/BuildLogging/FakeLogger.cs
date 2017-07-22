using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging
{
    internal sealed class FakeLogger : ILogger
    {
        private readonly IBuildManager _buildManager;

        public LoggerVerbosity Verbosity { get => LoggerVerbosity.Quiet; set { } }

        public string Parameters { get => "VisualStudioLogger=true"; set { } }

        public FakeLogger(IBuildManager buildManager)
        {
            _buildManager = buildManager;
        }

        public void Initialize(IEventSource eventSource)
        {
            eventSource.ProjectStarted += ProjectStarted;
            eventSource.ProjectFinished += ProjectFinished;
        }

        private void ProjectFinished(object sender, ProjectFinishedEventArgs e) => _buildManager.NotifyProjectEnded(e);

        private void ProjectStarted(object sender, ProjectStartedEventArgs e) => _buildManager.NotifyProjectStarted(e);

        public void Shutdown()
        {
        }
    }
}
