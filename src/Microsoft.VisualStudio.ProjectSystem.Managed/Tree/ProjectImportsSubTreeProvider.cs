// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.Tree
{
    /// <summary>
    /// Provides a top-level project sub-tree showing the tree of MSBuild project imports,
    /// including props/targets from the SDK, targets, etc.
    /// </summary>
    /// <remarks>
    /// <para>
    /// To reduce clutter and memory usage, this tree is only visible (and populated) when
    /// the "Show all Files" feature of Solution Explorer is enabled.
    /// </para>
    /// <para>
    /// This feature can be enabled and disabled via the <c>ProjectImportsTree</c> project capability.
    /// </para>
    /// </remarks>
    [Export(ExportContractNames.ProjectTreeProviders.PhysicalViewRootGraft, typeof(IProjectTreeProvider))]
    [AppliesTo(ProjectCapability.ProjectImportsTree)]
    internal sealed partial class ProjectImportsSubTreeProvider : ProjectTreeProviderBase, IProjectTreeProvider, IShowAllFilesProjectTreeProvider
    {
        private static readonly ProjectImageMoniker s_rootIcon = ManagedImageMonikers.ProjectImports.ToProjectSystemType();
        private static readonly ProjectImageMoniker s_nodeIcon = ManagedImageMonikers.TargetFile.ToProjectSystemType();
        private static readonly ProjectImageMoniker s_nodeImplicitIcon = ManagedImageMonikers.TargetFilePrivate.ToProjectSystemType();

        public static ProjectTreeFlags ProjectImport { get; } = ProjectTreeFlags.Create("ProjectImport");
        public static ProjectTreeFlags ProjectImportImplicit { get; } = ProjectTreeFlags.Create("ProjectImportImplicit");

        private static readonly ProjectTreeFlags s_projectImportsTreeRootFlags = ProjectTreeFlags.Create(
            ProjectTreeFlags.Common.BubbleUp |              // sort to top of tree, not alphabetically
            ProjectTreeFlags.Common.VirtualFolder |
            ProjectTreeFlags.Common.DisableAddItemFolder);

        private static readonly ProjectTreeFlags s_projectImportFlags = ProjectImport | ProjectTreeFlags.FileOnDisk;
        private static readonly ProjectTreeFlags s_projectImportImplicitFlags = s_projectImportFlags + ProjectImportImplicit;

        private readonly ImplicitProjectCheck _importPathCheck = new ImplicitProjectCheck();
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;
        private readonly IUnconfiguredProjectTasksService _unconfiguredProjectTasksService;

        private DisposableBag? _subscriptions;
        private bool _showAllFiles;

        [ImportingConstructor]
        internal ProjectImportsSubTreeProvider(
            IProjectThreadingService threadingService,
            IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
            IUnconfiguredProjectTasksService unconfiguredProjectTasksService,
            UnconfiguredProject unconfiguredProject)
            : base(threadingService, unconfiguredProject)
        {
            _projectSubscriptionService = projectSubscriptionService;
            _unconfiguredProjectTasksService = unconfiguredProjectTasksService;
        }

        public bool ShowAllFiles
        {
            get => _showAllFiles;
            set
            {
                // Ensure we no-op if the value didn't change, avoiding race conditions
                lock (SyncObject)
                {
                    if (_showAllFiles == value)
                    {
                        return;
                    }

                    _showAllFiles = value;
                }

                ToggleTree();

                return;

                void ToggleTree()
                {
                    // Queue up an operation that will toggle the import tree
                    _unconfiguredProjectTasksService.LoadedProjectAsync(
                        async () =>
                        {
                            await TaskScheduler.Default.SwitchTo(alwaysYield: true);

                            _unconfiguredProjectTasksService.UnloadCancellationToken.ThrowIfCancellationRequested();

                            lock (SyncObject)
                            {
                                Verify.NotDisposed(this);

                                // Use the presence or absence of a subscription to indicate which operation we are performing here.
                                // This avoids a race condition between the (locked) changing of _showAllFiles and the (locked) changing
                                // of _subscriptions. Even if there is a race, the right number of toggles will occur and the end result
                                // will be correct.
                                if (_subscriptions == null)
                                {
                                    SetUpTree();
                                }
                                else
                                {
                                    TearDownTree();
                                }
                            }
                        });

                    return;

                    void SetUpTree()
                    {
                        Assumes.Null(_subscriptions);

                        // Set a visible root
                        SubmitTreeUpdateAsync(
                            (currentTree, configuredProjectExports, token) =>
                            {
                                // Update (make visible) or create a new tree if no prior one exists
                                IProjectTree tree = currentTree == null
                                    ? NewTree(Resources.ImportsTreeNodeName, icon: s_rootIcon, flags: s_projectImportsTreeRootFlags)
                                    : currentTree.Value.Tree.SetVisible(true);

                                return Task.FromResult(new TreeUpdateResult(tree));
                            });

                        // Subscribe to data to populate the tree and keep it updated with changes
                        IReceivableSourceBlock<IProjectVersionedValue<IProjectImportTreeSnapshot>> importTreeSource
                            = _projectSubscriptionService.ImportTreeSource.SourceBlock;

                        IReceivableSourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> projectRuleSource
                            = _projectSubscriptionService.ProjectRuleSource.SourceBlock;

                        var intermediateBlock =
                            new BufferBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                                new ExecutionDataflowBlockOptions
                                {
                                    NameFormat = "Import Tree Intermediate: {1}"
                                });

                        _subscriptions ??= new DisposableBag();

                        _subscriptions.Add(
                            projectRuleSource.LinkTo(
                                intermediateBlock,
                                ruleNames: ConfigurationGeneral.SchemaName,
                                suppressVersionOnlyUpdates: false,
                                linkOptions: DataflowOption.PropagateCompletion));

                        ITargetBlock<IProjectVersionedValue<ValueTuple<IProjectImportTreeSnapshot, IProjectSubscriptionUpdate>>> actionBlock =
                            DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<ValueTuple<IProjectImportTreeSnapshot, IProjectSubscriptionUpdate>>>(
                                SyncTree,
                                new ExecutionDataflowBlockOptions
                                {
                                    NameFormat = "Import Tree Action: {1}"
                                });

                        using (TrySuppressExecutionContextFlow())
                        {
                            _subscriptions.Add(ProjectDataSources.SyncLinkTo(
                                importTreeSource.SyncLinkOptions(),
                                intermediateBlock.SyncLinkOptions(),
                                actionBlock,
                                linkOptions: DataflowOption.PropagateCompletion));
                        }

                        JoinUpstreamDataSources(_projectSubscriptionService.ImportTreeSource);

                        return;

                        void SyncTree(IProjectVersionedValue<ValueTuple<IProjectImportTreeSnapshot, IProjectSubscriptionUpdate>> e)
                        {
                            if (e.Value.Item2.CurrentState.TryGetValue(ConfigurationGeneral.SchemaName, out IProjectRuleSnapshot snapshot) &&
                                snapshot.Properties.TryGetValue(ConfigurationGeneral.MSBuildProjectExtensionsPathProperty, out string projectExtensionsPath))
                            {
                                _importPathCheck.ProjectExtensionsPath = projectExtensionsPath;
                            }

                            SubmitTreeUpdateAsync(
                                (currentTree, configuredProjectExports, token) =>
                                {
                                    IProjectTree updatedTree = SyncNode(
                                        imports: e.Value.Item1.Value,
                                        tree: currentTree.Value.Tree);

                                    return Task.FromResult(new TreeUpdateResult(updatedTree, e.DataSourceVersions));
                                });

                            return;

                            IProjectTree SyncNode(IReadOnlyList<IProjectImportSnapshot> imports, IProjectTree tree)
                            {
                                var existingChildByPath = new Dictionary<string, IProjectTree>(StringComparers.Paths);

                                foreach (IProjectTree existingNode in tree.Children)
                                {
                                    Assumes.NotNullOrEmpty(existingNode.FilePath);

                                    if (!imports.Any(import => StringComparers.Paths.Equals(import.ProjectPath, existingNode.FilePath)))
                                    {
                                        // Remove child that's no longer present
                                        if (tree.TryFind(existingNode.Identity, out IProjectTree? child))
                                        {
                                            tree = child.Remove();
                                        }
                                    }
                                    else
                                    {
                                        existingChildByPath[existingNode.FilePath] = existingNode;
                                    }
                                }

                                foreach (IProjectImportSnapshot import in imports)
                                {
                                    if (!existingChildByPath.TryGetValue(import.ProjectPath, out IProjectTree child))
                                    {
                                        // No child exists for this import, so add it
                                        bool isImplicit = _importPathCheck.IsImplicit(import.ProjectPath);
                                        ProjectTreeFlags flags = isImplicit ? s_projectImportImplicitFlags : s_projectImportFlags;
                                        ProjectImageMoniker icon = isImplicit ? s_nodeImplicitIcon : s_nodeIcon;

                                        IProjectTree newChild = NewTree(
                                            Path.GetFileName(import.ProjectPath),
                                            filePath: import.ProjectPath,
                                            flags: flags,
                                            icon: icon);

                                        // Recur down the tree
                                        newChild = SyncNode(import.Imports, newChild);

                                        tree = tree.Add(newChild).Parent!;
                                    }
                                    else
                                    {
                                        // Child exists, so continue walking tree
                                        IProjectTree newChild = SyncNode(import.Imports, child);

                                        if (!ReferenceEquals(child, newChild))
                                        {
                                            tree = tree.Remove(child).Add(newChild).Parent!;
                                        }
                                    }
                                }

                                return tree;
                            }
                        }

                        static IDisposable TrySuppressExecutionContextFlow()
                        {
                            return ExecutionContext.IsFlowSuppressed()
                                ? EmptyDisposable.Instance
                                : ExecutionContext.SuppressFlow();
                        }
                    }

                    void TearDownTree()
                    {
                        Assumes.NotNull(_subscriptions);
                        _subscriptions.Dispose();
                        _subscriptions = null;

                        SubmitTreeUpdateAsync(
                            (currentTree, configuredProjectExports, token) =>
                            {
                                IProjectTree tree = currentTree.Value.Tree;

                                // Set invisible
                                tree = tree.SetVisible(false);

                                // Remove all children
                                while (tree.Children.Count != 0)
                                {
                                    tree = tree.Children[0].Remove();
                                }

                                return Task.FromResult(new TreeUpdateResult(tree));
                            });
                    }
                }
            }
        }

        // CPS's physical tree provider currently only uses the IShowAllFilesProjectTreeProvider.ShowAllFiles property
        // for graft root providers. These other interface methods have safe no-op implementations in case that changes
        // one day.

        bool IShowAllFilesProjectTreeProvider.CanIncludeItems(IImmutableSet<IProjectTree> nodes)   => true;
        bool IShowAllFilesProjectTreeProvider.CanExcludeItems(IImmutableSet<IProjectTree> nodes)   => true;
        Task IShowAllFilesProjectTreeProvider.IncludeItemsAsync(IImmutableSet<IProjectTree> nodes) => Task.CompletedTask;
        Task IShowAllFilesProjectTreeProvider.ExcludeItemsAsync(IImmutableSet<IProjectTree> nodes) => Task.CompletedTask;

        protected override ConfiguredProjectExports GetActiveConfiguredProjectExports(ConfiguredProject newActiveConfiguredProject)
        {
            Requires.NotNull(newActiveConfiguredProject, nameof(newActiveConfiguredProject));

            return GetActiveConfiguredProjectExports<MyConfiguredProjectExports>(newActiveConfiguredProject);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _subscriptions?.Dispose();
            }

            base.Dispose(disposing);
        }

        [Export]
        private sealed class MyConfiguredProjectExports : ConfiguredProjectExports
        {
            [ImportingConstructor]
            public MyConfiguredProjectExports(ConfiguredProject configuredProject)
                : base(configuredProject)
            {
            }
        }
    }
}
