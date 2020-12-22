// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters
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
            IDependency dependency,
            AddDependencyContext context)
        {
            IDependency? matchingDependency = null;
            bool shouldApplyAlias = false;

            foreach ((DependencyId _, IDependency other) in context)
            {
                if (StringComparers.DependencyTreeIds.Equals(other.Id, dependency.Id) ||
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
                    // Check whether we have a match of form "Caption (Suffix)".
                    string? suffix = GetSuffix(other);

                    if (suffix != null)
                    {
                        int expectedItemSpecIndex = dependency.Caption.Length + 2; // " (".Length
                        int expectedLength = expectedItemSpecIndex + suffix.Length + 1; // ")".Length

                        if (other.Caption.Length == expectedLength &&
                            string.Compare(other.Caption, expectedItemSpecIndex, suffix, 0, suffix.Length, StringComparisons.ProjectTreeCaptionIgnoreCase) == 0)
                        {
                            shouldApplyAlias = true;
                        }
                    }
                }
            }

            if (shouldApplyAlias)
            {
                if (matchingDependency != null)
                {
                    // Change the matching dependency's caption too
                    context.AddOrUpdate(matchingDependency.WithCaption(caption: GetAlias(matchingDependency)));
                }

                // Use the alias for the caption
                context.Accept(dependency.WithCaption(caption: GetAlias(dependency)));
            }
            else
            {
                // Accept without changes
                context.Accept(dependency);
            }

            return;

            static string? GetSuffix(IDependency dependency) => dependency.OriginalItemSpec ?? dependency.FilePath;

            static string GetAlias(IDependency dependency)
            {
                string? suffix = GetSuffix(dependency);

                return Strings.IsNullOrEmpty(suffix) || suffix.Equals(dependency.Caption, StringComparisons.ProjectTreeCaptionIgnoreCase)
                    ? dependency.Caption
                    : string.Concat(dependency.Caption, " (", suffix, ")");
            }
        }
    }
}
