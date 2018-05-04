// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal sealed class TargetViewModel : NodeViewModel
    {
        private readonly Target _target;
        private string _text;
        private List<object> _children;
        private SelectedObjectWrapper _properties;

        protected override Node Node => _target;

        public override bool IsPrimary => _target.IsRequestedTarget;

        public override string Text => _text ?? (_text = $"Target {_target.Name}");

        public override IEnumerable<object> Children => _children ?? (_children = GetChildren());

        public override SelectedObjectWrapper Properties => _properties ?? (_properties =
            new SelectedObjectWrapper(
                _target.Name,
                "Target",
                _target.Messages,
                new Dictionary<string, IDictionary<string, string>> {
                    {"Build", new Dictionary<string, string>
                        {
                            {"Node", _target.NodeId.ToString() },
                            {"Source File", _target.SourceFilePath },
                            {"Parent", _target.ParentTarget },
                            {"Started", _target.StartTime.ToString(CultureInfo.InvariantCulture)},
                            {"Finished", _target.EndTime.ToString(CultureInfo.InvariantCulture)},
                            {"Reason", _target.Reason.ToString() }
                        }
                    }
                }));

        public TargetViewModel(Target target)
        {
            _target = target;
        }

        private List<object> GetChildren()
        {
            var list = new List<object>();

            if (_target.OutputItems.Any())
            {
                list.Add(new ListViewModel<Item>("Output Items", _target.OutputItems, item => new ItemViewModel(item)));
            }

            var allActions =
                _target.ItemActions.Select(action =>
                        Tuple.Create((BaseViewModel) new ListViewModel<Item>(
                            action.IsAddition ? $"Add {action.ItemGroup.Name}" : $"Remove {action.ItemGroup.Name}",
                            action.ItemGroup.Items, item => new ItemViewModel(item)), action.Time))
                    .Union(_target.PropertySets.Select(propertySet =>
                        Tuple.Create((BaseViewModel) new PropertyViewModel($"Set Property {propertySet.Name}", propertySet.Value), propertySet.Time)))
                    .Union(_target.Tasks.Select(task => Tuple.Create((BaseViewModel) new TaskViewModel(task), task.StartTime)))
                    .OrderBy(pair => pair.Item2)
                    .Select(pair => pair.Item1)
                    .ToList();

            if (allActions.Any())
            {
                list.AddRange(allActions);
            }

            return list;
        }
    }
}
