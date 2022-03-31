// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    /// <summary>
    /// Provides "TargetFramework" project configuration dimension and values.
    /// </summary>
    [Export(typeof(IProjectConfigurationDimensionsProvider))]
    [Export(typeof(IActiveConfiguredProjectsDimensionProvider))]
    [AppliesTo(ProjectCapabilities.ProjectConfigurationsDeclaredDimensions)]
    [Order(DimensionProviderOrder.TargetFramework)]
    [ConfigurationDimensionDescription(ConfigurationGeneral.TargetFrameworkProperty, isVariantDimension: true)]
    internal class TargetFrameworkProjectConfigurationDimensionProvider : BaseProjectConfigurationDimensionProvider, IActiveConfiguredProjectsDimensionProvider
    {
        [ImportingConstructor]
        public TargetFrameworkProjectConfigurationDimensionProvider(IProjectAccessor projectAccessor)
            : base(
                  projectAccessor,
                  dimensionName: ConfigurationGeneral.TargetFrameworkProperty,
                  propertyName: ConfigurationGeneral.TargetFrameworksProperty)
        {
        }
    }
}
