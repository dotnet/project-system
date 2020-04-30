// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.NuGet.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;

namespace Microsoft.VisualStudio.NuGet
{
    [Export(typeof(IRelation))]
    internal sealed class PackageToFrameworkAssemblyGroupRelation : RelationBase<PackageReferenceItem, PackageAssemblyGroupItem>
    {
        protected override bool HasContainedItems(PackageReferenceItem parent) => parent.Library.FrameworkAssemblies.Length != 0;

        protected override void UpdateContainsCollection(PackageReferenceItem parent, AggregateContainsRelationCollectionSpan span)
        {
            span.UpdateContainsItems(
                parent.Library.FrameworkAssemblies.Length == 0 ? Array.Empty<AssetsFileTargetLibrary>() : new[] { parent.Library },
                (library, item) => 0,
                (library, item) => false,
                library => new PackageAssemblyGroupItem(parent.Target, library, PackageAssemblyGroupType.Framework));
        }

        protected override IEnumerable<PackageReferenceItem>? CreateContainedByItems(PackageAssemblyGroupItem child)
        {
            if (child.GroupType == PackageAssemblyGroupType.Framework)
            {
                yield return new PackageReferenceItem(child.Target, child.Library);
            }
        }
    }
}
