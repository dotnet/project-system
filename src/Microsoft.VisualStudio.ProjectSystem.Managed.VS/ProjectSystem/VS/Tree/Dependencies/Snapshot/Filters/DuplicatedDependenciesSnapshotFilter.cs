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
    internal class DuplicatedDependenciesSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 101;

        public override IDependency BeforeAdd(
            string projectPath,
            ITargetFramework targetFramework,
            IDependency dependency,
            ImmutableDictionary<string, IDependency>.Builder worldBuilder,
            ImmutableHashSet<IDependency>.Builder topLevelBuilder,
            Dictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviders,
            HashSet<string> projectItemSpecs,
            out bool filterAnyChanges)
        {
            filterAnyChanges = false;
            IDependency resultDependency = dependency;

            IDependency matchingDependency = null;
            foreach (IDependency x in topLevelBuilder)
            {
                if (!x.Id.Equals(dependency.Id, StringComparison.OrdinalIgnoreCase)
                     && x.ProviderType.Equals(dependency.ProviderType, StringComparison.OrdinalIgnoreCase)
                     && x.Caption.Equals(dependency.Caption, StringComparison.OrdinalIgnoreCase))
                {
                    matchingDependency = x;
                    break;
                }
            }

            // If found node with same caption, or if there were nodes with same caption but with Alias already applied
            // NOTE: Performance sensitive, so avoid formatting the Caption with parenthesis if it's possible to avoid it.
            bool shouldApplyAlias = matchingDependency != null;
            if (!shouldApplyAlias)
            {
                int adjustedLength = dependency.Caption.Length + " (".Length;
                foreach (IDependency x in topLevelBuilder)
                {
                    if (!x.Id.Equals(dependency.Id)
                         && x.ProviderType.Equals(dependency.ProviderType, StringComparison.OrdinalIgnoreCase)
                         && x.Caption.StartsWith(dependency.Caption, StringComparison.OrdinalIgnoreCase)
                         && x.Caption.Length >= adjustedLength
                         && string.Compare(x.Caption, adjustedLength, x.OriginalItemSpec, 0, x.OriginalItemSpec.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        shouldApplyAlias = true;
                        break;
                    }
                }
            }

            if (shouldApplyAlias)
            {
                filterAnyChanges = true;
                if (matchingDependency != null)
                {
                    matchingDependency = matchingDependency.SetProperties(caption: matchingDependency.Alias);
                    worldBuilder.Remove(matchingDependency.Id);
                    worldBuilder.Add(matchingDependency.Id, matchingDependency);
                    topLevelBuilder.Remove(matchingDependency);
                    topLevelBuilder.Add(matchingDependency);
                }

                resultDependency = resultDependency.SetProperties(caption: dependency.Alias);
            }

            return resultDependency;
        }
    }
}
