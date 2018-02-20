// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// This class represents assembly attribute properties that are stored in the project file OR the source code of the project.
    /// </summary>
    internal class AssemblyInfoProperties : DelegatedProjectPropertiesBase
    {
        private readonly ImmutableDictionary<string, SourceAssemblyAttributePropertyValueProvider> _attributeValueProviderMap;

        // See https://github.com/dotnet/sdk/blob/master/src/Tasks/Microsoft.NET.Build.Tasks/build/Microsoft.NET.GenerateAssemblyInfo.targets
        internal static readonly ImmutableDictionary<string, (string attributeName, string generatePropertyInProjectFileName)> AssemblyPropertyInfoMap = new Dictionary<string, (string AttributeName, string GeneratePropertyInProjectFileName)>
        {
            { "Description",           ( AttributeName: "System.Reflection.AssemblyDescriptionAttribute", GeneratePropertyInProjectFileName: "GenerateAssemblyDescriptionAttribute" ) },
            { "Company",               ( AttributeName: "System.Reflection.AssemblyCompanyAttribute", GeneratePropertyInProjectFileName: "GenerateAssemblyCompanyAttribute" ) },
            { "Product",               ( AttributeName: "System.Reflection.AssemblyProductAttribute", GeneratePropertyInProjectFileName: "GenerateAssemblyProductAttribute" ) },
            { "Copyright",             ( AttributeName: "System.Reflection.AssemblyCopyrightAttribute", GeneratePropertyInProjectFileName: "GenerateAssemblyCopyrightAttribute" ) },
            { "AssemblyVersion",       ( AttributeName: "System.Reflection.AssemblyVersionAttribute", GeneratePropertyInProjectFileName: "GenerateAssemblyVersionAttribute" ) },
            { "Version",               ( AttributeName: "System.Reflection.AssemblyInformationalVersionAttribute", GeneratePropertyInProjectFileName: "GenerateAssemblyInformationalVersionAttribute" ) },
            { "FileVersion",           ( AttributeName: "System.Reflection.AssemblyFileVersionAttribute", GeneratePropertyInProjectFileName: "GenerateAssemblyFileVersionAttribute" ) },
            { "NeutralLanguage",       ( AttributeName: "System.Resources.NeutralResourcesLanguageAttribute", GeneratePropertyInProjectFileName: "GenerateNeutralResourcesLanguageAttribute" ) },
        }.ToImmutableDictionary();

        public AssemblyInfoProperties(
            IProjectProperties delegatedProjectProperties,
            Func<ProjectId> getActiveProjectId,
            Workspace workspace,
            IProjectThreadingService threadingService)
            : base (delegatedProjectProperties)
        {
            _attributeValueProviderMap = CreateAttributeValueProviderMap(getActiveProjectId, workspace, threadingService);
        }

        private static ImmutableDictionary<string, SourceAssemblyAttributePropertyValueProvider> CreateAttributeValueProviderMap(
            Func<ProjectId> getActiveProjectId,
            Workspace workspace,
            IProjectThreadingService threadingService)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, SourceAssemblyAttributePropertyValueProvider>();
            foreach (var kvp in AssemblyPropertyInfoMap)
            {
                var provider = new SourceAssemblyAttributePropertyValueProvider(kvp.Value.attributeName, getActiveProjectId, workspace, threadingService);
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
                !await IsAssemblyInfoPropertyGeneratedByBuild(propertyName).ConfigureAwait(true))
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
                !await IsAssemblyInfoPropertyGeneratedByBuild(propertyName).ConfigureAwait(true))
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
                !await IsAssemblyInfoPropertyGeneratedByBuild(propertyName).ConfigureAwait(true))
            {
                SourceAssemblyAttributePropertyValueProvider provider = _attributeValueProviderMap[propertyName];
                await provider.SetPropertyValueAsync(unevaluatedPropertyValue).ConfigureAwait(true);
            }
            else
            {
                await base.SetPropertyValueAsync(propertyName, unevaluatedPropertyValue, dimensionalConditions).ConfigureAwait(true);
            }
        }

        private async Task<bool> IsAssemblyInfoPropertyGeneratedByBuild(string propertyName)
        {
            (string attributeName, string generatePropertyInProjectFileName) = AssemblyPropertyInfoMap[propertyName];

            // Generate property in project file only if:
            // 1. "GenerateAssemblyInfo" is true AND
            // 2. "GenerateXXX" for this specific property is true.
            var propertyValue = await base.GetEvaluatedPropertyValueAsync("GenerateAssemblyInfo").ConfigureAwait(true);
            if (!bool.TryParse(propertyValue, out bool value) || !value)
            {
                return false;
            }

            propertyValue = await base.GetEvaluatedPropertyValueAsync(generatePropertyInProjectFileName).ConfigureAwait(true);
            if (!bool.TryParse(propertyValue, out value) || !value)
            {
                return false;
            }

            return true;
        }
    }
}
