// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    /// <summary>
    ///     Contains builder methods for creating <see cref="IVsProjectProperties"/> and 
    ///     <see cref="IVsReferenceItems"/> instances.
    /// </summary>
    internal static class RestoreBuilder
    {
        /// <summary>
        ///     Converts an immutable dictionary of properties into an <see cref="IVsProjectProperties"/> instance.
        /// </summary>
        public static IVsProjectProperties ToProjectProperties(IImmutableDictionary<string, string> properties)
        {
            return new ProjectProperties(properties.Select(v => new ProjectProperty(v.Key, v.Value)));
        }

        /// <summary>
        ///     Converts an immutable dictionary of items and metadata into an <see cref="IVsReferenceItems"/> instance.
        /// </summary>
        public static IVsReferenceItems ToReferenceItems(IImmutableDictionary<string, IImmutableDictionary<string, string>> items)
        {
            return new ReferenceItems(items.Select(item => ToReferenceItem(item.Key, item.Value)));
        }

        public static IVsReferenceItem ToReferenceItem(string name, IImmutableDictionary<string, string> metadata)
        {
            return new ReferenceItem(name, ToReferenceProperties(metadata));
        }

        private static IVsReferenceProperties ToReferenceProperties(IImmutableDictionary<string, string> metadata)
        {
            return new ReferenceProperties(metadata.Select(property => new ReferenceProperty(property.Key, property.Value)));
        }
    }
}
