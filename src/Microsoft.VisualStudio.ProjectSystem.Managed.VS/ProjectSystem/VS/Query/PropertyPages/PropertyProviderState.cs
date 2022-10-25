// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Holds the state we need to pass from producers of <see cref="UIPropertyValueSnapshot"/> instances
    /// to other producers that will create the <see cref="UIPropertyValueSnapshot"/>s' child entities.
    /// </summary>
    internal sealed class PropertyProviderState
    {
        public PropertyProviderState(IProjectState projectState, Rule containingRule, QueryProjectPropertiesContext propertiesContext, string propertyName)
        {
            ProjectState = projectState;
            ContainingRule = containingRule;
            PropertiesContext = propertiesContext;
            PropertyName = propertyName;
        }

        public IProjectState ProjectState { get; }
        public Rule ContainingRule { get; }
        public QueryProjectPropertiesContext PropertiesContext { get; }
        public string PropertyName { get; }
    }
}
