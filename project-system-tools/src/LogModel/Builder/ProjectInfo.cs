// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class ProjectInfo : BaseInfo
    {
        private List<TargetInfo> _executedTargets;
        private Dictionary<int, TargetInfo> _targetInfos;

        public int Id { get; }
        public int NodeId { get; }
        public string Name { get; }
        public int ParentProject { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; private set; }
        public string ProjectFile { get; }
        public ImmutableDictionary<string, string> GlobalProperties { get; }
        public ImmutableDictionary<string, string> Properties { get; }
        public ImmutableList<ItemGroupInfo> ItemGroups { get; }
        public IReadOnlyList<TargetInfo> ExecutedTargets => _executedTargets;
        public Result Result { get; private set; }
        public ImmutableHashSet<string> TargetsToBuild { get; }
        public string ToolsVersion { get; }

        public ProjectInfo(int id, int nodeId, int parentProject, DateTime startTime, ImmutableHashSet<string> targetsToBuild, string toolsVersion, string name, string projectFile, ImmutableDictionary<string, string> globalProperties, ImmutableDictionary<string, string> properties, ImmutableList<ItemGroupInfo> itemGroups)
        {
            Id = id;
            NodeId = nodeId;
            ParentProject = parentProject;
            StartTime = startTime;
            TargetsToBuild = targetsToBuild;
            ToolsVersion = toolsVersion;
            Name = name;
            ProjectFile = projectFile;
            GlobalProperties = globalProperties;
            Properties = properties;
            ItemGroups = itemGroups;
        }

        public void EndProject(DateTime endTime, bool result)
        {
            EndTime = endTime;
            Result = result ? Result.Succeeded : Result.Failed;
        }

        public TargetInfo GetTarget(int id) => 
            _targetInfos == null || !_targetInfos.TryGetValue(id, out var targetInfo)
                ? throw new LoggerException(Resources.CannotFindTarget)
                : targetInfo;

        public void AddTarget(int id, TargetInfo targetInfo)
        {
            if (_targetInfos == null)
            {
                _targetInfos = new Dictionary<int, TargetInfo>();
            }

            if (_targetInfos.ContainsKey(id))
            {
                throw new LoggerException(Resources.DuplicateTarget);
            }

            _targetInfos[id] = targetInfo;
        }

        public void AddExecutedTarget(string name, TargetInfo targetInfo)
        {
            if (_executedTargets == null)
            {
                _executedTargets = new List<TargetInfo>();
            }

            _executedTargets.Add(targetInfo);
        }
    }
}
