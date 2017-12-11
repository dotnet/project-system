using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class Build : Node
    {
        public Project Project { get; }

        public ImmutableDictionary<string, string> Environment { get; }

        public Build(Project project, ImmutableDictionary<string, string> environment, ImmutableList<Message> messages, DateTime startTime, DateTime endTime, Result result)
            : base(messages, startTime, endTime, result)
        {
            Project = project;
            Environment = environment;
        }
    }
}
