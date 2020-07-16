// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable disable

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor.ViewModel
{
    internal sealed class PropertyViewModel : BaseViewModel
    {
        private readonly string _value;
        private SelectedObjectWrapper _properties;

        public override string Text { get; }

        public override SelectedObjectWrapper Properties => _properties ?? (_properties = 
            new SelectedObjectWrapper(Text, "Property", null,
                new Dictionary<string, IDictionary<string, string>> {
                    {"Property", new Dictionary<string, string>
                        {
                            {"Value", _value}
                        }
                    }
                }));

        public PropertyViewModel(string name, string value)
        {
            Text = name;
            _value = value;
        }
    }
}
