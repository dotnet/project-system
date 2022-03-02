// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal partial class ProjectTreeParser
    {
        private partial class MutableProjectTree
        {
            // Does nothing other than return the "SubType" property
            private class SubTypeRule : IRule
            {
                private readonly MutableProjectTree _tree;

                public SubTypeRule(MutableProjectTree tree)
                {
                    _tree = tree;
                }

                public IPropertyGroup this[string categoryName] => throw new NotImplementedException();

                public string Name => throw new NotImplementedException();
                public string DisplayName => throw new NotImplementedException();
                public string Description => throw new NotImplementedException();
                public string HelpString => throw new NotImplementedException();
                public string PageTemplate => throw new NotImplementedException();
                public string SwitchPrefix => throw new NotImplementedException();
                public string Separator => throw new NotImplementedException();
                public IReadOnlyList<ICategory> Categories => throw new NotImplementedException();
                public Rule Schema => throw new NotImplementedException();
                public int Order => throw new NotImplementedException();
                public string File => throw new NotImplementedException();
                public string ItemType => throw new NotImplementedException();
                public string ItemName => throw new NotImplementedException();
                public IReadOnlyList<IPropertyGroup> PropertyGroups => throw new NotImplementedException();
                public IEnumerable<IProperty> Properties => throw new NotImplementedException();
                public IProjectPropertiesContext Context => throw new NotImplementedException();
                public bool PropertyPagesHidden => throw new NotImplementedException();

                public IProperty GetProperty(string propertyName)
                {
                    throw new NotImplementedException();
                }

                public Task<string> GetPropertyValueAsync(string propertyName)
                {
                    if (propertyName == "SubType")
                    {
                        return Task.FromResult(_tree.SubType ?? "");
                    }

                    throw new NotImplementedException();
                }
            }
        }
    }
}

