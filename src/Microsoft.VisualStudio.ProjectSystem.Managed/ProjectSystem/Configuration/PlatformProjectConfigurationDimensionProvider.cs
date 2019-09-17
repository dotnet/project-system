// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

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
