using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class Evaluation
    {
        public ImmutableList<Message> Messages { get; }

        public ImmutableList<EvaluatedProject> EvaluatedProjects { get; }

        public Evaluation(ImmutableList<Message> messages, ImmutableList<EvaluatedProject> evaluatedProjects)
        {
            Messages = messages;
            EvaluatedProjects = evaluatedProjects;
        }
    }
}
