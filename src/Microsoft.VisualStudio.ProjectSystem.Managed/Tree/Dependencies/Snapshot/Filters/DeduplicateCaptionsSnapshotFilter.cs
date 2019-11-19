// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Deduplicates captions of top-level dependencies from the same provider. This is done by
    /// appending the <see cref="IDependencyModel.OriginalItemSpec"/> to the caption in parentheses.
    /// </summary>
    [Export(typeof(IDependenciesSnapshotFilter))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal sealed class DeduplicateCaptionsSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 101;

        public override void BeforeAddOrUpdate(
            ITargetFramework targetFramework,
            IDependency dependency,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs,
            AddDependencyContext context)
        {
            // Only apply to top-level dependencies
            if (!dependency.TopLevel)
            {
                context.Accept(dependency);
                return;
            }

            IDependency? matchingDependency = null;
            bool shouldApplyAlias = false;

            foreach ((string _, IDependency other) in context)
            {
                if (!other.TopLevel ||
                    StringComparers.DependencyTreeIds.Equals(other.Id, dependency.Id) ||
                    !StringComparers.DependencyProviderTypes.Equals(other.ProviderType, dependency.ProviderType))
                {
                    continue;
                }

                if (other.Caption.StartsWith(dependency.Caption, StringComparisons.ProjectTreeCaptionIgnoreCase))
                {
                    if (other.Caption.Length == dependency.Caption.Length)
                    {
                        // Exact match.
                        matchingDependency = other;
                        shouldApplyAlias = true;
                        break;
                    }

                    // Prefix matches.
                    // Check whether we have a match of form "Caption (ItemSpec)".

                    string itemSpec = other.OriginalItemSpec;
                    int expectedItemSpecIndex = dependency.Caption.Length + 2;        // " (".Length
                    int expectedLength = expectedItemSpecIndex + itemSpec.Length + 1; // ")".Length

                    if (other.Caption.Length == expectedLength &&
                        string.Compare(other.Caption, expectedItemSpecIndex, itemSpec, 0, itemSpec.Length, StringComparisons.ProjectTreeCaptionIgnoreCase) == 0)
                    {
                        shouldApplyAlias = true;
                    }
                }
            }

            if (shouldApplyAlias)
            {
                if (matchingDependency != null)
                {
                    // Change the matching dependency's alias too
                    context.AddOrUpdate(matchingDependency.SetProperties(caption: GetAlias(matchingDependency)));
                }

                // Use the alias for the caption
                context.Accept(dependency.SetProperties(caption: GetAlias(dependency)));
            }
            else
            {
                // Accept without changes
                context.Accept(dependency);
            }

            return;

            static string GetAlias(IDependency dependency)
            {
                string path = dependency.OriginalItemSpec ?? dependency.Path;

                return string.IsNullOrEmpty(path) || path.Equals(dependency.Caption, StringComparisons.ProjectTreeCaptionIgnoreCase)
                    ? dependency.Caption
                    : string.Concat(dependency.Caption, " (", path, ")");
            }
        }
    }
}
