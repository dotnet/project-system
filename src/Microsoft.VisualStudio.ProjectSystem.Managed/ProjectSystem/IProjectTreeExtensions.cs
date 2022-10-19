// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.Contracts;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectTreeExtensions
    {
        /// <summary>
        /// Gets the direct child of <paramref name="tree"/> with <paramref name="caption"/>
        /// if found, otherwise <see langword="null"/>.
        /// </summary>
        [Pure]
        public static IProjectTree? FindChildWithCaption(this IProjectTree tree, string caption)
        {
            return tree.Children.FirstOrDefault(
                (child, cap) => string.Equals(cap, child.Caption, StringComparisons.ProjectTreeCaptionIgnoreCase),
                caption);
        }

        /// <summary>
        /// Finds the first child node having <paramref name="flags"/>, or <see langword="null"/> if no child matches.
        /// </summary>
        [Pure]
        internal static IProjectTree? FindChildWithFlags(this IProjectTree self, ProjectTreeFlags flags)
        {
            foreach (IProjectTree child in self.Children)
            {
                if (child.Flags.Contains(flags))
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>
        ///     Returns the properties of a <see cref="IProjectTree"/>, returning the result from a 
        ///     project snapshot if it is available, otherwise, returns the live results.
        /// </summary>
        /// <remarks>
        ///     Prefer this method over <see cref="IProjectTree.BrowseObjectProperties"/> to avoid 
        ///     needing to take a project lock to read properties, which can avoid UI delays but
        ///     with possibility of out-of-date data.
        /// </remarks>
        internal static IRule? GetBrowseObjectPropertiesViaSnapshotIfAvailable(this IProjectTree node, ConfiguredProject project)
        {
            Assumes.Present(project.Services.PropertyPagesCatalog);

            IRule? properties = node.BrowseObjectProperties;

            if (properties?.Schema is null || !project.Services.PropertyPagesCatalog.SourceBlock.TryReceive(null, out IProjectVersionedValue<IProjectCatalogSnapshot>? catalogSnapshot))
                return properties;

            // We let the snapshot be out of date with the "live" project
            if (!catalogSnapshot.Value.NamedCatalogs.TryGetValue(PropertyPageContexts.BrowseObject, out IPropertyPagesCatalog pagesCatalog))
                return properties;

            Assumes.NotNull(catalogSnapshot.Value.Project);

            IRule? snapshot = pagesCatalog.BindToContext(properties.Schema.Name, catalogSnapshot.Value.Project.ProjectInstance, properties.ItemType, properties.ItemName);

            return snapshot ?? properties;
        }
    }
}
