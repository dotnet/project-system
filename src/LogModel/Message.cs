// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
