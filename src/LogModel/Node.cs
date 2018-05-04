// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public abstract class Node
    {
        public DateTime StartTime { get; }
        public DateTime EndTime { get; }

        public ImmutableList<Message> Messages { get; }

        public Result Result { get; }

        protected Node(ImmutableList<Message> messages, DateTime startTime, DateTime endTime, Result result)
        {
            Messages = messages;
            StartTime = startTime;
            EndTime = endTime;
            Result = result;
        }
    }
}
