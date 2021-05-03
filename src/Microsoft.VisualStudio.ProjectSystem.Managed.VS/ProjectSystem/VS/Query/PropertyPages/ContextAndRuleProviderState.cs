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
        public ContextAndRuleProviderState(IPropertyPageQueryCache cache, QueryProjectPropertiesContext context, Rule rule)
        {
            Cache = cache;
            Context = context;
            Rule = rule;
        }

        public IPropertyPageQueryCache Cache { get; }
        public QueryProjectPropertiesContext Context { get; }
        public Rule Rule { get; }
    }
}
