// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    ///     Contains common extensions for <see cref="IProjectRuleSnapshot"/> instances.
    /// </summary>
    internal static partial class ProjectRuleSnapshotExtensions
    {
        /// <summary>
        ///     Gets the value that is associated with specified name, or an empty string ("") if it does not exist.
        /// </summary>
        public static string GetPropertyOrEmpty(this IImmutableDictionary<string, string> properties, string name)
        {
            Requires.NotNull(properties, nameof(properties));
            Requires.NotNullOrEmpty(name, nameof(name));

            return properties.GetValueOrDefault(name, string.Empty);
        }

        /// <summary>
        ///     Gets the value that is associated with the specified rule and property.
        /// </summary>
        [return: NotNullIfNotNull(parameterName: "defaultValue")]
        public static string? GetPropertyOrDefault(this IImmutableDictionary<string, IProjectRuleSnapshot> snapshots, string ruleName, string propertyName, string? defaultValue)
        {
            Requires.NotNull(snapshots, nameof(snapshots));
            Requires.NotNullOrEmpty(ruleName, nameof(ruleName));
            Requires.NotNullOrEmpty(propertyName, nameof(propertyName));

            if (snapshots.TryGetValue(ruleName, out IProjectRuleSnapshot snapshot) && snapshot.Properties.TryGetValue(propertyName, out string value))
            {
                // Similar to MSBuild, we treat the absence of a property the same as an empty property
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return defaultValue;
        }

        /// <summary>
        ///     Returns a value indicating if the value that is associated with the specified rule and property is <see langword="true"/>.
        /// </summary>
        public static bool IsPropertyTrue(this IImmutableDictionary<string, IProjectRuleSnapshot> snapshots, string ruleName, string propertyName, bool defaultValue)
        {
            Requires.NotNull(snapshots, nameof(snapshots));
            Requires.NotNull(ruleName, nameof(ruleName));
            Requires.NotNull(propertyName, nameof(propertyName));

            string value = snapshots.GetPropertyOrDefault(ruleName, propertyName, defaultValue ? "true" : "false");

            return StringComparers.PropertyLiteralValues.Equals(value, "true");
        }

        /// <summary>
        ///     Gets the bool value of a property, or <see langword="null"/> if it is empty or cannot otherwise be parsed as a bool.
        /// </summary>
        public static bool? GetBooleanPropertyValue(this IImmutableDictionary<string, IProjectRuleSnapshot> snapshots, string ruleName, string propertyName)
        {
            Requires.NotNull(snapshots, nameof(snapshots));
            Requires.NotNull(ruleName, nameof(ruleName));
            Requires.NotNull(propertyName, nameof(propertyName));

            string? value = snapshots.GetPropertyOrDefault(ruleName, propertyName, defaultValue: null);

            return bool.TryParse(value, out bool b) ? b : null;
        }

        /// <summary>
        ///     Gets the snapshot associated with the specified rule, or an empty snapshot if it does not exist.
        /// </summary>
        public static IProjectRuleSnapshot GetSnapshotOrEmpty(this IImmutableDictionary<string, IProjectRuleSnapshot> snapshots, string ruleName)
        {
            Requires.NotNull(snapshots, nameof(snapshots));

            if (snapshots.TryGetValue(ruleName, out IProjectRuleSnapshot result))
            {
                return result;
            }

            return EmptyProjectRuleSnapshot.Instance;
        }

        public static bool HasChange(this IImmutableDictionary<string, IProjectChangeDescription> changesByRule)
        {
            Requires.NotNull(changesByRule, nameof(changesByRule));

            foreach ((_, IProjectChangeDescription change) in changesByRule)
            {
                if (change.Difference.AnyChanges)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
