using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public abstract class Node
    {
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }
        public TimeSpan Duration => EndTime - StartTime;

        public ImmutableList<Message> Messages { get; }

        public Result Result { get; }

        protected Node(ImmutableList<Message> messages, DateTime startTime, DateTime endTime, Result result)
        {
            Messages = messages;
            StartTime = startTime;
            EndTime = endTime;
            Result = result;
        }

        public string DurationText
        {
            get
            {
                var result = Duration.ToString(@"s\.fff");
                return result == "0.000" ? "" : $" ({result}s)";
            }
        }
    }
}
