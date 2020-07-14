// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class TaskInfo : BaseInfo
    {
        private List<ItemGroupInfo> _parameterItems;
        private Dictionary<string, string> _parameterProperties;
        private List<ItemGroupInfo> _outputItems;
        private Dictionary<string, string> _outputProperties;
        private List<ProjectInfo> _childProjectInfos;

        public int Id { get; }
        public int NodeId { get; }
        public string Name { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; private set; }
        public string FromAssembly { get; }
        public string SourceFilePath { get; }
        public Result Result { get; private set; }
        public string CommandLineArguments { get; private set; }
        public IReadOnlyList<ProjectInfo> ChildProjectInfos => _childProjectInfos;
        public IReadOnlyList<ItemGroupInfo> ParameterItems => _parameterItems;
        public IReadOnlyDictionary<string, string> ParameterProperties => _parameterProperties;
        public IReadOnlyList<ItemGroupInfo> OutputItems => _outputItems;
        public IReadOnlyDictionary<string, string> OutputProperties => _outputProperties;

        public TaskInfo(int id, int nodeId, string name, DateTime startTime, string fromAssembly, string sourceFilePath)
        {
            Id = id;
            NodeId = nodeId;
            Name = name;
            StartTime = startTime;
            FromAssembly = fromAssembly;
            SourceFilePath = sourceFilePath;
        }

        public TaskInfo(string name, DateTime time)
        {
            Name = name;
            StartTime = time;
            EndTime = time;
            Result = Result.Skipped;
        }

        public void SetCommandLineArguments(string arguments)
        {
            CommandLineArguments = arguments;
        }

        public void AddParameterItems(ItemGroupInfo parameterItems)
        {
            if (_parameterItems == null)
            {
                _parameterItems = new List<ItemGroupInfo>();
            }

            _parameterItems.Add(parameterItems);
        }

        public void AddParameterProperty(string name, string value)
        {
            if (_parameterProperties == null)
            {
                _parameterProperties = new Dictionary<string, string>();
            }

            if (_parameterProperties.ContainsKey(name))
            {
                throw new LoggerException(Resources.OverwritingProperty);
            }

            _parameterProperties[name] = value;
        }

        public void AddOutputItems(ItemGroupInfo outputItems)
        {
            if (_outputItems == null)
            {
                _outputItems = new List<ItemGroupInfo>();
            }

            _outputItems.Add(outputItems);
        }

        public void AddOutputProperty(string name, string value)
        {
            if (_outputProperties == null)
            {
                _outputProperties = new Dictionary<string, string>();
            }

            if (_outputProperties.ContainsKey(name))
            {
                throw new LoggerException(Resources.OverwritingProperty);
            }

            _outputProperties[name] = value;
        }

        public IEnumerable<string> GetTaskParameter(string name)
        {
            var values = ParameterItems?.SingleOrDefault(itemGroup => itemGroup.Name == name)?.Items.Select(item => item.Name);

            if (values == null)
            {
                var value = ParameterProperties?.SingleOrDefault(property => property.Key == name);
                if (string.IsNullOrEmpty(value?.Key))
                {
                    return Enumerable.Empty<string>();
                }

                return new[] { value.Value.Value };
            }

            return values;
        }

        public void AddChildProject(ProjectInfo childProject)
        {
            if (_childProjectInfos == null)
            {
                _childProjectInfos = new List<ProjectInfo>();
            }

            _childProjectInfos.Add(childProject);
        }

        public void FinishTask(DateTime endTime, bool result)
        {
            EndTime = endTime;
            Result = result ? Result.Succeeded : Result.Failed;
        }
    }
}
