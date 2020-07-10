using System;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public class Message
    {
        public DateTime Timestamp { get; }
        public string Text { get; }

        public Message(DateTime timestamp, string text)
        {
            Timestamp = timestamp;
            Text = text;
        }
    }
}
