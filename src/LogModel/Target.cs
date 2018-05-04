// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class Target : Node
    {
        public int NodeId { get; }

        public string Name { get; }

        public string SourceFilePath { get; }

        public string ParentTarget { get; }

        public TargetBuiltReason Reason { get; }

        public bool IsRequestedTarget { get; }

        public ImmutableList<Item> OutputItems { get; }

        public ImmutableList<ItemAction> ItemActions { get; }

        public ImmutableList<PropertySet> PropertySets { get; }

        public ImmutableList<Task> Tasks { get; }

        public Target(int nodeId, string name, bool isRequestedTarget, string sourceFilePath, string parentTarget, TargetBuiltReason reason, ImmutableList<ItemAction> itemActions, ImmutableList<PropertySet> propertySets, ImmutableList<Item> outputItems, ImmutableList<Task> tasks, DateTime startTime, DateTime endTime, ImmutableList<Message> messages, Result result)
            : base(messages, startTime, endTime, result)
        {
            NodeId = nodeId;
            Name = name;
            IsRequestedTarget = isRequestedTarget;
            SourceFilePath = sourceFilePath;
            ParentTarget = parentTarget;
            Reason = reason;
            ItemActions = itemActions;
            PropertySets = propertySets;
            OutputItems = outputItems;
            Tasks = tasks;
        }
    }
}
