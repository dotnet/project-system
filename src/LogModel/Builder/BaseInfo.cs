using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal abstract class BaseInfo
    {
        private List<MessageInfo> _messages;

        public IEnumerable<MessageInfo> Messages => _messages;

        public void AddMessage(MessageInfo message)
        {
            if (_messages == null)
            {
                _messages = new List<MessageInfo>();
            }

            _messages.Add(message);
        }

        public void AddMessage(string message, DateTime timestamp) =>
            AddMessage(new MessageInfo(message, timestamp));
    }
}
