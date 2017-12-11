using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class Target : Node
    {
        public int NodeId { get; }

        public string Name { get; }

        public string SourceFilePath { get; }

        public string ParentTarget { get; }

        public bool IsRequestedTarget { get; }

        public ImmutableList<Item> OutputItems { get; }

        public ImmutableList<ItemAction> ItemActions { get; }

        public ImmutableList<PropertySet> PropertySets { get; }

        public ImmutableList<Task> Tasks { get; }

        public Target(int nodeId, string name, bool isRequestedTarget, string sourceFilePath, string parentTarget, ImmutableList<ItemAction> itemActions, ImmutableList<PropertySet> propertySets, ImmutableList<Item> outputItems, ImmutableList<Task> tasks, DateTime startTime, DateTime endTime, ImmutableList<Message> messages, Result result)
            : base(messages, startTime, endTime, result)
        {
            NodeId = nodeId;
            Name = name;
            IsRequestedTarget = isRequestedTarget;
            SourceFilePath = sourceFilePath;
            ParentTarget = parentTarget;
            ItemActions = itemActions;
            PropertySets = propertySets;
            OutputItems = outputItems;
            Tasks = tasks;
        }
    }
}
