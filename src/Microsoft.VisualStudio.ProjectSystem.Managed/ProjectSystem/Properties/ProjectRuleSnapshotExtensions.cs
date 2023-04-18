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
            Requires.NotNull(properties);
            Requires.NotNullOrEmpty(name);

            return properties.GetValueOrDefault(name, string.Empty);
        }

        /// <summary>
        ///     Gets the value that is associated with the specified rule and property.
        /// </summary>
        [return: NotNullIfNotNull(parameterName: nameof(defaultValue))]
        public static string? GetPropertyOrDefault(this IImmutableDictionary<string, IProjectRuleSnapshot> snapshots, string ruleName, string propertyName, string? defaultValue)
        {
            Requires.NotNull(snapshots);
            Requires.NotNullOrEmpty(ruleName);
            Requires.NotNullOrEmpty(propertyName);

            if (snapshots.TryGetValue(ruleName, out IProjectRuleSnapshot? snapshot) && snapshot.Properties.TryGetValue(propertyName, out string? value))
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
            Requires.NotNull(snapshots);
            Requires.NotNull(ruleName);
            Requires.NotNull(propertyName);

            string value = snapshots.GetPropertyOrDefault(ruleName, propertyName, defaultValue ? "true" : "false");

            return StringComparers.PropertyLiteralValues.Equals(value, "true");
        }

        /// <summary>
        ///     Gets the bool value of a property, or <see langword="null"/> if it is empty or cannot otherwise be parsed as a bool.
        /// </summary>
        public static bool? GetBooleanPropertyValue(this IImmutableDictionary<string, IProjectRuleSnapshot> snapshots, string ruleName, string propertyName)
        {
            Requires.NotNull(snapshots);
            Requires.NotNull(ruleName);
            Requires.NotNull(propertyName);

            string? value = snapshots.GetPropertyOrDefault(ruleName, propertyName, defaultValue: null);

            return bool.TryParse(value, out bool b) ? b : null;
        }

        /// <summary>
        ///     Gets the snapshot associated with the specified rule, or an empty snapshot if it does not exist.
        /// </summary>
        public static IProjectRuleSnapshot GetSnapshotOrEmpty(this IImmutableDictionary<string, IProjectRuleSnapshot> snapshots, string ruleName)
        {
            Requires.NotNull(snapshots);

            if (snapshots.TryGetValue(ruleName, out IProjectRuleSnapshot? result))
            {
                return result;
            }

            return EmptyProjectRuleSnapshot.Instance;
        }

        public static bool HasChange(this IImmutableDictionary<string, IProjectChangeDescription> changesByRule)
        {
            Requires.NotNull(changesByRule);

            foreach ((_, IProjectChangeDescription change) in changesByRule)
            {
                if (change.Difference.AnyChanges)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns snapshot data in its original order. If the original order cannot be determined, and exception is thrown.
        /// Requires the snapshot to have come from CPS's build data sources. Evaluation data is currently not orderable.
        /// </summary>
        /// <remarks>
        /// You may use <see cref="TryGetOrderedItems(IProjectRuleSnapshot)"/> instead, which will return unordered data
        /// rather than throw.
        /// </remarks>
        public static IReadOnlyCollection<KeyValuePair<string, IImmutableDictionary<string, string>>> GetOrderedItems(this IProjectRuleSnapshot snapshot)
        {
            if (snapshot.Items.Count == 0)
            {
                // Some empty snapshot data can leak through here that doesn't implement IDataWithOriginalSource.
                // Protect against that and return an empty collection. There's no order to empty data, so there's
                // no behavior issue here.
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1761872
                return ImmutableList<KeyValuePair<string, IImmutableDictionary<string, string>>>.Empty;
            }

            var orderedSource = snapshot.Items as IDataWithOriginalSource<KeyValuePair<string, IImmutableDictionary<string, string>>>;

            // CPS build data is always supposed to implement this interface. If you see this exception, it's
            // most likely that the snapshot this extension method was called on contains evaluation data, for
            // which data ordering is currently not supported. If seen on build data however, this is likely
            // an issue in CPS.
            Assumes.NotNull(orderedSource);

#pragma warning disable RS0030 // Do not used banned APIs
            return orderedSource.SourceData;
#pragma warning restore RS0030 // Do not used banned APIs
        }

        /// <summary>
        /// Returns snapshot items in order, if possible.
        /// If the order cannot be determined, they are returned in a "random" order.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, IImmutableDictionary<string, string>>> TryGetOrderedItems(this IProjectRuleSnapshot snapshot)
        {
            if (snapshot.Items is IDataWithOriginalSource<KeyValuePair<string, IImmutableDictionary<string, string>>> dataWithOriginalSource)
            {
#pragma warning disable RS0030 // Do not used banned APIs
                return dataWithOriginalSource.SourceData;
#pragma warning restore RS0030 // Do not used banned APIs
            }

            // We couldn't obtain ordered items for some reason.
            // Return the items in whatever order the backing collection from CPS models them in.
            return snapshot.Items;
        }
    }
}
