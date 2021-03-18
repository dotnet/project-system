// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
        public PropertyPageProviderState(IPropertyPageQueryCache cache, QueryProjectPropertiesContext context, Rule rule)
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
