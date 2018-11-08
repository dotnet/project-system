// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal static class MetadataExtensions
    {
        /// <summary>
        /// Finds the resolved reference item for a given unresolved reference.
        /// </summary>
        /// <param name="projectRuleSnapshot">Resolved reference project items snapshot to search.</param>
        /// <param name="itemSpec">The unresolved reference item name.</param>
        /// <returns>The key is item name and the value is the metadata dictionary.</returns>
        public static IImmutableDictionary<string, string> GetProjectItemProperties(this IProjectRuleSnapshot projectRuleSnapshot, string itemSpec)
        {
            Requires.NotNull(projectRuleSnapshot, nameof(projectRuleSnapshot));
            Requires.NotNullOrEmpty(itemSpec, nameof(itemSpec));

            projectRuleSnapshot.Items.TryGetValue(itemSpec, out IImmutableDictionary<string, string> properties);

            return properties;
        }

        public static bool TryGetStringProperty(this IImmutableDictionary<string, string> properties, string key, out string stringValue)
        {
            if (properties != null &&
                properties.TryGetValue(key, out stringValue) &&
                !string.IsNullOrEmpty(stringValue))
            {
                return true;
            }

            stringValue = default;
            return false;
        }

        public static string GetStringProperty(this IImmutableDictionary<string, string> properties, string key)
        {
            return properties.TryGetStringProperty(key, out string value) ? value : null;
        }

        public static bool TryGetBoolProperty(this IImmutableDictionary<string, string> properties, string key, out bool boolValue)
        {
            if (properties.TryGetStringProperty(key, out string valueString) &&
                bool.TryParse(valueString, out boolValue))
            {
                return true;
            }

            boolValue = default;
            return false;
        }

        public static bool? GetBoolProperty(this IImmutableDictionary<string, string> properties, string key)
        {
            return properties.TryGetBoolProperty(key, out bool value) ? value : default;
        }

        public static bool TryGetEnumProperty<T>(this IImmutableDictionary<string, string> properties, string key, out T enumValue) where T : struct
        {
            if (properties.TryGetStringProperty(key, out string valueString) &&
                Enum.TryParse(valueString, ignoreCase: true, out enumValue))
            {
                return true;
            }

            enumValue = default;
            return false;
        }

        public static T? GetEnumProperty<T>(this IImmutableDictionary<string, string> properties, string key) where T : struct
        {
            return properties.TryGetEnumProperty(key, out T value) ? value : default;
        }
    }
}
