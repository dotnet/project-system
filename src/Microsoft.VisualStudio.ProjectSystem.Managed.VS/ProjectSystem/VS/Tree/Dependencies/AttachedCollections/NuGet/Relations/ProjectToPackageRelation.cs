// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.NuGet.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;

namespace Microsoft.VisualStudio.NuGet
{
    [Export(typeof(IRelation))]
    internal sealed class ProjectToPackageRelation : RelationBase<ProjectReferenceItem, PackageReferenceItem>
    {
        protected override bool HasContainedItems(ProjectReferenceItem parent)
        {
            if (parent.Target.TryGetDependencies(parent.Library.Name, version: null, out ImmutableArray<AssetsFileTargetLibrary> dependencies))
            {
                return dependencies.Any(dependency => dependency.Type == AssetsFileLibraryType.Package);
            }

            return false;
        }

        protected override void UpdateContainsCollection(ProjectReferenceItem parent, AggregateContainsRelationCollectionSpan span)
        {
            if (!parent.Target.TryGetDependencies(parent.Library.Name, version: null, out ImmutableArray<AssetsFileTargetLibrary> dependencies))
            {
                dependencies = ImmutableArray<AssetsFileTargetLibrary>.Empty;
            }

            span.UpdateContainsItems(
                dependencies.Where(dep => dep.Type == AssetsFileLibraryType.Package).OrderBy(library => library.Name),
                (library, item) => StringComparer.Ordinal.Compare(library.Name, item.Library.Name),
                (library, item) => item.TryUpdateState(parent.Target, library),
                library => new PackageReferenceItem(parent.Target, library));
        }

        protected override IEnumerable<ProjectReferenceItem>? CreateContainedByItems(PackageReferenceItem child)
        {
            if (child.Target.TryGetDependents(child.Library.Name, out ImmutableArray<AssetsFileTargetLibrary> dependents))
            {
                return dependents
                    .Where(dep => dep.Type == AssetsFileLibraryType.Project)
                    .Select(library => new ProjectReferenceItem(child.Target, library));
            }

            return null;
        }
    }
}
