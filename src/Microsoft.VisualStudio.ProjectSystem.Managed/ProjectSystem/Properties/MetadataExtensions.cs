// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal static class MetadataExtensions
    {
        /// <summary>
        /// Finds the resolved reference item for a given unresolved reference.
        /// </summary>
        /// <param name="projectRuleSnapshot">Resolved reference project items snapshot to search.</param>
        /// <param name="itemSpec">The unresolved reference item name.</param>
        /// <returns>The key is item name and the value is the metadata dictionary.</returns>
        public static IImmutableDictionary<string, string>? GetProjectItemProperties(this IProjectRuleSnapshot projectRuleSnapshot, string itemSpec)
        {
            Requires.NotNull(projectRuleSnapshot, nameof(projectRuleSnapshot));
            Requires.NotNullOrEmpty(itemSpec, nameof(itemSpec));

            return projectRuleSnapshot.Items.TryGetValue(itemSpec, out IImmutableDictionary<string, string> properties)
                ? properties
                : null;
        }

        /// <summary>
        /// Attempts to get the string value corresponding to <paramref name="key"/>.
        /// </summary>
        /// <remarks>
        /// Missing and empty string values are treated in the same fashion.
        /// </remarks>
        /// <param name="properties">The property dictionary to query.</param>
        /// <param name="key">The key that identifies the property to look up.</param>
        /// <param name="stringValue">The value of the string if found and non-empty, otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the property was found with a non-empty value, otherwise <see langword="false"/>.</returns>
        public static bool TryGetStringProperty(this IImmutableDictionary<string, string> properties, string key, [NotNullWhen(returnValue: true)] out string? stringValue)
        {
            if (properties is not null &&
                properties.TryGetValue(key, out stringValue) &&
                !string.IsNullOrEmpty(stringValue))
            {
                return true;
            }

            stringValue = null;
            return false;
        }

        /// <summary>
        /// Gets the string value corresponding to <paramref name="key"/>, or <see langword="null"/> if the property was not found or had an empty value.
        /// </summary>
        /// <param name="properties">The property dictionary to query.</param>
        /// <param name="key">The key that identifies the property to look up.</param>
        /// <returns>The string value if found and non-empty, otherwise <see langword="null"/>.</returns>
        public static string? GetStringProperty(this IImmutableDictionary<string, string> properties, string key)
        {
            return properties.TryGetStringProperty(key, out string? value) && !string.IsNullOrEmpty(value) ? value : null;
        }

        /// <summary>
        /// Attempts to get the boolean interpretation of the value corresponding to <paramref name="key"/>.
        /// </summary>
        /// <param name="properties">The property dictionary to query.</param>
        /// <param name="key">The key that identifies the property to look up.</param>
        /// <param name="boolValue">The boolean value of the property if found and successfully parsed, otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the property was found with successfully parsed as a boolean, otherwise <see langword="false"/>.</returns>
        public static bool TryGetBoolProperty(this IImmutableDictionary<string, string> properties, string key, out bool boolValue)
        {
            if (properties.TryGetStringProperty(key, out string? valueString) &&
                bool.TryParse(valueString, out boolValue))
            {
                return true;
            }

            boolValue = default;
            return false;
        }

        /// <summary>
        /// Gets the boolean value corresponding to <paramref name="key"/>, or <see langword="null"/> if the property was missing or could not be parsed as a boolean.
        /// </summary>
        /// <param name="properties">The property dictionary to query.</param>
        /// <param name="key">The key that identifies the property to look up.</param>
        /// <returns>The boolean value if found and successfully parsed as a boolean, otherwise <see langword="null"/>.</returns>
        public static bool? GetBoolProperty(this IImmutableDictionary<string, string> properties, string key)
        {
            return properties.TryGetBoolProperty(key, out bool value) ? value : null;
        }

        /// <summary>
        /// Attempts to get the enum type <typeparamref name="T"/> interpretation of the value corresponding to <paramref name="key"/>.
        /// </summary>
        /// <param name="properties">The property dictionary to query.</param>
        /// <param name="key">The key that identifies the property to look up.</param>
        /// <param name="enumValue">The enum value of the property if found and successfully parsed, otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the property was found with successfully parsed as enum type <typeparamref name="T"/>, otherwise <see langword="false"/>.</returns>
        /// <typeparam name="T">The enum type.</typeparam>
        public static bool TryGetEnumProperty<T>(this IImmutableDictionary<string, string> properties, string key, out T enumValue) where T : struct, Enum
        {
            if (properties.TryGetStringProperty(key, out string? valueString) &&
                Enum.TryParse(valueString, ignoreCase: true, out enumValue))
            {
                return true;
            }

            enumValue = default;
            return false;
        }

        /// <summary>
        /// Gets the enum value corresponding to <paramref name="key"/>, or <see langword="null"/> if the property was missing or could not be parsed as an enum.
        /// </summary>
        /// <param name="properties">The property dictionary to query.</param>
        /// <param name="key">The key that identifies the property to look up.</param>
        /// <returns>The enum value if found and successfully parsed as enum type <typeparamref name="T"/>, otherwise <see langword="null"/>.</returns>
        /// <typeparam name="T">The enum type.</typeparam>
        public static T? GetEnumProperty<T>(this IImmutableDictionary<string, string> properties, string key) where T : struct, Enum
        {
            return properties.TryGetEnumProperty(key, out T value) ? value : null;
        }
    }
}
