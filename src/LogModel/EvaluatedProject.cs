using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class EvaluatedProject : Node
    {
        public string Name { get; }

        public EvaluatedProject(string name, DateTime startTime, DateTime endTime, ImmutableList<Message> messages) :
            base(messages, startTime, endTime, Result.Succeeded)
        {
            Name = name;
        }
    }
}
