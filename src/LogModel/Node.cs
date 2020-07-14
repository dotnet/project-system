// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
