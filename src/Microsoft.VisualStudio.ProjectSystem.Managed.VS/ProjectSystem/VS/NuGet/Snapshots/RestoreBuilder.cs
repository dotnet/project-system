// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    /// <summary>
    ///     Contains builder methods for creating <see cref="IVsProjectProperty"/> and 
    ///     <see cref="IVsReferenceItems"/> instances.
    /// </summary>
    internal static class RestoreBuilder
    {
        /// <summary>
        ///     Converts an immutable dictionary of properties into an <see cref="IEnumerable{T}"/> of 
        ///     <see cref="IVsProjectProperty"/> instances.
        /// </summary>
        public static IEnumerable<IVsProjectProperty> ToProjectProperties(IImmutableDictionary<string, string> properties)
        {
            return properties.Select(v => new ProjectProperty(v.Key, v.Value));
        }

        /// <summary>
        ///     Converts an immutable dictionary of items and metadata into an <see cref="IEnumerable{T}"/> of 
        ///     <see cref="IVsReferenceItem"/> instances.
        /// </summary>
        public static IEnumerable<IVsReferenceItem> ToReferenceItems(IImmutableDictionary<string, IImmutableDictionary<string, string>> items)
        {
            return items.Select(item => ToReferenceItem(item.Key, item.Value));
        }

        public static IVsReferenceItem ToReferenceItem(string name, IImmutableDictionary<string, string> metadata)
        {
            IEnumerable<IVsReferenceProperty> properties = metadata.Select(property => new ReferenceProperty(property.Key, property.Value));

            return new ReferenceItem(name, properties);
        }
    }
}
