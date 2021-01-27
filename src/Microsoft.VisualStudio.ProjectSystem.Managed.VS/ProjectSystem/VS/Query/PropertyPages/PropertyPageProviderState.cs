// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Holds the state we need to pass from producers of <see cref="PropertyPageValue"/> instances
    /// to other producers that will create the <see cref="PropertyPageValue"/>s' child entities.
    /// </summary>
    internal sealed class PropertyPageProviderState
    {
        public PropertyPageProviderState(IPropertyPageQueryCache cache, Rule rule)
            : this(cache, rule, new List<Rule>(capacity: 0))
        {
        }

        public PropertyPageProviderState(IPropertyPageQueryCache cache, Rule rule, List<Rule> debugChildRules)
        {
            Cache = cache;
            Rule = rule;
            DebugChildRules = debugChildRules;
        }

        public IPropertyPageQueryCache Cache { get; }
        public Rule Rule { get; }
        public List<Rule> DebugChildRules { get; }
    }
}
