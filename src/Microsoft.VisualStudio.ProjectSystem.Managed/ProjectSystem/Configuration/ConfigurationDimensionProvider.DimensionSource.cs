// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    internal partial class ConfigurationDimensionProvider
    {
        /// <summary>
        ///     Represents the source of a dimension value.
        /// </summary>
        private enum DimensionSource
        {
            // NOTE: Do not rename these values as they are used in telemetry.

            /// <summary>
            ///     The dimension was not found.
            /// </summary>
            NotFound,

            /// <summary>
            ///     The dimension was declared via the "Configurations", "Platforms" or "TargetFrameworks" properties.
            /// </summary>
            Declared,

            /// <summary>
            ///     The dimension was declared via the "Configuration" or "Platform" properties.
            /// </summary>
            Singular,

            /// <summary>
            ///     The dimension was declared implicitly via conditions against the "Configuration" or "Platform" properties.
            /// </summary>
            Implicit,

            /// <summary>
            ///     The dimension's value came from the solution's active configuration. This only occurs when guessing a dimension value.
            /// </summary>
            SolutionConfiguration,

            /// <summary>
            ///     The dimension's value came from the default value of the dimension.
            /// </summary>
            DefaultValue,
        }
    }
}
