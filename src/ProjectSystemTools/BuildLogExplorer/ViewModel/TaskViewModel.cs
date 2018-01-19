using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal sealed class TaskViewModel : NodeViewModel
    {
        private readonly Task _task;
        private string _text;
        private List<object> _children;
        private SelectedObjectWrapper _properties;

        protected override Node Node => _task;

        public override string Text => _text ?? (_text = $"Task {_task.Name}");

        public override IEnumerable<object> Children => _children ?? (_children = GetChildren());

        public override SelectedObjectWrapper Properties => _properties ?? (_properties =
            new SelectedObjectWrapper(
                _task.Name,
                "Task",
                _task.Messages,
                new Dictionary<string, IDictionary<string, string>> {
                    {"Build", new Dictionary<string, string>
                        {
                            {"Node", _task.NodeId.ToString() },
                            {"Source File", _task.SourceFilePath },
                            {"Assembly", _task.FromAssembly },
                            {"Command Line", _task.CommandLineArguments },
                            {"Started", _task.StartTime.ToString(CultureInfo.InvariantCulture)},
                            {"Finished", _task.EndTime.ToString(CultureInfo.InvariantCulture)}
                        }
                    }
                }));

        public TaskViewModel(Task task)
        {
            _task = task;
        }

        private List<object> GetChildren()
        {
            var list = new List<object>();

            var parameters = _task.ParameterProperties.Select(property => (BaseViewModel)new PropertyViewModel(property.Key, property.Value))
                .Union(_task.ParameterItems.Select(items => new ListViewModel<Item>(items.Name, items.Items, item => new ItemViewModel(item))))
                .ToList();

            if (parameters.Any())
            {
                list.Add(new ListViewModel<BaseViewModel>("Parameters", parameters, parameter => parameter));
            }

            if (_task.ChildProjects.Any())
            {
                list.AddRange(_task.ChildProjects.Select(project => new ProjectViewModel(project)));
            }

            var outputs = _task.OutputProperties.Select(property => (BaseViewModel)new PropertyViewModel(property.Key, property.Value))
                .Union(_task.OutputItems.Select(items => new ListViewModel<Item>(items.Name, items.Items, item => new ItemViewModel(item))))
                .ToList();

            if (outputs.Any())
            {
                list.Add(new ListViewModel<BaseViewModel>("Outputs", outputs, output => output));
            }

            return list;
        }
    }
}
