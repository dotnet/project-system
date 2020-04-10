// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Common helpers for property rules
    /// <see cref="DataSource.SourceType"/>
    /// property.
    /// </summary>
    internal static class RuleExtensions
    {
        /// <summary>
        /// Creates an empty Rule.
        /// </summary>
        /// <param name="itemType">The item type the rule represents.  May be null or empty to represent a project-level property rule.</param>
        /// <returns>An empty rule.</returns>
        internal static Rule SynthesizeEmptyRule(string? itemType = null)
        {
            var emptyRule = new Rule
            {
                DataSource = new DataSource { ItemType = itemType, Persistence = "ProjectFile" },
            };

            return emptyRule;
        }
    }
}
