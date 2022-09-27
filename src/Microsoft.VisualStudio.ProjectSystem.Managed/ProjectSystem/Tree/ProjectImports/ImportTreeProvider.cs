// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Collections;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.ProjectImports
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
    internal sealed class ImportTreeProvider : ProjectTreeProviderBase, IProjectTreeProvider, IShowAllFilesProjectTreeProvider
    {
        private static readonly ProjectImageMoniker s_rootIcon = KnownMonikers.ProjectImports.ToProjectSystemType();
        private static readonly ProjectImageMoniker s_nodeIcon = KnownMonikers.TargetFile.ToProjectSystemType();
        private static readonly ProjectImageMoniker s_nodeImplicitIcon = KnownMonikers.TargetFilePrivate.ToProjectSystemType();

        public static ProjectTreeFlags ProjectImport { get; } = ProjectTreeFlags.Create("ProjectImport");
        public static ProjectTreeFlags ProjectImportImplicit { get; } = ProjectTreeFlags.Create("ProjectImportImplicit");

        private static readonly ProjectTreeFlags s_projectImportsTreeRootFlags = ProjectTreeFlags.Create(
            ProjectTreeFlags.Common.BubbleUp |              // sort to top of tree, not alphabetically
            ProjectTreeFlags.Common.VirtualFolder |
            ProjectTreeFlags.Common.DisableAddItemFolder);

        private static readonly ProjectTreeFlags s_projectImportFlags = ProjectImport | ProjectTreeFlags.FileOnDisk | ProjectTreeFlags.FileSystemEntity;
        private static readonly ProjectTreeFlags s_projectImportImplicitFlags = s_projectImportFlags + ProjectImportImplicit;

        private readonly ProjectFileClassifier _projectFileClassifier = new();
        private readonly UnconfiguredProject _project;
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;
        private readonly IUnconfiguredProjectTasksService _unconfiguredProjectTasksService;

        private DisposableBag? _subscriptions;
        private bool _showAllFiles;

        [ImportingConstructor]
        internal ImportTreeProvider(
            IProjectThreadingService threadingService,
            UnconfiguredProject project,
            IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
            IUnconfiguredProjectTasksService unconfiguredProjectTasksService)
            : base(threadingService, project, useDisplayOrdering: true)
        {
            _project = project;
            _projectSubscriptionService = projectSubscriptionService;
            _unconfiguredProjectTasksService = unconfiguredProjectTasksService;
        }

        public override string? GetPath(IProjectTree node)
        {
            // Only process nodes belonging to our tree.
            // This test excludes the root which is fine as it doesn't have a file path.
            if (node.Flags.Contains(ProjectImport))
            {
                return node.FilePath;
            }

            return null;
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
                    _ = _unconfiguredProjectTasksService.LoadedProjectAsync(
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
                                if (_subscriptions is null)
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
                        _ = SubmitTreeUpdateAsync(
                            (currentTree, configuredProjectExports, token) =>
                            {
                                // Update (make visible) or create a new tree if no prior one exists
                                IProjectTree tree = currentTree is null
                                    ? NewTree(Resources.ImportsTreeNodeName, icon: s_rootIcon, flags: s_projectImportsTreeRootFlags)
                                    : currentTree.Value.Tree.SetVisible(true);

                                return Task.FromResult(new TreeUpdateResult(tree));
                            });

                        // Subscribe to data to populate the tree and keep it updated with changes
                        IReceivableSourceBlock<IProjectVersionedValue<IProjectImportTreeSnapshot>> importTreeSource
                            = _projectSubscriptionService.ImportTreeSource.SourceBlock;

                        IReceivableSourceBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>> projectRuleSource
                            = _projectSubscriptionService.ProjectRuleSource.SourceBlock;

                        IPropagatorBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>, IProjectVersionedValue<IProjectSubscriptionUpdate>> intermediateBlock
                            = DataflowBlockSlim.CreateSimpleBufferBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>("Import Tree Intermediate: {1}");

                        _subscriptions ??= new DisposableBag();

                        _subscriptions.Add(
                            projectRuleSource.LinkTo(
                                intermediateBlock,
                                ruleNames: ConfigurationGeneral.SchemaName,
                                suppressVersionOnlyUpdates: false,
                                linkOptions: DataflowOption.PropagateCompletion));

                        ITargetBlock<IProjectVersionedValue<ValueTuple<IProjectImportTreeSnapshot, IProjectSubscriptionUpdate>>> actionBlock =
                            DataflowBlockFactory.CreateActionBlock<IProjectVersionedValue<ValueTuple<IProjectImportTreeSnapshot, IProjectSubscriptionUpdate>>>(
                                SyncTree,
                                _project,
                                skipIntermediateInputData: true, // We can skip versions without breaking the tree
                                nameFormat: "Import Tree Action: {1}");

                        _subscriptions.Add(
                            ProjectDataSources.SyncLinkTo(
                                importTreeSource.SyncLinkOptions(),
                                intermediateBlock.SyncLinkOptions(),
                                actionBlock,
                                linkOptions: DataflowOption.PropagateCompletion));

                        JoinUpstreamDataSources(_projectSubscriptionService.ImportTreeSource, _projectSubscriptionService.ProjectRuleSource);

                        return;

                        void SyncTree(IProjectVersionedValue<ValueTuple<IProjectImportTreeSnapshot, IProjectSubscriptionUpdate>> e)
                        {
                            if (e.Value.Item2.CurrentState.TryGetValue(ConfigurationGeneral.SchemaName, out IProjectRuleSnapshot snapshot))
                            {
                                if (snapshot.Properties.TryGetValue(ConfigurationGeneral.MSBuildProjectExtensionsPathProperty, out string projectExtensionsPath))
                                {
                                    _projectFileClassifier.ProjectExtensionsPath = projectExtensionsPath;
                                }

                                if (snapshot.Properties.TryGetValue(ConfigurationGeneral.NuGetPackageFoldersProperty, out string nuGetPackageFolders))
                                {
                                    _projectFileClassifier.NuGetPackageFolders = nuGetPackageFolders;
                                }
                            }

                            _ = SubmitTreeUpdateAsync(
                                (currentTree, configuredProjectExports, token) =>
                                {
                                    Assumes.NotNull(currentTree);

                                    IProjectTree updatedTree = SyncNode(
                                        imports: e.Value.Item1.Value,
                                        node: (IProjectTree2)currentTree.Value.Tree);

                                    return Task.FromResult(new TreeUpdateResult(updatedTree, e.DataSourceVersions));
                                });

                            return;

                            IProjectTree2 SyncNode(IReadOnlyList<IProjectImportSnapshot> imports, IProjectTree2 node)
                            {
                                var captionByProjectPath = GetCaptionByProjectPath();

                                foreach (IProjectTree2 existingNode in node.Children)
                                {
                                    Assumes.NotNullOrEmpty(existingNode.FilePath);

                                    if (!imports.Any(import => StringComparers.Paths.Equals(import.ProjectPath, existingNode.FilePath)))
                                    {
                                        // Remove child that's no longer present
                                        if (node.TryFind(existingNode.Identity, out IProjectTree? child))
                                        {
                                            node = (IProjectTree2)child.Remove();
                                        }
                                    }
                                }

                                for (int displayOrder = 0; displayOrder < imports.Count; displayOrder++)
                                {
                                    IProjectImportSnapshot import = imports[displayOrder];

                                    // Attempt to find the existing child in the tree, based on file path
                                    IProjectTree2? child = (IProjectTree2?)node.Children.FirstOrDefault(c => StringComparers.Paths.Equals(c.FilePath, import.ProjectPath));

                                    if (child is null)
                                    {
                                        // No child exists for this import, so add it
                                        bool isImplicit = _projectFileClassifier.IsNonUserEditable(import.ProjectPath);
                                        ProjectTreeFlags flags = isImplicit ? s_projectImportImplicitFlags : s_projectImportFlags;
                                        ProjectImageMoniker icon = isImplicit ? s_nodeImplicitIcon : s_nodeIcon;
                                        string caption = captionByProjectPath[import.ProjectPath];

                                        IProjectTree2 newChild = NewTree(
                                            caption,
                                            filePath: import.ProjectPath,
                                            flags: flags,
                                            icon: icon,
                                            displayOrder: displayOrder);

                                        // Recur down the tree
                                        newChild = SyncNode(import.Imports, newChild);

                                        node = AddChild(newChild);
                                    }
                                    else if (child.DisplayOrder != displayOrder)
                                    {
                                        // Child exists but with the wrong display order
                                        node = (IProjectTree2)child.SetDisplayOrder(displayOrder).Parent!;
                                    }
                                    else
                                    {
                                        // Child exists with correct display order, so continue walking tree
                                        IProjectTree2 newChild = SyncNode(import.Imports, child);

                                        if (!ReferenceEquals(child, newChild))
                                        {
                                            node = ReplaceChild(child, newChild);
                                        }
                                    }
                                }

                                return node;

                                IProjectTree2 AddChild(IProjectTree2 child) => (IProjectTree2)node.Add(child).Parent!;
                                IProjectTree2 ReplaceChild(IProjectTree2 oldChild, IProjectTree2 newChild) => (IProjectTree2)node.Remove(oldChild).Add(newChild).Parent!;

                                Dictionary<string, string> GetCaptionByProjectPath()
                                {
                                    // Creates a map from each import's project path to the filename of that project path.
                                    // Although LINQ's Enumerable.ToDictionary extension method can be used to create such
                                    // a map, that implementation fails when there are duplicate entries in the imports.
                                    // Therefore, an extension method that allows duplicate keys to be ignored
                                    // is used to create the map.
                                    var result = imports.ToDictionary(i => i.ProjectPath, i => Path.GetFileName(i.ProjectPath), StringComparers.Paths, ignoreDuplicateKeys: true);

                                    var isDuplicateByFileName = new Dictionary<string, bool>(StringComparers.Paths);

                                    foreach ((string _, string fileName) in result)
                                    {
                                        isDuplicateByFileName[fileName] = isDuplicateByFileName.ContainsKey(fileName);
                                    }

                                    foreach (IProjectImportSnapshot import in imports)
                                    {
                                        string fileName = result[import.ProjectPath];
                                        
                                        if (isDuplicateByFileName[fileName])
                                        {
                                            result[import.ProjectPath] = $"{fileName} ({Path.GetDirectoryName(import.ProjectPath)})";
                                        }
                                    }

                                    return result;
                                }
                            }
                        }
                    }

                    void TearDownTree()
                    {
                        Assumes.NotNull(_subscriptions);
                        _subscriptions.Dispose();
                        _subscriptions = null;

                        _ = SubmitTreeUpdateAsync(
                            (currentTree, configuredProjectExports, token) =>
                            {
                                Assumes.NotNull(currentTree);

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
