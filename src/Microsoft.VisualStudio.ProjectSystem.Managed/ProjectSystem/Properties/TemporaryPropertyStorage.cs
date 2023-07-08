// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export(typeof(ITemporaryPropertyStorage))]
    internal sealed class TemporaryPropertyStorage : ITemporaryPropertyStorage
    {
        private ImmutableDictionary<string, string> _properties = ImmutableDictionary<string, string>.Empty;

        /// <remarks>
        /// We only need <paramref name="project"/> to force the creation of one of these per
        /// <see cref="ConfiguredProject"/>. Otherwise we end up sharing them between
        /// configurations/projects when we don't want to.
        /// </remarks>
        [ImportingConstructor]
        public TemporaryPropertyStorage(ConfiguredProject project)
        {
        }

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
