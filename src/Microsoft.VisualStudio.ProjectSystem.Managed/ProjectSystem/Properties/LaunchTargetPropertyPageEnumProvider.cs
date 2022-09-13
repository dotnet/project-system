// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Provides the names and display names of "debugger" property pages that specify a
    /// launch target.
    /// </summary>
    /// <remarks>
    /// Specifically, we look for pages with a "commandNameBasedDebugger" <see cref="Rule.PageTemplate"/>
    /// and a CommandName property in their metadata.
    /// </remarks>
    [ExportDynamicEnumValuesProvider(nameof(LaunchTargetPropertyPageEnumProvider))]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    internal class LaunchTargetPropertyPageEnumProvider : IDynamicEnumValuesProvider
    {
        private readonly ConfiguredProject _configuredProject;

        [ImportingConstructor]
        public LaunchTargetPropertyPageEnumProvider(ConfiguredProject configuredProject)
        {
            _configuredProject = configuredProject;
        }

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(new LaunchTargetPropertyPageEnumValuesGenerator(_configuredProject));
        }

        internal class LaunchTargetPropertyPageEnumValuesGenerator : IDynamicEnumValuesGenerator
        {
            private readonly ConfiguredProject _configuredProject;

            public LaunchTargetPropertyPageEnumValuesGenerator(ConfiguredProject configuredProject)
            {
                _configuredProject = configuredProject;
            }

            public bool AllowCustomValues => false;

            public async Task<ICollection<IEnumValue>> GetListedValuesAsync()
            {
                var catalogProvider = _configuredProject.Services.PropertyPagesCatalog;

                if (catalogProvider is null)
                {
                    return Array.Empty<IEnumValue>();
                }

                IPropertyPagesCatalog catalog = await catalogProvider.GetCatalogAsync(PropertyPageContexts.Project);
                return catalog.GetPropertyPagesSchemas()
                    .Select(catalog.GetSchema)
                    .WhereNotNull()
                    .Where(rule => string.Equals(rule.PageTemplate, "CommandNameBasedDebugger", StringComparison.OrdinalIgnoreCase)
                            && rule.Metadata.TryGetValue("CommandName", out object pageCommandNameObj))
                    .Select(rule => new PageEnumValue(new EnumValue
                    {
                        Name = rule.Name,
                        DisplayName = rule.DisplayName
                    }))
                    .ToArray<IEnumValue>();
            }

            public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue) => TaskResult.Null<IEnumValue>();
        }
    }
}
