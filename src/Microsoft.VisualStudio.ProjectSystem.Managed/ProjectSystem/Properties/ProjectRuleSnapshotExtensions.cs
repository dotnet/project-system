// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;

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

            string value;
            IProjectRuleSnapshot snapshot;
            if (snapshots.TryGetValue(ruleName, out snapshot) && snapshot.Properties.TryGetValue(propertyName, out value))
            {
                // Similar to MSBuild, we treat the absence of a property the same as an empty property
                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            return defaultValue;
        }
    }
}
