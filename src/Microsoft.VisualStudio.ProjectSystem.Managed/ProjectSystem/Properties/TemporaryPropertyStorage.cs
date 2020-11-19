// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export(typeof(ITemporaryPropertyStorage))]
    internal sealed class TemporaryPropertyStorage : ITemporaryPropertyStorage
    {
        private ImmutableDictionary<string, string> _properties = ImmutableDictionary<string, string>.Empty;

        public void AddOrUpdatePropertyValue(string propertyName, string propertyValue)
        {
            ImmutableInterlocked.AddOrUpdate(ref _properties, propertyName, propertyValue, (_, _) => propertyValue);
        }

        public string? GetPropertyValue(string propertyName)
        {
            return _properties.TryGetValue(propertyName, out string? value) ? value : null;
        }
    }
}
