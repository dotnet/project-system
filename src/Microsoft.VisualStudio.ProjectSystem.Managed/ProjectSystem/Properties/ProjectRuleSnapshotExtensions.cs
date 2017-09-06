// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    ///     Contains common extensions for <see cref="IProjectRuleSnapshot"/> instances.
    /// </summary>
    internal static class ProjectRuleSnapshotExtensions
    {
        /// <summary>
        ///     Gets the value that is associated with the specified rule and property.
        /// </summary>
        public static string GetPropertyOrDefault(this IImmutableDictionary<string, IProjectRuleSnapshot> snapshots, string ruleName, string propertyName, string defaultValue)
        {
            Requires.NotNull(snapshots, nameof(snapshots));
            Requires.NotNullOrEmpty(ruleName, nameof(ruleName));
            Requires.NotNullOrEmpty(propertyName, nameof(propertyName));
            if (snapshots.TryGetValue(ruleName, out IProjectRuleSnapshot snapshot) && snapshot.Properties.TryGetValue(propertyName, out string value))
            {
                // Similar to MSBuild, we treat the absence of a property the same as an empty property
                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            return defaultValue;
        }

        /// <summary>
        ///     Returns a value indicating if the value that is associated with the specified rule and property is <see langword="true"/>.
        /// </summary>
        public static bool IsPropertyTrue(this IImmutableDictionary<string, IProjectRuleSnapshot> snapshots, string ruleName, string propertyName, bool defaultValue)
        {
            string value = snapshots.GetPropertyOrDefault(ruleName, propertyName, defaultValue ? "true" : "false");

            return StringComparers.PropertyValues.Equals(value, "true");
        }
    }
}
