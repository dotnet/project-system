// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Framework.XamlTypes;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Common helpers for property rules
    /// <see cref="Microsoft.Build.Framework.XamlTypes.DataSource.SourceType"/>
    /// property.
    /// </summary>
    internal static class RuleExtensions
    {
        /// <summary>
        /// Creates an empty Rule.
        /// </summary>
        /// <param name="itemType">The item type the rule represents.  May be null or empty to represent a project-level property rule.</param>
        /// <returns>An empty rule.</returns>
        internal static Rule SynthesizeEmptyRule(string itemType = null)
        {
            var emptyRule = new Rule
            {
                DataSource = new DataSource { ItemType = itemType, Persistence = "ProjectFile" },
            };

            return emptyRule;
        }

        /// <summary>
        /// Gets the rule from the specified catalog, if such a catalog and rule exist.
        /// </summary>
        /// <param name="snapshot">The snapshot to read from.</param>
        /// <param name="catalogName">The name of the catalog.</param>
        /// <param name="ruleName">The name of the rule.</param>
        /// <returns>The rule, if found; otherwise <c>null</c>.</returns>
        internal static Rule GetSchema(this IProjectCatalogSnapshot snapshot, string catalogName, string ruleName)
        {
            Requires.NotNull(snapshot, nameof(snapshot));
            Requires.NotNull(catalogName, nameof(catalogName));
            Requires.NotNullOrEmpty(ruleName, nameof(ruleName));

            IPropertyPagesCatalog catalog;
            if (snapshot.NamedCatalogs.TryGetValue(catalogName, out catalog))
            {
                return catalog.GetSchema(ruleName);
            }
            else
            {
                return null;
            }
        }
    }
}
