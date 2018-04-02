// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal sealed class LogViewModel : BaseViewModel
    {
        private readonly string _filename;
        private readonly Log _log;
        private readonly IEnumerable<Exception> _exceptions;
        private string _text;
        private List<object> _children;
        private SelectedObjectWrapper _properties;

        public override string Text => _text ?? (_text = Path.GetFileNameWithoutExtension(_filename));

        public override IEnumerable<object> Children => _children ?? (_children = GetChildren());

        public override SelectedObjectWrapper Properties => _properties ?? (_properties =
            new SelectedObjectWrapper(
                Text,
                "Log",
                null,
                new Dictionary<string, IDictionary<string, string>> { {"Build", new Dictionary<string, string> { { "Filename", _filename } } }}));

        public LogViewModel(string filename, Log log)
        {
            _filename = filename;
            _log = log;
        }

        public LogViewModel(string filename, IEnumerable<Exception> exceptions)
        {
            _filename = filename;
            _exceptions = exceptions;
        }

        private List<object> GetChildren()
        {
            var list = new List<object>();

            if (_log != null)
            {
                if (_log?.Evaluations.Any() == true)
                {
                    list.Add(new ListViewModel<Evaluation>("Evaluations", _log.Evaluations, e => e.EvaluatedProjects.Count == 1 ? (BaseViewModel)new EvaluatedProjectViewModel(e) : new EvaluationViewModel(e)));
                }

                if (_log.Build?.Project != null)
                {
                    list.Add(new BuildViewModel(_log.Build));
                }
            }

            if (_exceptions != null)
            {
                list.Add(new ListViewModel<Exception>("Exceptions", _exceptions, ex => new ExceptionViewModel(ex)));
            }

            return list;
        }
    }
}
