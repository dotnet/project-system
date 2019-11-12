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
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

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
    [AppliesTo(Capability)]
    internal sealed class ProjectImportsSubTreeProvider : ProjectTreeProviderBase, IProjectTreeProvider, IShowAllFilesProjectTreeProvider
    {
        public const string Capability = "ProjectImportsTree";
        private const string RootNodeCaption = "Imports";

        private static readonly ProjectImageMoniker s_rootIcon = KnownMonikers.ImportSettings.ToProjectSystemType();
        private static readonly ProjectImageMoniker s_nodeIcon = KnownMonikers.TargetFile.ToProjectSystemType();

        private static ProjectTreeFlags ProjectImportsTreeRootFlags { get; } = ProjectTreeFlags.Create(
            ProjectTreeFlags.Common.BubbleUp |              // sort to top of tree, not alphabetically
            ProjectTreeFlags.Common.VirtualFolder |
            ProjectTreeFlags.Common.DisableAddItemFolder);
        
        private static ProjectTreeFlags ProjectImportFlags { get; } = ProjectTreeFlags.Create(
            ProjectTreeFlags.Common.SourceFile |
            ProjectTreeFlags.Common.NonMemberItem);         // enable double-click/enter to edit

        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;
        private readonly IUnconfiguredProjectTasksService _unconfiguredProjectTasksService;

        private IDisposable? _subscription;
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
                                // of _subscription. Even if there is a race, the right number of toggles will occur and the end result
                                // will be correct.
                                if (_subscription == null)
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
                        Assumes.Null(_subscription);

                        // Set a visible root
                        SubmitTreeUpdateAsync(
                            (currentTree, a, c) =>
                            {
                                IProjectTree tree;
                                if (currentTree == null)
                                {
                                    // Create a new tree as no prior one exists
                                    tree = NewTree(RootNodeCaption, icon: s_rootIcon, flags: ProjectImportsTreeRootFlags);
                                }
                                else
                                {
                                    // Reuse existing tree, but make it visible
                                    tree = currentTree.Value.Tree.SetVisible(true);
                                }

                                return Task.FromResult(new TreeUpdateResult(tree));
                            });

                        // Subscribe to data to populate the tree and keep it updated with changes
                        ITargetBlock<IProjectVersionedValue<IProjectImportTreeSnapshot>> actionBlock
                            = DataflowBlockSlim.CreateActionBlock(
                                new Action<IProjectVersionedValue<IProjectImportTreeSnapshot>>(SyncTree));

                        IReceivableSourceBlock<IProjectVersionedValue<IProjectImportTreeSnapshot>> importTreeSource
                            = _projectSubscriptionService.ImportTreeSource.SourceBlock;

                        using (TrySuppressExecutionContextFlow())
                        {
                            _subscription = importTreeSource.LinkTo(actionBlock, DataflowOption.PropagateCompletion);
                        }

                        JoinUpstreamDataSources(_projectSubscriptionService.ImportTreeSource);

                        return;

                        void SyncTree(IProjectVersionedValue<IProjectImportTreeSnapshot> e)
                        {
                            SubmitTreeUpdateAsync(
                                (treeSnapshot, configuredProjectExports, ct) =>
                                {
                                    IProjectTree updatedTree = SyncNode(
                                        imports: e.Value.Value,
                                        tree: treeSnapshot.Value.Tree);

                                    return Task.FromResult(new TreeUpdateResult(updatedTree, e.DataSourceVersions));
                                });

                            return;

                            IProjectTree SyncNode(IReadOnlyList<IProjectImportSnapshot> imports, IProjectTree tree)
                            {
                                var knownImportPaths = new HashSet<string?>(StringComparers.Paths);

                                // Process removals
                                foreach (IProjectTree existingNode in tree.Children)
                                {
                                    if (!imports.Any(import => StringComparers.Paths.Equals(import.ProjectPath, existingNode.FilePath)))
                                    {
                                        if (tree.TryFind(existingNode.Identity, out IProjectTree? child))
                                        {
                                            tree = child.Remove();
                                        }
                                    }
                                    else
                                    {
                                        knownImportPaths.Add(existingNode.FilePath);
                                    }
                                }

                                // Process additions
                                foreach (IProjectImportSnapshot import in imports)
                                {
                                    if (!knownImportPaths.Contains(import.ProjectPath))
                                    {
                                        IProjectTree newChild = NewTree(
                                            Path.GetFileName(import.ProjectPath),
                                            filePath: import.ProjectPath,
                                            flags: ProjectImportFlags,
                                            icon: s_nodeIcon);

                                        // Recur down the tree
                                        newChild = SyncNode(import.Imports, newChild);

                                        tree = tree.Add(newChild).Parent!;
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
                        Assumes.NotNull(_subscription);
                        _subscription.Dispose();
                        _subscription = null;

                        SubmitTreeUpdateAsync(
                            (currentTree, a, c) =>
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
                _subscription?.Dispose();
            }

            base.Dispose(disposing);
        }

        [Export]
        [AppliesTo(Capability)]
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
