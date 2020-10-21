// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    [Export(typeof(IPropertyPageQueryCacheProvider))]
    internal sealed class PropertyPageQueryCacheProvider : IPropertyPageQueryCacheProvider
    {
        public IPropertyPageQueryCache CreateCache(UnconfiguredProject project)
        {
            return new PropertyPageQueryCache(project);
        }
    }
}
