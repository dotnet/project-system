// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets.Models;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Implements <see cref="IAttachedCollectionSource"/> for package reference nodes in the dependencies tree.
    /// </summary>
    internal sealed class PackageReferenceAttachedCollectionSource : AssetsFileAttachedCollectionSourceBase
    {
        private readonly string? _target;
        private readonly string _packageId;
        private readonly string _version;

        public PackageReferenceAttachedCollectionSource(
            UnconfiguredProject unconfiguredProject,
            IVsHierarchyItem hierarchyItem,
            string? target,
            string packageId,
            string version,
            IAssetsFileDependenciesDataSource dataSource,
            JoinableTaskContext joinableTaskContext,
            IFileIconProvider fileIconProvider)
            : base(unconfiguredProject, hierarchyItem, dataSource, joinableTaskContext, fileIconProvider)
        {
            _target = target;
            _packageId = packageId;
            _version = version;
        }

        protected override IEnumerable<object>? UpdateItems(AssetsFileDependenciesSnapshot snapshot)
        {
            List<object>? items = null;

            ProcessLogMessages(ref items, snapshot, _target, _packageId);

            if (snapshot.TryGetDependencies(_packageId, _version, _target, out ImmutableArray<AssetsFileTargetLibrary> dependencies))
            {
                ProcessLibraryReferences(ref items, snapshot, dependencies, _target);
            }

            if (snapshot.TryGetPackage(_packageId, _version, _target, out AssetsFileTargetLibrary? library))
            {
                ProcessLibraryContent(ref items, library, snapshot);
            }

            return items;
        }
    }
}
