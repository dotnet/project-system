// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class TargetInfo : BaseInfo
    {
        private List<ItemActionInfo> _itemActionInfos;
        private List<PropertySetInfo> _propertySetInfos;
        private List<TaskInfo> _executedTasks;
        private Dictionary<int, TaskInfo> _taskInfos;

        public int Id { get; }
        public int NodeId { get; }
        public string Name { get; }
        public string SourceFilePath { get; }
        public string ParentTarget { get; }
        public TargetBuiltReason Reason { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; private set; }
        public Result Result { get; private set; }
        public ImmutableList<ItemInfo> OutputItems { get; private set; }
        public bool IsRequestedTarget { get; private set; }
        public IReadOnlyList<ItemActionInfo> ItemActionInfos => _itemActionInfos;
        public IReadOnlyList<PropertySetInfo> PropertySetInfos => _propertySetInfos;
        public IReadOnlyDictionary<int, TaskInfo> TaskInfos => _taskInfos;

        public TargetInfo(int id, int nodeId, string name, string sourceFilePath, string parentTarget, TargetBuiltReason reason, DateTime startTime)
        {
            Id = id;
            NodeId = nodeId;
            Name = name;
            SourceFilePath = sourceFilePath;
            ParentTarget = parentTarget;
            Reason = reason;
            StartTime = startTime;
        }

        public TargetInfo(string name, DateTime startTime)
        {
            Id = BuildEventContext.InvalidTargetId;
            NodeId = BuildEventContext.InvalidNodeId;
            Name = name;
            StartTime = startTime;
            EndTime = startTime;
            Result = Result.Skipped;
        }

        public TargetInfo(string name, string sourceFilePath, string parentTarget, TargetBuiltReason reason, DateTime startTime)
        {
            Id = BuildEventContext.InvalidTargetId;
            NodeId = BuildEventContext.InvalidNodeId;
            Name = name;
            SourceFilePath = sourceFilePath;
            ParentTarget = parentTarget;
            Reason = reason;
            StartTime = startTime;
            EndTime = startTime;
            Result = Result.Skipped;
        }

        public void SetIsRequestedTarget()
        {
            IsRequestedTarget = true;
        }

        public void AddItemAction(ItemActionInfo itemActionInfo)
        {
            if (_itemActionInfos == null)
            {
                _itemActionInfos = new List<ItemActionInfo>();
            }

            _itemActionInfos.Add(itemActionInfo);
        }

        public void AddTask(int id, TaskInfo taskInfo)
        {
            if (_taskInfos == null)
            {
                _taskInfos = new Dictionary<int, TaskInfo>();
            }

            if (_executedTasks == null)
            {
                _executedTasks = new List<TaskInfo>();
            }

            if (_taskInfos.ContainsKey(id))
            {
                throw new LoggerException(Resources.DuplicateTask);
            }

            _taskInfos[id] = taskInfo;
            _executedTasks.Add(taskInfo);
        }

        public void AddExecutedTask(TaskInfo taskInfo)
        {
            if (_executedTasks == null)
            {
                _executedTasks = new List<TaskInfo>();
            }

            _executedTasks.Add(taskInfo);
        }

        public void AddPropertySet(PropertySetInfo propertySetInfo)
        {
            if (_propertySetInfos == null)
            {
                _propertySetInfos = new List<PropertySetInfo>();
            }

            _propertySetInfos.Add(propertySetInfo);
        }

        public void EndTarget(DateTime endTime, bool result, ImmutableList<ItemInfo> outputItems)
        {
            EndTime = endTime;
            Result = result ? Result.Succeeded : Result.Failed;
            OutputItems = outputItems;
        }
    }
}
