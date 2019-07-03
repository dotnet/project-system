// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    /// <summary>
    /// Provides a customized view to extract configuration dimension description data from <see cref="ConfigurationDimensionDescriptionAttribute"/>.
    /// </summary>
    public interface IConfigurationDimensionDescriptionMetadataView : IOrderPrecedenceMetadataView
    {
#pragma warning disable CA1819 // Properties should not return arrays

        /// <summary>
        /// Dimension names.
        /// This must match <see cref="ConfigurationDimensionDescriptionAttribute.DimensionName"/>.
        /// </summary>
        string[] DimensionName { get; }


        /// <summary>
        /// Whether it is a dimension to calculate configuration groups.
        /// This must match <see cref="ConfigurationDimensionDescriptionAttribute.IsVariantDimension"/>.
        /// </summary>
        bool[] IsVariantDimension { get; }

#pragma warning restore CA1819 // Properties should not return arrays
    }
}
