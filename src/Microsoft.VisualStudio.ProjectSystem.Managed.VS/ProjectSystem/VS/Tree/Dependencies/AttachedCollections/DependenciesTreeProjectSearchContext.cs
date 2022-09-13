// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
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

        public DependenciesTreeProjectSearchContext(
            DependenciesTreeSearchContext outer,
            UnconfiguredProject unconfiguredProject,
            IProjectTree dependenciesNode,
            IVsHierarchyItemManager hierarchyItemManager,
            IUnconfiguredProjectVsServices projectVsServices,
            IRelationProvider relationProvider)
        {
            _inner = outer;
            UnconfiguredProject = unconfiguredProject;
            _dependenciesNode = dependenciesNode;
            _hierarchyItemManager = hierarchyItemManager;
            _projectVsServices = projectVsServices;
            _relationProvider = relationProvider;
        }

        public CancellationToken CancellationToken => _inner.CancellationToken;

        public async Task<IDependenciesTreeConfiguredProjectSearchContext?> ForConfiguredProjectAsync(ConfiguredProject configuredProject, CancellationToken cancellationToken = default)
        {
            Requires.NotNull(configuredProject, nameof(configuredProject));

            IProjectTree targetRootNode;

            if (_dependenciesNode.FindChildWithFlags(DependencyTreeFlags.TargetNode) is null)
            {
                // Tree does not show any target nodes
                targetRootNode = _dependenciesNode;
            }
            else
            {
                if (configuredProject.Services.ProjectSubscription is null)
                {
                    return null;
                }

                IProjectSubscriptionUpdate subscriptionUpdate = (await configuredProject.Services.ProjectSubscription.ProjectRuleSource.GetLatestVersionAsync(configuredProject, cancellationToken: cancellationToken)).Value;

                if (!subscriptionUpdate.CurrentState.TryGetValue(ConfigurationGeneral.SchemaName, out IProjectRuleSnapshot configurationGeneralSnapshot) ||
                    !configurationGeneralSnapshot.Properties.TryGetValue(ConfigurationGeneral.TargetFrameworkProperty, out string tf))
                {
                    return null;
                }

                IProjectTree? targetNode = _dependenciesNode.FindChildWithFlags(ProjectTreeFlags.Create("$TFM:" + tf));

                if (targetNode is null)
                {
                    TraceUtilities.TraceError("Should not fail to find the target node.");
                    return null;
                }

                targetRootNode = targetNode;
            }

            return new DependenciesTreeConfiguredProjectSearchContext(_inner, targetRootNode, _hierarchyItemManager, _projectVsServices, _relationProvider);
        }
    }
}
