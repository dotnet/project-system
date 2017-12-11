// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal class MessageInfo
    {
        public DateTime Timestamp { get; }
        public string Text { get; }

        public MessageInfo(string text, DateTime timestamp)
        {
            Text = text;
            Timestamp = timestamp;
        }
    }
}
