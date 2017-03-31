// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Configuration
{
    /// <summary>
    ///     Contains constants representing the order precedence for <see cref="IProjectConfigurationDimensionsProvider2"/> implementations.
    /// </summary>
    internal static class DimensionProviderOrder
    {
        public const int Configuration = Order.BeforeBeforeDefault;
        public const int Platform = Order.BeforeDefault;
        public const int TargetFramework = Order.Default;
    }
}
