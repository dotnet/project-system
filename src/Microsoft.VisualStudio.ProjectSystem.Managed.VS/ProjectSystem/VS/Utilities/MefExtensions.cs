// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal static class MefExtensions
    {
        /// <summary>
        /// Get a single component, if present
        /// </summary>
        /// <typeparam name="T">The type identity of the export to retrieve.</typeparam>
        /// <param name="exportProvider">The container to query.</param>
        /// <param name="capabilitiesScope"></param>
        /// <returns>The exported component</returns>
        [return: MaybeNull]
        public static T GetExportedValueOrDefault<T>(this ExportProvider exportProvider, IProjectCapabilitiesScope capabilitiesScope)
        {
            Requires.NotNull(exportProvider, nameof(exportProvider));
            Requires.NotNull(capabilitiesScope, nameof(capabilitiesScope));

            Lazy<T, IAppliesToMetadataView> lazy = exportProvider
                .GetExports<T, IAppliesToMetadataView>()
                .SingleOrDefault(export => export.Metadata.AppliesTo(capabilitiesScope));

            return lazy == null ? default : lazy.Value;
        }
    }
}
