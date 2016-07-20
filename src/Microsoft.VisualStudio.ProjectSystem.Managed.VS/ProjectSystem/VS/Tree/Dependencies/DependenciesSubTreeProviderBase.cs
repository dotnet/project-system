// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
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
    internal class DependenciesSubTreeProviderBase : OnceInitializedOnceDisposed, IProjectDependenciesSubTreeProvider
    {
        /// <summary>
        /// Gets the unconfigured project.
        /// </summary>
        [Import]
        protected UnconfiguredProject UnconfiguredProject { get; private set; }

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

        private readonly TaskCompletionSource<ConfiguredProjectExports> _initialActiveConfiguredProjectExports 
                            = new TaskCompletionSource<ConfiguredProjectExports>();

        /// <summary>
        /// Gets a task that is complete when the first active configuration is known.
        /// </summary>
        protected Task InitialActiveConfiguredProjectAvailable
        {
            get { return _initialActiveConfiguredProjectExports.Task; }
        }

        /// <summary>
        /// The value to dispose to terminate the link providing evaluation snapshots.
        /// </summary>
        private IDisposable _projectSubscriptionLink;

        /// <summary>
        /// The value to dispose to terminate the SyncLinkTo subscription.
        /// </summary>
        private IDisposable _projectSyncLink;

        /// <summary>
        /// The set of rule names that represent unresolved references.
        /// </summary>
        protected ImmutableHashSet<string> UnresolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet;

        /// <summary>
        /// The set of rule names that represent resolved references.
        /// </summary>
        protected ImmutableHashSet<string> ResolvedReferenceRuleNames = Empty.OrdinalIgnoreCaseStringSet;

        protected override void Initialize()
        {
            if (_rootNode == null)
            {
                _rootNode = CreateRootNode();
            }

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

                            var intermediateBlock = new BufferBlock<
                                                            IProjectVersionedValue<
                                                                IProjectSubscriptionUpdate>>();

                            _projectSubscriptionLink = ProjectSubscriptionService.JointRuleSource.SourceBlock.LinkTo(
                                intermediateBlock,
                                ruleNames: UnresolvedReferenceRuleNames.Union(ResolvedReferenceRuleNames),
                                suppressVersionOnlyUpdates: false);

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

                            _projectSyncLink = ProjectDataSources.SyncLinkTo(
                                intermediateBlock.SyncLinkOptions(),
                                ProjectSubscriptionService.ProjectCatalogSource.SourceBlock.SyncLinkOptions(),
                                ProjectSubscriptionService.SharedFoldersSource.SourceBlock.SyncLinkOptions(),
                                actionBlock);
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
                _projectSubscriptionLink.DisposeIfNotNull();
                _projectSyncLink.DisposeIfNotNull();
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
            var changes = ProcessDependenciesChanges(e);

            OnDependenciesChanged(changes, e.Value.Item2);
        }

        #region IProjectDependenciesSubTreeProvider

        public virtual string ProviderType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private IDependencyNode _rootNode;
        public IDependencyNode RootNode
        {
            get
            {
                EnsureInitialized();
                return _rootNode;
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
        public bool ShouldBeVisible
        {
            get
            {
                return true;
            }
        }

        public virtual IEnumerable<ImageMoniker> Icons
        {
            get
            {
                return null;
            }
        }

        public virtual IDependencyNode GetDependencyNode(string nodeId)
        {
            throw new NotImplementedException();
        }

        public virtual Task<IEnumerable<IDependencyNode>> SearchAsync(string searchTerm)
        {
            return Task.FromResult<IEnumerable<IDependencyNode>>(null);
        }

        protected virtual IDependencyNode CreateRootNode()
        {
            throw new NotImplementedException();
        }

        protected virtual IDependenciesChangeDiff ProcessDependenciesChanges(
                                                    IProjectVersionedValue<
                                                        Tuple<IProjectSubscriptionUpdate,
                                                              IProjectCatalogSnapshot,
                                                              IProjectSharedFoldersSnapshot>> e)
        {
            throw new NotImplementedException();
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

        public event DependenciesChangedEventHandler DependenciesChanged;

        protected void OnDependenciesChanged(IDependenciesChangeDiff changes, IProjectCatalogSnapshot catalogs)
        {
            DependenciesChanged?.Invoke(this, new DependenciesChangedEventArgs(this, changes, catalogs));
        }

        #endregion

        protected abstract class ConfiguredProjectExports
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
            /// Initializes a new instance of the <see cref="ConfiguredProjectExports"/> class.
            /// </summary>
            protected ConfiguredProjectExports(ConfiguredProject configuredProject)
            {
            }

            /// <summary>
            /// Gets the property pages data model provider.
            /// </summary>
            [Import]
            public IPropertyPagesDataModelProvider PropertyPagesDataModelProvider { get; private set; }

            /// <summary>
            /// Gets the project asynchronous tasks service.
            /// </summary>
            [Import(ExportContractNames.Scopes.ConfiguredProject)]
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called by MEF.")]
            public IProjectAsynchronousTasksService ConfiguredProjectAsynchronousTasksService { get; private set; }

            /// <summary>
            /// Gets the configured project to which this tree provider applies.
            /// </summary>
            [Import]
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called by MEF.")]
            public ConfiguredProject ConfiguredProject { get; private set; }

            /// <summary>
            /// Gets the rule factory
            /// </summary>
            [Import]
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called by MEF.")]
            public IRuleFactory RuleFactory { get; private set; }

            /// <summary>
            /// Gets an IRule to attach to a project item so that browse object properties will be displayed.
            /// </summary>
            public IRule GetRuleForItem(IProjectPropertiesContext context, IProjectCatalogSnapshot catalogs)
            {
                Requires.NotNull(context, nameof(context));
                Requires.NotNull(catalogs, nameof(catalogs));

                var browseObjectCatalog = catalogs.NamedCatalogs[PropertyPageContexts.BrowseObject];
                var schemas = browseObjectCatalog.GetPropertyPagesSchemas(context.ItemType);
                if (schemas.Count == 1)
                {
                    return browseObjectCatalog.BindToContext(schemas.First(), context);
                }

                // Since we have no browse object, we still need to create *something* 
                // so that standard property pages can pop up.
                var emptyRule = RuleExtensions.SynthesizeEmptyRule(context.ItemType);
                return PropertyPagesDataModelProvider.GetRule(emptyRule, 
                                                              context.IsProjectFile ? null : context.File, 
                                                              context.ItemType, context.ItemName);
            }

            /// <summary>
            /// Gets the item type for a given rule.
            /// </summary>
            /// <param name="ruleName">The name of the rule to get an item type for.</param>
            /// <param name="catalogs">The catalog snapshot to use to find the item type.</param>
            /// <param name="allowNull">Whether or not to allow a null result for a missing or malformed rule.</param>
            /// <returns>The matching item type.</returns>
            [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Ignored")]
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
                    itemType != null || allowNull, Resources.NoItemTypeForRule, ruleName);

                return itemType;
            }

            /// <summary>
            /// Gets the rule name for an item type.
            /// </summary>
            /// <param name="itemType">The item type to get a rule name for.</param>
            /// <param name="catalogs">The catalog snapshot to use to find the rule name.</param>
            /// <param name="allowNull">Whether or not to allow a null result for a missing or malformed rule.</param>
            /// <returns>The matching rule name.</returns>
            [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Ignored")]
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
                                                                  Resources.NoItemTypeForRule, 
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
}
