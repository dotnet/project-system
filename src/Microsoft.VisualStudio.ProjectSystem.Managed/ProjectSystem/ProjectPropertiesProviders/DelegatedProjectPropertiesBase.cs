// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectPropertiesProviders
{
    /// <summary>
    /// An implementation of IProjectProperties that delegates its operations
    /// to another IProjectProperties object
    /// </summary>
    internal class DelegatedProjectPropertiesBase : IProjectProperties
    {
        protected internal IProjectProperties DelegatedProperties;

        public DelegatedProjectPropertiesBase(IProjectProperties properties)
        {
            DelegatedProperties = properties;
        }

        /// <summary>
        /// <see cref="IProjectProperties.Context"/>
        /// </summary>
        public virtual IProjectPropertiesContext Context => DelegatedProperties.Context;

        /// <summary>
        /// <see cref="IProjectProperties.FileFullPath"/>
        /// </summary>
        public virtual string FileFullPath => DelegatedProperties.FileFullPath;

        /// <summary>
        /// <see cref="IProjectProperties.PropertyKind"/>
        /// </summary>
        public virtual PropertyKind PropertyKind => DelegatedProperties.PropertyKind;

        /// <summary>
        /// <see cref="IProjectProperties.DeleteDirectPropertiesAsync"/>
        /// </summary>
        public virtual Task DeleteDirectPropertiesAsync()
            => DelegatedProperties.DeleteDirectPropertiesAsync();

        /// <summary>
        /// <see cref="IProjectProperties.DeletePropertyAsync"/>
        /// </summary>
        public virtual Task DeletePropertyAsync(string propertyName, IReadOnlyDictionary<string, string> dimensionalConditions = null)
            => DelegatedProperties.DeletePropertyAsync(propertyName, dimensionalConditions);

        /// <summary>
        /// <see cref="IProjectProperties.GetDirectPropertyNamesAsync"/>
        /// </summary>
        public virtual Task<IEnumerable<string>> GetDirectPropertyNamesAsync()
            => DelegatedProperties.GetDirectPropertyNamesAsync();

        /// <summary>
        /// <see cref="IProjectProperties.GetEvaluatedPropertyValueAsync"/>
        /// </summary>
        public virtual Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
            => DelegatedProperties.GetEvaluatedPropertyValueAsync(propertyName);

        /// <summary>
        /// <see cref="IProjectProperties.GetPropertyNamesAsync"/>
        /// </summary>
        public virtual Task<IEnumerable<string>> GetPropertyNamesAsync()
            => DelegatedProperties.GetPropertyNamesAsync();

        /// <summary>
        /// <see cref="IProjectProperties.GetUnevaluatedPropertyValueAsync"/>
        /// </summary>
        public virtual Task<string> GetUnevaluatedPropertyValueAsync(string propertyName)
            => DelegatedProperties.GetUnevaluatedPropertyValueAsync(propertyName);

        /// <summary>
        /// <see cref="IProjectProperties.IsValueInheritedAsync"/>
        /// </summary>
        public virtual Task<bool> IsValueInheritedAsync(string propertyName)
            => DelegatedProperties.IsValueInheritedAsync(propertyName);

        /// <summary>
        /// <see cref="IProjectProperties.SetPropertyValueAsync"/>
        /// </summary>
        public virtual Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string> dimensionalConditions = null)
            => DelegatedProperties.SetPropertyValueAsync(propertyName, unevaluatedPropertyValue, dimensionalConditions);
    }
}