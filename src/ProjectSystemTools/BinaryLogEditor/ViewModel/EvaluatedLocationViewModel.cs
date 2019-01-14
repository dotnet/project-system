// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework.Profiler;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal sealed class EvaluatedLocationViewModel : BaseViewModel
    {
        private readonly EvaluatedLocation _evaluatedLocation;
        private string _text;
        private List<object> _children;
        private SelectedObjectWrapper _properties;

        public override string Text => _text ?? (_text = GetTextOfLocation());

        public override IEnumerable<object> Children => _children ?? (_children = GetChildren());

        public override SelectedObjectWrapper Properties => _properties ?? (_properties =
            new SelectedObjectWrapper(
                _evaluatedLocation.ElementName ?? string.Empty,
                _evaluatedLocation.Kind.ToString(),
                null,
                new Dictionary<string, IDictionary<string, string>> {
                    {"General", new Dictionary<string, string>
                        {
                            {"Description", _evaluatedLocation.ElementDescription},
                            {"File", _evaluatedLocation.File }
                        }
                     }
                }));

        public EvaluatedLocationViewModel(EvaluatedLocation evaluatedLocation)
        {
            _evaluatedLocation = evaluatedLocation;
        }

        private string GetTextOfLocation()
        {
            string description = null;

            switch (_evaluatedLocation.Kind)
            {
                case EvaluationLocationKind.Element:
                    if (_evaluatedLocation.ElementName == null)
                    {
                        description = Path.GetFileName(_evaluatedLocation.File);
                    }
                    else
                    {
                        description = $"<{_evaluatedLocation.ElementName}>";
                    }
                    break;

                case EvaluationLocationKind.Condition:
                    description = _evaluatedLocation.ElementDescription;
                    break;

                case EvaluationLocationKind.Glob:
                    description = _evaluatedLocation.ElementDescription;
                    break;
            }

            if (description == null)
            {
                description = $"{_evaluatedLocation.File}";
            }

            return $"{description} {(_evaluatedLocation.Line == null ? string.Empty : $" Line {_evaluatedLocation.Line}")} [{FormatTime(_evaluatedLocation.Time)}]";
        }

        private List<object> GetChildren() => ((IEnumerable<object>)_evaluatedLocation.Children.Select(location => new EvaluatedLocationViewModel(location))).ToList();
    }
}
