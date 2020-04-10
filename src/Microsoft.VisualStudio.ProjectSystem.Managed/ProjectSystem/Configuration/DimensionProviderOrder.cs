// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    /// <summary>
    ///     Contains constants representing the order precedence for <see cref="IProjectConfigurationDimensionsProvider2"/> implementations.
    /// </summary>
    internal static class DimensionProviderOrder
    {
        // These values determine the order of dimensions inside the configuration service.
        // We want Configuration|Platform|TargetFramework.

        public const int Configuration = Order.BeforeBeforeDefault;
        public const int Platform = Order.BeforeDefault;
        public const int TargetFramework = Order.Default;
    }
}
