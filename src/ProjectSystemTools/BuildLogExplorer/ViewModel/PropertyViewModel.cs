using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal sealed class PropertyViewModel : BaseViewModel
    {
        private readonly string _name;
        private readonly string _value;
        private SelectedObjectWrapper _properties;

        public override string Text => _name;

        public override SelectedObjectWrapper Properties => _properties ?? (_properties = 
            new SelectedObjectWrapper(_name, "Property", null,
                new Dictionary<string, IDictionary<string, string>> {
                    {"Property", new Dictionary<string, string>
                        {
                            {"Value", _value}
                        }
                    }
                }));

        public PropertyViewModel(string name, string value)
        {
            _name = name;
            _value = value;
        }
    }
}
