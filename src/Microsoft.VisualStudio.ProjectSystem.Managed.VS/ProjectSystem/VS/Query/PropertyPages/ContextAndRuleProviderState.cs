// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Hold a <see cref="Rule"/> and the context in which to bind it. Used by 
    /// <see cref="LaunchProfileDataProducer"/> and <see cref="PropertyPageDataProducer"/>
    /// to pass the state their child producers will need, but allows the actual binding
    /// of the <see cref="Rule"/> to be delayed until needed.
    /// </summary>
    internal sealed class ContextAndRuleProviderState
    {
        public ContextAndRuleProviderState(IProjectState projectState, QueryProjectPropertiesContext propertiesContext, Rule rule)
        {
            ProjectState = projectState;
            PropertiesContext = propertiesContext;
            Rule = rule;
        }

        public IProjectState ProjectState { get; }
        public QueryProjectPropertiesContext PropertiesContext { get; }
        public Rule Rule { get; }
    }
}
