// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// If there are several top level dependencies with same captions and same provider type,
    /// we need to change their captions, to avoid collision. To de-dupe captions we change captions 
    /// for all such nodes to Alias which is "Caption (OriginalItemSpec)".
    /// </summary>
    [Export(typeof(IDependenciesSnapshotFilter))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal sealed class DuplicatedDependenciesSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 101;

        public override void BeforeAddOrUpdate(
            string projectPath,
            ITargetFramework targetFramework,
            IDependency dependency,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string> projectItemSpecs,
            IAddDependencyContext context)
        {
            IDependency matchingDependency = null;
            bool shouldApplyAlias = false;

            foreach ((string _, IDependency other) in context)
            {
                if (!other.TopLevel ||
                    other.Id.Equals(dependency.Id, StringComparison.OrdinalIgnoreCase) ||
                    !StringComparers.DependencyProviderTypes.Equals(other.ProviderType, dependency.ProviderType))
                {
                    continue;
                }

                if (other.Caption.StartsWith(dependency.Caption, StringComparison.OrdinalIgnoreCase))
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
                        string.Compare(other.Caption, expectedItemSpecIndex, itemSpec, 0, itemSpec.Length, StringComparison.OrdinalIgnoreCase) == 0)
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
                    context.AddOrUpdate(matchingDependency.SetProperties(caption: matchingDependency.Alias));
                }

                // Use the alias for the caption
                context.Accept(dependency.SetProperties(caption: dependency.Alias));
            }
            else
            {
                // Accept without changes
                context.Accept(dependency);
            }
        }
    }
}
