// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// A search context which decorates an <see cref="DependenciesTreeSearchContext"/> instance
    /// with project-specific behaviour.
    /// </summary>
    internal sealed class DependenciesTreeProjectSearchContext : IDependenciesTreeProjectSearchContext
    {
        private readonly DependenciesTreeSearchContext _inner;
        private readonly IProjectTree _dependenciesNode;
        private readonly IVsHierarchyItemManager _hierarchyItemManager;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IRelationProvider _relationProvider;

        public UnconfiguredProject UnconfiguredProject { get; }

        public ImmutableArray<string> TargetFrameworks { get; }

        public DependenciesTreeProjectSearchContext(
            DependenciesTreeSearchContext outer,
            UnconfiguredProject unconfiguredProject,
            ImmutableArray<string> targetFrameworks,
            IProjectTree dependenciesNode,
            IVsHierarchyItemManager hierarchyItemManager,
            IUnconfiguredProjectVsServices projectVsServices,
            IRelationProvider relationProvider)
        {
            _inner = outer;
            UnconfiguredProject = unconfiguredProject;
            TargetFrameworks = targetFrameworks;
            _dependenciesNode = dependenciesNode;
            _hierarchyItemManager = hierarchyItemManager;
            _projectVsServices = projectVsServices;
            _relationProvider = relationProvider;
        }

        public CancellationToken CancellationToken => _inner.CancellationToken;

        public IDependenciesTreeProjectTargetSearchContext? ForTarget(string targetFramework)
        {
            IProjectTree targetRootNode;

            if (TargetFrameworks.Length > 1)
            {
                IProjectTree? targetNode = _dependenciesNode.FindChildWithFlags(ProjectTreeFlags.Create("$TFM:" + targetFramework));

                if (targetNode == null)
                {
                    TraceUtilities.TraceError("Should not fail to find the target node.");
                    return null;
                }

                targetRootNode = targetNode;
            }
            else
            {
                targetRootNode = _dependenciesNode;
            }

            return new DependenciesTreeProjectTargetSearchContext(_inner, targetRootNode, _hierarchyItemManager, _projectVsServices, _relationProvider);
        }
    }
}
