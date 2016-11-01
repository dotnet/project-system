// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Base class for <see cref="IProjectDependenciesSubTreeProvider"/> implementations that need to 
    /// subscribe to design time build to get data.
    /// </summary>
    internal abstract class DependenciesSubTreeProviderBase : OnceInitializedOnceDisposed, IProjectDependenciesSubTreeProvider
    {
        /// <summary>
        /// A lock object to protect data integrity of this instance.
        /// </summary>
        private readonly object _syncObject = new object();

        /// <summary>
        /// A cache of rule names to item types.
        /// </summary>
        private readonly Dictionary<string, string> _ruleNameToItemType
                = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A cache of item types to rule names.
        /// </summary>
        private readonly Dictionary<string, string> _itemTypeToRuleName
                = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The value to dispose to terminate the link providing evaluation snapshots.
        /// </summary>
        private List<IDisposable> _evaluationSubscriptionLinks;

        /// <summary>
        /// The value to dispose to terminate the SyncLinkTo subscriptions.
        /// </summary>
        private List<IDisposable> _projectSyncLinks;

        private object _rootNodeSync = new object();

        /// <summary>
        /// Gets the project asynchronous tasks service.
        /// </summary>
        [Import(ExportContractNames.Scopes.UnconfiguredProject)]
        protected IProjectAsynchronousTasksService UnconfiguredProjectAsynchronousTasksService { get; private set; }

        /// <summary>
        /// Gets the subscription service for source items.
        /// </summary>
        [Import]
        protected IActiveConfiguredProjectSubscriptionService ProjectSubscriptionService { get; private set; }

        /// <summary>
        /// The set of rule names that represent unresolved references.
        /// </summary>
        protected ImmutableHashSet<string> UnresolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet;

        /// <summary>
        /// The set of rule names that represent resolved references.
        /// </summary>
        protected ImmutableHashSet<string> ResolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet;

        public event EventHandler<DependenciesChangedEventArgs> DependenciesChanged;

        private IDependencyNode _rootNode;
        public IDependencyNode RootNode
        {
            get
            {
                if (_rootNode == null)
                {
                    EnsureInitialized();
                    _rootNode = CreateRootNode();
                }

                return _rootNode;
            }
            protected set
            {
                _rootNode = value;
            }
        }

        /// <summary>
        /// Specifies if dependency sub node thinks that it is in error state. Different sub nodes
        /// can have different conditions for error state.
        /// </summary>
        public virtual bool IsInErrorState
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Allows sub node provider to explicitly hide it's node when needed
        /// </summary>
        public bool ShouldBeVisibleWhenEmpty
        {
            get
            {
                return false;
            }
        }

        public abstract string ProviderType { get; }

        public abstract IEnumerable<ImageMoniker> Icons { get; }

        /// <summary>
        /// Creates a root node specific to provider implementation
        /// </summary>
        /// <returns></returns>
        protected abstract IDependencyNode CreateRootNode();

        /// <summary>
        /// Creates a specific node type for given provider
        /// </summary>
        protected virtual IDependencyNode CreateDependencyNode(string itemSpec,
                                                               string itemType,
                                                               int priority = 0,
                                                               IImmutableDictionary<string, string> properties = null,
                                                               bool resolved = true)
        {
            // Note: it is not abstract since not all providers would need to implement it
            throw new NotImplementedException();
        }

        protected virtual string OriginalItemSpecPropertyName
        {
            get
            {
                return ResolvedProjectReference.OriginalItemSpecProperty;
            }
        }

        protected override void Initialize()
        {
            _evaluationSubscriptionLinks = new List<IDisposable>();
            _projectSyncLinks = new List<IDisposable>();

            using (UnconfiguredProjectAsynchronousTasksService.LoadedProject())
            {
                // this.IsApplicable may take a project lock, so we can't do it inline with this method
                // which is holding a private lock.  It turns out that doing it asynchronously isn't a problem anyway,
                // so long as we guard against races with the Dispose method.
                UnconfiguredProjectAsynchronousTasksService.LoadedProjectAsync(
                    async delegate
                    {
                        await TaskScheduler.Default.SwitchTo(alwaysYield: true);
                        UnconfiguredProjectAsynchronousTasksService.
                            UnloadCancellationToken.ThrowIfCancellationRequested();

                        lock (SyncObject)
                        {
                            Verify.NotDisposed(this);

                            var intermediateBlockDesignTime = new BufferBlock<
                                                            IProjectVersionedValue<
                                                                IProjectSubscriptionUpdate>>();

                            _evaluationSubscriptionLinks.Add(ProjectSubscriptionService.JointRuleSource.SourceBlock.LinkTo(
                                intermediateBlockDesignTime,
                                ruleNames: UnresolvedReferenceRuleNames.Union(ResolvedReferenceRuleNames),
                                suppressVersionOnlyUpdates: true));

                            var actionBlock = new ActionBlock<
                                                    IProjectVersionedValue<
                                                        Tuple<IProjectSubscriptionUpdate,
                                                               IProjectCatalogSnapshot,
                                                               IProjectSharedFoldersSnapshot>>>
                                                  (new Action<
                                                        IProjectVersionedValue<
                                                            Tuple<IProjectSubscriptionUpdate,
                                                                  IProjectCatalogSnapshot,
                                                                  IProjectSharedFoldersSnapshot>>>(
                                                        ProjectSubscriptionService_Changed),
                                                   new ExecutionDataflowBlockOptions()
                                                   {
                                                       NameFormat = "ReferencesSubtree Input: {1}"
                                                   });

                            _projectSyncLinks.Add(ProjectDataSources.SyncLinkTo(
                                intermediateBlockDesignTime.SyncLinkOptions(),
                                ProjectSubscriptionService.ProjectCatalogSource.SourceBlock.SyncLinkOptions(),
                                ProjectSubscriptionService.SharedFoldersSource.SourceBlock.SyncLinkOptions(),
                                actionBlock));

                            var intermediateBlockEvaluation = new BufferBlock<
                                IProjectVersionedValue<
                                    IProjectSubscriptionUpdate>>();
                            _evaluationSubscriptionLinks.Add(ProjectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                                intermediateBlockEvaluation,
                                ruleNames: UnresolvedReferenceRuleNames,
                                suppressVersionOnlyUpdates: true));

                            _projectSyncLinks.Add(ProjectDataSources.SyncLinkTo(
                                intermediateBlockEvaluation.SyncLinkOptions(),
                                ProjectSubscriptionService.ProjectCatalogSource.SourceBlock.SyncLinkOptions(),
                                ProjectSubscriptionService.SharedFoldersSource.SourceBlock.SyncLinkOptions(),
                                actionBlock));

                        }
                    },
                    registerFaultHandler: true);
            }
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _evaluationSubscriptionLinks.ForEach(x => x?.Dispose());
                _evaluationSubscriptionLinks.Clear();

                _projectSyncLinks.ForEach(x => x?.Dispose());
                _projectSyncLinks.Clear();
            }
        }

        /// <summary>
        /// Handles changes to the references items in the project and updates the project tree.
        /// </summary>
        /// <param name="e">A description of the changes made to the project.</param>
        private void ProjectSubscriptionService_Changed(IProjectVersionedValue<
                                                            Tuple<IProjectSubscriptionUpdate,
                                                                  IProjectCatalogSnapshot,
                                                                  IProjectSharedFoldersSnapshot>> e)
        {
            DependenciesChange dependenciesChange;

            lock (_rootNodeSync)
            {
                dependenciesChange = ProcessDependenciesChanges(e.Value.Item1, e.Value.Item2);

                // process separatelly shared projects changes
                ProcessSharedProjectImportNodes(e.Value.Item3, dependenciesChange);

                // Apply dependencies changes to actual RootNode children collection
                // remove first nodes from actual RootNode
                dependenciesChange.RemovedNodes.ForEach(RootNode.RemoveChild);

                ProcessDuplicatedNodes(dependenciesChange);

                dependenciesChange.UpdatedNodes.ForEach((topLevelNode) =>
                {
                    var oldNode = RootNode.Children.FirstOrDefault(x => x.Id.Equals(topLevelNode.Id));
                    if (oldNode != null)
                    {
                        RootNode.RemoveChild(oldNode);
                        RootNode.AddChild(topLevelNode);
                    }
                });

                dependenciesChange.AddedNodes.ForEach(RootNode.AddChild);
            }

            OnDependenciesChanged(dependenciesChange.GetDiff(), e);
        }

        public virtual IDependencyNode GetDependencyNode(DependencyNodeId nodeId)
        {
            Requires.NotNull(nodeId, nameof(nodeId));

            return RootNode.Children.FirstOrDefault(x => x.Id.Equals(nodeId));
        }

        public virtual Task<IEnumerable<IDependencyNode>> SearchAsync(IDependencyNode node, string searchTerm)
        {
            return Task.FromResult<IEnumerable<IDependencyNode>>(null);
        }

        protected virtual DependenciesChange ProcessDependenciesChanges(
                                                    IProjectSubscriptionUpdate projectSubscriptionUpdate,
                                                    IProjectCatalogSnapshot catalogs)
        {
            var changes = projectSubscriptionUpdate.ProjectChanges;
            var resolvedReferenceChanges =
                ResolvedReferenceRuleNames.Where(x => changes.Keys.Contains(x))
                                          .Select(ruleName => changes[ruleName]).ToImmutableHashSet();
                
            var unresolvedReferenceSnapshots = changes.Values
                .Where(cd => !ResolvedReferenceRuleNames.Any(ruleName =>
                                string.Equals(ruleName,
                                              cd.After.RuleName,
                                              StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(d => d.After.RuleName, d => d, StringComparer.OrdinalIgnoreCase);

            var rootTreeNodes = new HashSet<IDependencyNode>(RootNode.Children);
            var dependenciesChange = new DependenciesChange();
            foreach (var unresolvedChange in unresolvedReferenceSnapshots.Values)
            {
                if (!unresolvedChange.Difference.AnyChanges)
                {
                    continue;
                }

                var itemType = GetItemTypeFromRuleName(unresolvedChange.After.RuleName,
                                                                 catalogs,
                                                                 true);
                if (itemType == null)
                {
                    // We must be missing that rule. Skip it.
                    continue;
                }

                foreach (string removedItemSpec in unresolvedChange.Difference.RemovedItems)
                {
                    var node = rootTreeNodes.FindNode(removedItemSpec, itemType);
                    if (node != null)
                    {
                        dependenciesChange.RemovedNodes.Add(node);
                    }
                }

                foreach (string addedItemSpec in unresolvedChange.Difference.AddedItems)
                {
                    var node = rootTreeNodes.FindNode(addedItemSpec, itemType);
                    if (node == null)
                    {
                        var properties = GetProjectItemProperties(unresolvedChange.After, addedItemSpec);
                        node = CreateDependencyNode(addedItemSpec,
                                                    itemType,
                                                    properties: properties,
                                                    resolved: false);
                        dependenciesChange.AddedNodes.Add(node);
                    }
                }
            }

            var updatedUnresolvedSnapshots = unresolvedReferenceSnapshots.Values.Select(cd => cd.After);
            foreach (var resolvedReferenceRuleChanges in resolvedReferenceChanges)
            {
                if (!resolvedReferenceRuleChanges.Difference.AnyChanges)
                {
                    continue;
                }

                // if resolved reference appears in Removed list, it means that it is either removed from
                // project or can not be resolved anymore. In case when it can not be resolved,
                // we must remove old "resolved" node and add new unresolved node with corresponding 
                // properties changes (rules, icon, etc)
                // Note: removed resolved node is not added to "added unresolved diff", which we process
                // above, thus we need to do this properties update here. It is just cleaner to re-add node
                // instead of modifying properties.
                foreach (string removedItemSpec in resolvedReferenceRuleChanges.Difference.RemovedItems)
                {
                    string unresolvedItemSpec = resolvedReferenceRuleChanges.Before
                                                    .Items[removedItemSpec][OriginalItemSpecPropertyName];
                    IProjectRuleSnapshot unresolvedReferenceSnapshot = null;
                    string unresolvedItemType = GetUnresolvedReferenceItemType(unresolvedItemSpec,
                                                                            updatedUnresolvedSnapshots,
                                                                            catalogs,
                                                                            out unresolvedReferenceSnapshot);
                    var node = rootTreeNodes.FindNode(removedItemSpec, unresolvedItemType);
                    if (node != null)
                    {
                        dependenciesChange.RemovedNodes.Add(node);

                        IImmutableDictionary<string, string> properties = null;
                        if (unresolvedReferenceSnapshot != null)
                        {
                            properties = GetProjectItemProperties(unresolvedReferenceSnapshot, unresolvedItemSpec);
                        }

                        node = CreateDependencyNode(unresolvedItemSpec,
                                                    unresolvedItemType,
                                                    properties: properties,
                                                    resolved: false);
                        dependenciesChange.AddedNodes.Add(node);
                    }
                }

                foreach (string addedItemSpec in resolvedReferenceRuleChanges.Difference.AddedItems)
                {
                    var properties = GetProjectItemProperties(resolvedReferenceRuleChanges.After, addedItemSpec);
                    if (properties == null || !properties.Keys.Contains(OriginalItemSpecPropertyName))
                    {
                        // if there no OriginalItemSpec, we can not associate item with the rule
                        continue;
                    }

                    var originalItemSpec = properties[OriginalItemSpecPropertyName];
                    IProjectRuleSnapshot unresolvedReferenceSnapshot = null;
                    var itemType = GetUnresolvedReferenceItemType(originalItemSpec,
                                                                  updatedUnresolvedSnapshots,
                                                                  catalogs,
                                                                  out unresolvedReferenceSnapshot);
                    if (string.IsNullOrEmpty(itemType))
                    {
                        // Note: design time build resolves not only our unresolved assemblies, but also
                        // all transitive assembly dependencies, which ar enot direct references and 
                        // we should not show them. If reference does not have an unresolved reference
                        // corresponded to it, i.e. itemType = null here - we skip it.
                        continue;
                    }

                    // avoid adding unresolved dependency along with resolved one 
                    var existingUnresolvedNode = dependenciesChange.AddedNodes.FindNode(originalItemSpec, itemType);
                    if (existingUnresolvedNode != null)
                    {
                        dependenciesChange.AddedNodes.Remove(existingUnresolvedNode);
                    }

                    // if unresolved dependency was added earlier, remove it, since it will be substituted by resolved one
                    existingUnresolvedNode = rootTreeNodes.FindNode(originalItemSpec, itemType);
                    if (existingUnresolvedNode != null)
                    {
                        dependenciesChange.RemovedNodes.Add(existingUnresolvedNode);
                    }

                    var newNode = CreateDependencyNode(originalItemSpec,
                                                    itemType: itemType,
                                                    properties: properties);
                    dependenciesChange.AddedNodes.Add(newNode);
                }
            }

            return dependenciesChange;
        }

        /// <summary>
        /// Updates the shared project import nodes that are shown under the 'Dependencies/Projects' node.
        /// </summary>
        /// <param name="sharedFolders">Snapshot of shared folders.</param>
        /// <param name="dependenciesChange"></param>
        /// <returns></returns>
        protected virtual void ProcessSharedProjectImportNodes(IProjectSharedFoldersSnapshot sharedFolders,
                                                               DependenciesChange dependenciesChange)
        {
            // does nothing by default
        }

        protected virtual void ProcessDuplicatedNodes(DependenciesChange dependenciesChange)
        {
            // Now add new nodes and dedupe any nodes that might have same caption.
            // For dedupping we apply aliases to all nodes with similar Caption. Alias 
            // is a "Caption (ItemSpec)" and is unique. We try to find existing node with 
            // with the same caption, if found we apply alias to both nodes. If not found 
            // we also check if there are nodes with alias already applied earlier and having 
            // same caption. If yes, we just need to apply alias to our current node only.
            foreach (var nodeToAdd in dependenciesChange.AddedNodes)
            {
                var shouldApplyAlias = false;
                var matchingChild = RootNode.Children.FirstOrDefault(
                        x => x.Caption.Equals(nodeToAdd.Caption, StringComparison.OrdinalIgnoreCase));
                if (matchingChild == null)
                {
                    shouldApplyAlias = RootNode.Children.Any(
                        x => x.Caption.Equals(
                                string.Format(CultureInfo.CurrentCulture, "{0} ({1})", nodeToAdd.Caption, x.Id.ItemSpec),
                                StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    shouldApplyAlias = true;
                }

                if (shouldApplyAlias)
                {
                    if (matchingChild != null)
                    {
                        ((DependencyNode)matchingChild).Caption = matchingChild.Alias;
                        dependenciesChange.UpdatedNodes.Add(matchingChild);
                    }

                    ((DependencyNode)nodeToAdd).Caption = nodeToAdd.Alias;
                }

                RootNode.Children.Add(nodeToAdd);
            }
        }

        private string GetUnresolvedReferenceItemType(
                    string unresolvedItemSpec,
                    IEnumerable<IProjectRuleSnapshot> unresolvedReferenceSnapshots,
                    IProjectCatalogSnapshot catalogs,
                    out IProjectRuleSnapshot unresolvedReferenceSnapshot)
        {
            Requires.NotNull(unresolvedItemSpec, nameof(unresolvedItemSpec));
            Requires.NotNull(unresolvedReferenceSnapshots, nameof(unresolvedReferenceSnapshots));
            Requires.NotNull(catalogs, nameof(catalogs));

            unresolvedReferenceSnapshot = null;
            foreach (var referenceSnapshot in unresolvedReferenceSnapshots)
            {
                if (referenceSnapshot.Items.ContainsKey(unresolvedItemSpec))
                {
                    var itemType = GetItemTypeFromRuleName(referenceSnapshot.RuleName, catalogs, true);
                    if (itemType != null)
                    {
                        unresolvedReferenceSnapshot = referenceSnapshot;
                        return itemType;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the resolved reference item for a given unresolved reference.
        /// </summary>
        /// <param name="projectRuleSnapshot">Resolved reference project items snapshot to search.</param>
        /// <param name="unresolvedItemSpec">The unresolved reference item name.</param>
        /// <returns>The key is item name and the value is the metadata dictionary.</returns>
        protected static IImmutableDictionary<string, string> GetProjectItemProperties(
                            IProjectRuleSnapshot projectRuleSnapshot,
                            string unresolvedItemSpec)
        {
            Contract.Requires(projectRuleSnapshot != null);
            Contract.Requires(!string.IsNullOrEmpty(unresolvedItemSpec));

            foreach (var item in projectRuleSnapshot.Items)
            {
                string originalItemSpec = item.Key;
                if (string.Equals(originalItemSpec, unresolvedItemSpec, StringComparison.OrdinalIgnoreCase))
                {
                    return item.Value;
                }
            }

            return null;
        }

        protected void OnDependenciesChanged(IDependenciesChangeDiff changes,
                                             IProjectVersionedValue<
                                                Tuple<IProjectSubscriptionUpdate,
                                                        IProjectCatalogSnapshot,
                                                        IProjectSharedFoldersSnapshot>> e)
        {
            DependenciesChanged(this,
                                new DependenciesChangedEventArgs(
                                        this,
                                        changes,
                                        e?.Value?.Item2,
                                        e?.DataSourceVersions));
        }

        /// <summary>
        /// Gets the item type for a given rule.
        /// </summary>
        /// <param name="ruleName">The name of the rule to get an item type for.</param>
        /// <param name="catalogs">The catalog snapshot to use to find the item type.</param>
        /// <param name="allowNull">Whether or not to allow a null result for a missing or malformed rule.</param>
        /// <returns>The matching item type.</returns>
        public string GetItemTypeFromRuleName(string ruleName,
                                              IProjectCatalogSnapshot catalogs,
                                              bool allowNull = false)
        {
            Requires.NotNullOrEmpty(ruleName, nameof(ruleName));
            Requires.NotNull(catalogs, nameof(catalogs));

            string itemType;
            lock (_syncObject)
            {
                if (!_ruleNameToItemType.TryGetValue(ruleName, out itemType))
                {
                    var rule = catalogs.GetSchema(PropertyPageContexts.Project, ruleName)
                        ?? catalogs.GetSchema(PropertyPageContexts.File, ruleName);
                    itemType = rule != null ? rule.DataSource.ItemType : null;

                    if (itemType != null)
                    {
                        _ruleNameToItemType[ruleName] = itemType;
                        _itemTypeToRuleName[itemType] = ruleName;
                    }
                }
            }

            ProjectErrorUtilities.VerifyThrowProjectException(
                itemType != null || allowNull, VSResources.NoItemTypeForRule, ruleName);

            return itemType;
        }

        /// <summary>
        /// Gets the rule name for an item type.
        /// </summary>
        /// <param name="itemType">The item type to get a rule name for.</param>
        /// <param name="catalogs">The catalog snapshot to use to find the rule name.</param>
        /// <param name="allowNull">Whether or not to allow a null result for a missing or malformed rule.</param>
        /// <returns>The matching rule name.</returns>
        public string GetRuleNameFromItemType(string itemType,
                                              IProjectCatalogSnapshot catalogs,
                                              bool allowNull = false)
        {
            Requires.NotNullOrEmpty(itemType, nameof(itemType));
            Requires.NotNull(catalogs, nameof(catalogs));

            string ruleName;
            lock (_syncObject)
            {
                if (!_itemTypeToRuleName.TryGetValue(itemType, out ruleName))
                {
                    ruleName = GetRuleNameByItemType(catalogs, PropertyPageContexts.Project, itemType)
                                ?? GetRuleNameByItemType(catalogs, PropertyPageContexts.File, itemType);

                    if (ruleName != null)
                    {
                        _ruleNameToItemType[ruleName] = itemType;
                        _itemTypeToRuleName[itemType] = ruleName;
                    }
                }
            }

            ProjectErrorUtilities.VerifyThrowProjectException(ruleName != null || allowNull,
                                                              VSResources.NoItemTypeForRule,
                                                              itemType);

            return ruleName;
        }

        /// <summary>
        /// Returns all rule names from the snapshot that match an item type.
        /// </summary>
        /// <remarks>
        /// In the current CPS world, an item type will only match one rule name at most. This may change in the future.
        /// </remarks>
        private static string GetRuleNameByItemType(IProjectCatalogSnapshot snapshot,
                                                    string catalogName,
                                                    string itemType)
        {
            Requires.NotNull(snapshot, nameof(snapshot));
            Requires.NotNull(catalogName, nameof(catalogName));
            Requires.NotNullOrEmpty(itemType, nameof(itemType));

            IPropertyPagesCatalog catalog;
            if (snapshot.NamedCatalogs.TryGetValue(catalogName, out catalog))
            {
                return catalog.GetPropertyPagesSchemas(itemType).FirstOrDefault();
            }
            else
            {
                return null;
            }
        }
    }
}
