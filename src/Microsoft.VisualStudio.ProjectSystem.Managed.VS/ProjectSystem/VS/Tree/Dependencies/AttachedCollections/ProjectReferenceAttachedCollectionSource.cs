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
    /// Implements <see cref="IAttachedCollectionSource"/> for project reference nodes in the dependencies tree.
    /// </summary>
    internal sealed class ProjectReferenceAttachedCollectionSource : AssetsFileAttachedCollectionSourceBase
    {
        private readonly string? _target;
        private readonly string _projectId;

        public ProjectReferenceAttachedCollectionSource(
            IVsHierarchyItem hierarchyItem,
            string? target,
            string projectId,
            IAssetsFileDependenciesDataSource dataSource,
            JoinableTaskContext joinableTaskContext)
            : base(hierarchyItem, dataSource, joinableTaskContext)
        {
            _target = target;
            _projectId = projectId;
        }

        protected override IEnumerable<object>? UpdateItems(AssetsFileDependenciesSnapshot snapshot)
        {
            List<object>? items = null;

            ProcessLogMessages(ref items, snapshot, _target, _projectId);

            if (snapshot.TryGetDependencies(_projectId, version: null, _target, out ImmutableArray<AssetsFileTargetLibrary> dependencies))
            {
                ProcessLibraryReferences(ref items, snapshot, dependencies, _target);
            }

            return items;
        }
    }
}
