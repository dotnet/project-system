// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    ///     Provides extension methods for <see cref="BaseProperty"/> instances.
    /// </summary>
    internal static class BasePropertyExtensions
    {
        /// <summary>
        ///     Returns the value of the metadata item identified by <paramref name="metadataName"/>.
        /// </summary>
        /// <param name="property">The property to examine.</param>
        /// <param name="metadataName">The name of the metadata item to return.</param>
        /// <returns>
        ///     The value of the corresponding metadata item, or <see langword="null"/> if it is not
        ///     found.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="property"/> is <see langword="null"/> or empty.
        /// </exception>
        public static string? GetMetadataValueOrNull(this BaseProperty property, string metadataName)
        {
            Requires.NotNull(property, nameof(property));
            Requires.NotNullOrEmpty(metadataName, nameof(metadataName));

            return property.Metadata.FirstOrDefault(nvp => nvp.Name == metadataName)?.Value;
        }

        /// <summary>
        ///     Whether or not the the <paramref name="property"/> is configuration-dependent or not.
        /// </summary>
        /// <param name="property">The property to examine.</param>
        /// <returns>
        ///     <see langword="true"/> if the property's <see cref="DataSource"/> (or the <see cref="Rule"/>'s
        ///     <see cref="DataSource"/>) indicates that it is configuration-dependent; <see langword="false"/>
        ///     otherwise.
        /// </returns>
        public static bool IsConfigurationDependent(this BaseProperty property)
        {
            return property.DataSource?.HasConfigurationCondition
                ?? property.ContainingRule.DataSource?.HasConfigurationCondition
                ?? false;
        }
    }
}
