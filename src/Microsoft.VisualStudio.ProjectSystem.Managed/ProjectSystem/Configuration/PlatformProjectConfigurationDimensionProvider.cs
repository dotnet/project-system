// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    /// <summary>
    /// Provides 'Platform' project configuration dimension and values.
    /// </summary>
    [Export(typeof(IProjectConfigurationDimensionsProvider))]
    [AppliesTo(ProjectCapabilities.ProjectConfigurationsDeclaredDimensions)]
    [Order(DimensionProviderOrder.Platform)]
    [ConfigurationDimensionDescription(ConfigurationGeneral.PlatformProperty)]
    internal class PlatformProjectConfigurationDimensionProvider : BaseProjectConfigurationDimensionProvider
    {
        [ImportingConstructor]
        public PlatformProjectConfigurationDimensionProvider(IProjectAccessor projectAccessor)
            : base(
                  projectAccessor,
                  dimensionName: ConfigurationGeneral.PlatformProperty,
                  propertyName: "Platforms",
                  dimensionDefaultValue: "AnyCPU")
        {
        }
    }
}
