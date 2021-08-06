// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    internal partial class ConfigurationDimensionProvider
    {
        /// <summary>
        ///     Describes a single dimension, such as "Configuration".
        /// </summary>
        internal record DimensionDefinition(
            string SingularPropertyName,
            string MultiplePropertyName,
            bool IsVariantDimension,
            string? DefaultValue)
        {
            public string Name = SingularPropertyName;
        }
    }
}
