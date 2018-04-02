// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal sealed class EvaluatedProjectViewModel : NodeViewModel
    {
        private readonly Evaluation _evaluation;
        private readonly EvaluatedProject _evaluatedProject;
        private string _text;
        private SelectedObjectWrapper _properties;

        public override string Text => _text ?? (_text = _evaluatedProject.Name);

        protected override Node Node => _evaluatedProject;

        public override SelectedObjectWrapper Properties => _properties ?? (_properties =
            new SelectedObjectWrapper(
                _evaluatedProject.Name,
                "Evaluated Project",
                _evaluation?.Messages.Union(_evaluatedProject.Messages) ?? _evaluatedProject.Messages,
                new Dictionary<string, IDictionary<string, string>> {
                    {"Build", new Dictionary<string, string>
                        {
                            {"Started", _evaluatedProject.StartTime.ToString(CultureInfo.InvariantCulture)},
                            {"Finished", _evaluatedProject.EndTime.ToString(CultureInfo.InvariantCulture)}
                        }
                     }
                }));

        public EvaluatedProjectViewModel(EvaluatedProject evaluatedProject)
        {
            _evaluatedProject = evaluatedProject;
        }

        public EvaluatedProjectViewModel(Evaluation evaluation)
        {
            _evaluatedProject = evaluation.EvaluatedProjects[0];
            _evaluation = evaluation;
        }
    }
}
