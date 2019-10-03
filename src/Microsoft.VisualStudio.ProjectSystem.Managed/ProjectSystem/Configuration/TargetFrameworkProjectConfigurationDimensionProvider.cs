// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

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
