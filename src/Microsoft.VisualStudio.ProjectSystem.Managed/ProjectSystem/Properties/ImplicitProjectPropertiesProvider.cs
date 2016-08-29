// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Provides project properties that normally should not live in the project file but may be 
    /// written from an external source (e.g. the solution file). This provider avoids writing 
    /// these properties to the project file, but if they are already present there, it updates 
    /// the value to keep in sync with the external source.
    /// Values that are not written are held in memory so they can be read for the lifetime of this
    /// provider. If the property is changed after loading the project file, the file may be out of 
    /// sync until a full project reload occurs. Specifically, if provider is managing property in 
    /// memory, and a property is added the project file, all operations ignore the value in the 
    /// project file until this property is deleted or a full reload of the project system occurs.
    /// </summary>
    [Export("ImplicitProjectFile", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [ExportMetadata("Name", "ImplicitProjectFile")]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class ImplicitProjectPropertiesProvider : DelegatedProjectPropertiesProviderBase
    {
        private readonly ConcurrentDictionary<string, string> _propertyValues = new ConcurrentDictionary<string, string>();

        [ImportingConstructor]
        public ImplicitProjectPropertiesProvider(
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectPropertiesProvider provider,
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectInstancePropertiesProvider instanceProvider,
            UnconfiguredProject unconfiguredProject)
            : base(provider, instanceProvider, unconfiguredProject)
        {
        }

        public override IProjectProperties GetProperties(string file, string itemType, string item)
            => new ImplicitProjectProperties(DelegatedProvider.GetProperties(file, itemType, item), _propertyValues);

        /// <summary>
        /// Implementation of IProjectProperties that avoids writing properties unless they
        /// already exist (i.e. are being updated) and delegates the rest of its operations
        /// to another IProjectProperties object
        /// </summary>
        private class ImplicitProjectProperties : DelegatedProjectPropertiesBase
        {
            private readonly ConcurrentDictionary<string, string> _propertyValues;

            public ImplicitProjectProperties(IProjectProperties properties, ConcurrentDictionary<string, string> propertyValues)
                : base(properties)
            {
                _propertyValues = propertyValues;
            }

            /// <summary>
            /// If a property exists in the delegated properties object, then pass the set
            /// through (overwrite). Otherwise manage the value in memory in this properties 
            /// object.
            /// </summary>
            public override async Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string> dimensionalConditions = null)
            {
                var propertyNames = await DelegatedProperties.GetPropertyNamesAsync().ConfigureAwait(false);
                if (propertyNames.Contains(propertyName))
                {
                    // overwrite the property if it exists
                    await DelegatedProperties.SetPropertyValueAsync(propertyName, unevaluatedPropertyValue, dimensionalConditions).ConfigureAwait(false);
                }
                else
                {
                    // store the property in this property object, not in the project file
                    _propertyValues[propertyName] = unevaluatedPropertyValue;
                }
            }

            /// <summary>
            /// If the property name is one that is implicitly managed here, remove it from
            /// the value map. Otherwise delegate this request to the backing property.
            /// </summary>
            public override Task DeletePropertyAsync(string propertyName, IReadOnlyDictionary<string, string> dimensionalConditions = null)
            {
                string unevaluatedPropertyValue;
                if (_propertyValues.TryRemove(propertyName, out unevaluatedPropertyValue))
                {
                    return Task.CompletedTask;
                }
                return DelegatedProperties.DeletePropertyAsync(propertyName, dimensionalConditions);
            }

            /// <summary>
            /// If the property name is one that is implicitly managed here, return the unevaluated value.
            /// Otherwise delegate this request to the backing property.
            /// </summary>
            public override Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
            {
                string unevaluatedPropertyValue;
                if (_propertyValues.TryGetValue(propertyName, out unevaluatedPropertyValue))
                {
                    return Task.FromResult(unevaluatedPropertyValue);
                }
                return DelegatedProperties.GetEvaluatedPropertyValueAsync(propertyName);
            }

            /// <summary>
            /// If the property name is one that is implicitly managed here, return that.
            /// Otherwise delegate this request to the backing property.
            /// </summary>
            public override Task<string> GetUnevaluatedPropertyValueAsync(string propertyName)
            {
                string unevaluatedPropertyValue;
                if (_propertyValues.TryGetValue(propertyName, out unevaluatedPropertyValue))
                {
                    return Task.FromResult(unevaluatedPropertyValue);
                }
                return DelegatedProperties.GetUnevaluatedPropertyValueAsync(propertyName);
            }
        }
    }
}
