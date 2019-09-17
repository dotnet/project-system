// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
