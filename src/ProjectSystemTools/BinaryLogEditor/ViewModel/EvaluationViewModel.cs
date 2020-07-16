// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal sealed class EvaluationViewModel : BaseViewModel
    {
        private readonly Evaluation _evaluation;
        private string _text;
        private SelectedObjectWrapper _properties;
        private IEnumerable<object> _children;

        public override IEnumerable<object> Children => _children ??= GetChildren();

        public override string Text => _text ??= $"Evaluation {_evaluation.EvaluatedProjects.Aggregate(TimeSpan.Zero, (t, p) => t + (p.EndTime - p.StartTime)):mm':'ss'.'ffff}";

        public override SelectedObjectWrapper Properties => _properties ??= new SelectedObjectWrapper("Evaluation", "Evaluation", _evaluation.Messages, null);

        public EvaluationViewModel(Evaluation evaluation)
        {
            _evaluation = evaluation;
        }

        private IEnumerable<object> GetChildren() => _evaluation.EvaluatedProjects.Select(p => new EvaluatedProjectViewModel(p)).ToList<object>();
    }
}
