using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal sealed class BuildViewModel : NodeViewModel
    {
        private readonly LogModel.Build _build;
        private string _text;
        private SelectedObjectWrapper _properties;
        private IEnumerable<object> _children;

        public override IEnumerable<object> Children => _children ?? (_children = GetChildren());

        protected override Node Node => _build;

        public override string Text => _text ?? (_text = "Build");

        public override SelectedObjectWrapper Properties => _properties ?? (_properties =
            new SelectedObjectWrapper(
                "Build",
                "Build",
                _build.Messages,
                new Dictionary<string, IDictionary<string, string>> {
                    {"Build", new Dictionary<string, string>
                        {
                            {"Started", _build.StartTime.ToString(CultureInfo.InvariantCulture)},
                            {"Finished", _build.EndTime.ToString(CultureInfo.InvariantCulture)}
                        }
                    },
                    {"Environment", _build.Environment}}));

        public BuildViewModel(LogModel.Build build)
        {
            _build = build;
        }

        private List<object> GetChildren()
        {
            var list = new List<object>();

            //if (_build.Evaluations.Any())
            //{
            //    list.Add(new ListViewModel<Evaluation>("Evaluations", _build.Evaluations, e => e.EvaluatedProjects.Count == 1 ? (BaseViewModel)new EvaluatedProjectViewModel(e) : new EvaluationViewModel(e)));
            //}

            list.Add(new ProjectViewModel(_build.Project));

            return list;
        }
    }
}
