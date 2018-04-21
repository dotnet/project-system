// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal sealed class EvaluationPassViewModel : BaseViewModel
    {
        private readonly EvaluatedPass _evaluatedPass;
        private string _text;
        //private List<object> _children;
        //private SelectedObjectWrapper _properties;

        public override string Text => _text ?? (_text = _evaluatedPass.Description);

        //public override IEnumerable<object> Children => _children ?? (_children = GetChildren());

        //public override SelectedObjectWrapper Properties => _properties ?? (_properties =
        //    new SelectedObjectWrapper(
        //        _evaluatedProject.Name,
        //        "Evaluated Project",
        //        _evaluation?.Messages.Union(_evaluatedProject.Messages) ?? _evaluatedProject.Messages,
        //        new Dictionary<string, IDictionary<string, string>> {
        //            {"Build", new Dictionary<string, string>
        //                {
        //                    {"Started", _evaluatedProject.StartTime.ToString(CultureInfo.InvariantCulture)},
        //                    {"Finished", _evaluatedProject.EndTime.ToString(CultureInfo.InvariantCulture)}
        //                }
        //             }
        //        }));

        public EvaluationPassViewModel(EvaluatedPass evaluatedPass)
        {
            _evaluatedPass = evaluatedPass;
        }

        //private List<object> GetChildren()
        //{
        //    var list = new List<object>();

        //    if (_evaluatedProject.EvaluationProfile != null)
        //    {
        //        // TODO: Summary
        //        list.AddRange(_evaluatedProject.EvaluationProfile.Passes.Select(pass => new NodeViewModel()));
        //        list.Add(new ListViewModel<ItemGroup>("Items", _project.ItemGroups,
        //            itemGroup =>
        //                new ListViewModel<Item>(itemGroup.Name, itemGroup.Items, item => new ItemViewModel(item))));
        //    }

        //    if (_project.Targets.Any())
        //    {
        //        list.AddRange(_project.Targets.Select(target => new TargetViewModel(target)));
        //    }

        //    return list;
        //}
    }
}
