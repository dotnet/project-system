using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class Log
    {
        public Build Build { get; }

        public ImmutableList<Evaluation> Evaluations { get; }

        public Log(Build build, ImmutableList<Evaluation> evaluations)
        {
            Build = build;
            Evaluations = evaluations;
        }
    }
}
