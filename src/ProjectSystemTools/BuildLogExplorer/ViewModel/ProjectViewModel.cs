using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal sealed class ProjectViewModel : NodeViewModel
    {
        private readonly Project _project;
        private string _text;
        private List<object> _children;
        private SelectedObjectWrapper _properties;

        protected override Node Node => _project;

        public override string Text => _text ?? (_text = $"{_project.Name}");

        public override IEnumerable<object> Children => _children ?? (_children = GetChildren());

        public override SelectedObjectWrapper Properties => _properties ?? (_properties =
            new SelectedObjectWrapper(
                _project.Name,
                "Project",
                _project.Messages,
                new Dictionary<string, IDictionary<string, string>> {
                    {"Build", new Dictionary<string, string>
                        {
                            {"Node", _project.NodeId.ToString() },
                            {"Project File", _project.ProjectFile },
                            {"Started", _project.StartTime.ToString(CultureInfo.InvariantCulture)},
                            {"Finished", _project.EndTime.ToString(CultureInfo.InvariantCulture)},
                            {"Tools Version", _project.ToolsVersion }
                        }
                     },
                    {"GlobalProperties", _project.GlobalProperties},
                    {"Properties", _project.Properties}
                }));

        public ProjectViewModel(Project project)
        {
            _project = project;
        }

        private List<object> GetChildren()
        {
            var list = new List<object>();

            if (_project.ItemGroups.Any())
            {
                list.Add(new ListViewModel<ItemGroup>("Items", _project.ItemGroups,
                    itemGroup =>
                        new ListViewModel<Item>(itemGroup.Name, itemGroup.Items, item => new ItemViewModel(item))));
            }

            if (_project.Targets.Any())
            {
                list.AddRange(_project.Targets.Select(target => new TargetViewModel(target)));
            }

            return list;
        }
    }
}
