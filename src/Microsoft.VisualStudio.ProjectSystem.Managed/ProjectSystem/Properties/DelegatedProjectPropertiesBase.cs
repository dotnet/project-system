// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// An implementation of IProjectProperties that delegates its operations
    /// to another IProjectProperties object
    /// </summary>
    internal class DelegatedProjectPropertiesBase : IProjectProperties
    {
        protected readonly IProjectProperties DelegatedProperties;

        protected DelegatedProjectPropertiesBase(IProjectProperties properties)
        {
            Requires.NotNull(properties, nameof(properties));

            DelegatedProperties = properties;
        }

        public virtual IProjectPropertiesContext Context => DelegatedProperties.Context;

        public virtual string FileFullPath => DelegatedProperties.FileFullPath;

        public virtual PropertyKind PropertyKind => DelegatedProperties.PropertyKind;

        public virtual Task DeleteDirectPropertiesAsync()
            => DelegatedProperties.DeleteDirectPropertiesAsync();

        public virtual Task DeletePropertyAsync(string propertyName, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
            => DelegatedProperties.DeletePropertyAsync(propertyName, dimensionalConditions);

        public virtual Task<IEnumerable<string>> GetDirectPropertyNamesAsync()
            => DelegatedProperties.GetDirectPropertyNamesAsync();

        public virtual Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
            => DelegatedProperties.GetEvaluatedPropertyValueAsync(propertyName);

        public virtual Task<IEnumerable<string>> GetPropertyNamesAsync()
            => DelegatedProperties.GetPropertyNamesAsync();

        public virtual Task<string?> GetUnevaluatedPropertyValueAsync(string propertyName)
            => DelegatedProperties.GetUnevaluatedPropertyValueAsync(propertyName);

        public virtual Task<bool> IsValueInheritedAsync(string propertyName)
            => DelegatedProperties.IsValueInheritedAsync(propertyName);

        public virtual Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
            => DelegatedProperties.SetPropertyValueAsync(propertyName, unevaluatedPropertyValue, dimensionalConditions);
    }
}
