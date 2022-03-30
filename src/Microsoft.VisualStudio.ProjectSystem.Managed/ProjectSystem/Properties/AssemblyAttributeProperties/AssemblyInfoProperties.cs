// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Buffers.PooledObjects;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// This class represents assembly attribute properties that are stored in the project file OR the source code of the project.
    /// </summary>
    internal class AssemblyInfoProperties : DelegatedProjectPropertiesBase
    {
        private readonly ImmutableDictionary<string, SourceAssemblyAttributePropertyValueProvider> _attributeValueProviderMap;

        // See https://github.com/dotnet/sdk/blob/master/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.GenerateAssemblyInfo.targets
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
            Func<ProjectId?> getActiveProjectId,
            Workspace workspace,
            IProjectThreadingService threadingService)
            : base(delegatedProjectProperties)
        {
            _attributeValueProviderMap = CreateAttributeValueProviderMap(getActiveProjectId, workspace, threadingService);
        }

        private static ImmutableDictionary<string, SourceAssemblyAttributePropertyValueProvider> CreateAttributeValueProviderMap(
            Func<ProjectId?> getActiveProjectId,
            Workspace workspace,
            IProjectThreadingService threadingService)
        {
            var builder = PooledDictionary<string, SourceAssemblyAttributePropertyValueProvider>.GetInstance();
            foreach ((string key, (string attributeName, _)) in AssemblyPropertyInfoMap)
            {
                var provider = new SourceAssemblyAttributePropertyValueProvider(attributeName, getActiveProjectId, workspace, threadingService);
                builder.Add(key, provider);
            }

            return builder.ToImmutableDictionaryAndFree();
        }

        /// <summary>
        /// Get the unevaluated property value.
        /// </summary>
        public override async Task<string?> GetUnevaluatedPropertyValueAsync(string propertyName)
        {
            if (_attributeValueProviderMap.ContainsKey(propertyName) &&
                !await IsAssemblyInfoPropertyGeneratedByBuildAsync(propertyName))
            {
                return await GetPropertyValueFromSourceAttributeAsync(propertyName);
            }

            return await base.GetUnevaluatedPropertyValueAsync(propertyName);
        }

        /// <summary>
        /// Get the value of a property.
        /// </summary>
        public override async Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
        {
            if (_attributeValueProviderMap.ContainsKey(propertyName) &&
                !await IsAssemblyInfoPropertyGeneratedByBuildAsync(propertyName))
            {
                return (await GetPropertyValueFromSourceAttributeAsync(propertyName)) ?? "";
            }

            return await base.GetEvaluatedPropertyValueAsync(propertyName);
        }

        /// <summary>
        /// Get the value of a property from source assembly attribute.
        /// </summary>
        private async Task<string?> GetPropertyValueFromSourceAttributeAsync(string propertyName)
        {
            if (_attributeValueProviderMap.TryGetValue(propertyName, out SourceAssemblyAttributePropertyValueProvider? provider))
            {
                return await provider.GetPropertyValueAsync();
            }

            return null;
        }

        /// <summary>
        /// Set the value of a property.
        /// </summary>
        public override async Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (_attributeValueProviderMap.TryGetValue(propertyName, out SourceAssemblyAttributePropertyValueProvider? provider) &&
                !await IsAssemblyInfoPropertyGeneratedByBuildAsync(propertyName))
            {
                await provider.SetPropertyValueAsync(unevaluatedPropertyValue);
            }
            else
            {
                await base.SetPropertyValueAsync(propertyName, unevaluatedPropertyValue, dimensionalConditions);
            }
        }

        private async Task<bool> IsAssemblyInfoPropertyGeneratedByBuildAsync(string propertyName)
        {
            (_, string generatePropertyInProjectFileName) = AssemblyPropertyInfoMap[propertyName];

            // Generate property in project file only if:
            // 1. "GenerateAssemblyInfo" is true AND
            // 2. "GenerateXXX" for this specific property is true.
            string? propertyValue = await base.GetEvaluatedPropertyValueAsync("GenerateAssemblyInfo");
            if (!bool.TryParse(propertyValue, out bool value) || !value)
            {
                return false;
            }

            propertyValue = await base.GetEvaluatedPropertyValueAsync(generatePropertyInProjectFileName);
            return bool.TryParse(propertyValue, out value) && value;
        }
    }
}
