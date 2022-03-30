// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    /// <summary>
    /// Provides 'Configuration' project configuration dimension and values.
    /// </summary>
    [Export(typeof(IProjectConfigurationDimensionsProvider))]
    [AppliesTo(ProjectCapabilities.ProjectConfigurationsDeclaredDimensions)]
    [Order(DimensionProviderOrder.Configuration)]
    [ConfigurationDimensionDescription(ConfigurationGeneral.ConfigurationProperty)]
    internal class ConfigurationProjectConfigurationDimensionProvider : BaseProjectConfigurationDimensionProvider
    {
        [ImportingConstructor]
        public ConfigurationProjectConfigurationDimensionProvider(IProjectAccessor projectAccessor)
            : base(
                  projectAccessor,
                  dimensionName: ConfigurationGeneral.ConfigurationProperty,
                  propertyName: "Configurations",
                  dimensionDefaultValue: "Debug")
        {
        }
    }
}
