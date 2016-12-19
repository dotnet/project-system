// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// This class represents assembly attribute properties that are stored in the project file OR the source code of the project.
    /// </summary>
    internal class AssemblyInfoProperties : DelegatedProjectPropertiesBase
    {
        private readonly ProjectProperties _ruleBasedProperties;
        private readonly ImmutableDictionary<string, SourceAssemblyAttributePropertyValueProvider> _attributeValueProviderMap;

        private static readonly ImmutableDictionary<string, string> s_attributeNameMap = new Dictionary<string, string>
        {
            { "Description",           "System.Reflection.AssemblyDescriptionAttribute" },
            { "AssemblyCompany",       "System.Reflection.AssemblyCompanyAttribute" },
            { "Product",               "System.Reflection.AssemblyProductAttribute" },
            { "Copyright",             "System.Reflection.AssemblyCopyrightAttribute" },
            { "AssemblyVersion",       "System.Reflection.AssemblyVersionAttribute" },
            { "PackageVersion",        "System.Reflection.AssemblyInformationalVersionAttribute" },
            { "FileVersion",           "System.Reflection.AssemblyFileVersionAttribute" },
            { "NeutralLanguage",       "System.Resources.NeutralResourcesLanguageAttribute" }
        }.ToImmutableDictionary();

        public AssemblyInfoProperties(
            IProjectProperties delegatedProjectProperties,
            ProjectProperties ruleBasedProperties,
            Func<ProjectId> getActiveProjectId,
            Workspace workspace,
            IProjectThreadingService threadingService)
            : base (delegatedProjectProperties)
        {
            _ruleBasedProperties = ruleBasedProperties;
            _attributeValueProviderMap = CreateAttributeValueProviderMap(getActiveProjectId, workspace, threadingService);
        }

        private static ImmutableDictionary<string, SourceAssemblyAttributePropertyValueProvider> CreateAttributeValueProviderMap(
            Func<ProjectId> getActiveProjectId,
            Workspace workspace,
            IProjectThreadingService threadingService)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, SourceAssemblyAttributePropertyValueProvider>();
            foreach (var kvp in s_attributeNameMap)
            {
                var provider = new SourceAssemblyAttributePropertyValueProvider(kvp.Value, getActiveProjectId, workspace, threadingService);
                builder.Add(kvp.Key, provider);
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Get the unevaluated property value.
        /// </summary>
        public async override Task<string> GetUnevaluatedPropertyValueAsync(string propertyName)
        {
            if (_attributeValueProviderMap.ContainsKey(propertyName) &&
                await SaveAssemblyInfoInSourceAsync().ConfigureAwait(true))
            {
                return await GetPropertyValueFromSourceAttributeAsync(propertyName).ConfigureAwait(false);
            }

            return await base.GetUnevaluatedPropertyValueAsync(propertyName).ConfigureAwait(true);
        }

        /// <summary>
        /// Get the value of a property.
        /// </summary>
        public async override Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
        {
            if (_attributeValueProviderMap.ContainsKey(propertyName) &&
                await SaveAssemblyInfoInSourceAsync().ConfigureAwait(true))
            {
                return await GetPropertyValueFromSourceAttributeAsync(propertyName).ConfigureAwait(false);
            }

            return await base.GetEvaluatedPropertyValueAsync(propertyName).ConfigureAwait(true);
        }

        /// <summary>
        /// Get the value of a property from source assembly attribute.
        /// </summary>
        private async Task<string> GetPropertyValueFromSourceAttributeAsync(string propertyName)
        {
            if (_attributeValueProviderMap.TryGetValue(propertyName, out SourceAssemblyAttributePropertyValueProvider provider))
            {
                return await provider.GetPropertyValueAsync().ConfigureAwait(true);
            }

            return null;
        }

        /// <summary>
        /// Set the value of a property.
        /// </summary>
        public async override Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            if (_attributeValueProviderMap.ContainsKey(propertyName) &&
                await SaveAssemblyInfoInSourceAsync().ConfigureAwait(true))
            {
                if (_attributeValueProviderMap.TryGetValue(propertyName, out SourceAssemblyAttributePropertyValueProvider provider))
                {
                    await provider.SetPropertyValueAsync(unevaluatedPropertyValue).ConfigureAwait(true);
                }
            }
            else
            {
                await base.SetPropertyValueAsync(propertyName, unevaluatedPropertyValue, dimensionalConditions).ConfigureAwait(true);
            }
        }

        private async Task<bool> SaveAssemblyInfoInSourceAsync()
        {
            var configuration = await _ruleBasedProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(true);
            var result = await configuration.SaveAssemblyInfoInSource.GetValueAsync().ConfigureAwait(true);
            return result != null && (bool)result;
        }
    }
}
