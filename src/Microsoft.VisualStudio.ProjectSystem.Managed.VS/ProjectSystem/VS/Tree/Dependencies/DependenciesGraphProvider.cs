// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.CodeSchema;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Provides actual dependencies nodes under Dependencies\[DependencyType]\[TopLevel]\[....] sub nodes. 
    /// Note: when dependency has ProjectTreeFlags.Common.BrokenReference flag, GraphProvider API are not 
    /// called for that node.
    /// </summary>
    [GraphProvider(Name = "Microsoft.VisualStudio.ProjectSystem.VS.Tree.DependenciesNodeGraphProvider",
                   ProjectCapability = "DependenciesTree")]
    internal class DependenciesGraphProvider : OnceInitializedOnceDisposedAsync, IGraphProvider
    {
        private readonly GraphCommand ContainsGraphCommand = new GraphCommand(
                GraphCommandDefinition.Contains,
                targetCategories: null,
                linkCategories: new[] { GraphCommonSchema.Contains },
                trackChanges: true);

        private readonly object _RegisteredSubTreeProvidersLock = new object();

        private readonly object _ExpandedGraphContextsLock = new object();

        private readonly object _changedContextsQueueLock = new object();
        private Dictionary<string, ProjectContextEventArgs> _changedContextsQueue =
            new Dictionary<string, ProjectContextEventArgs>(StringComparer.OrdinalIgnoreCase);
        private Task _trackChangesTask;

        [ImportingConstructor]
        public DependenciesGraphProvider(IDependenciesGraphProjectContextProvider projectContextProvider,
                                         SVsServiceProvider serviceProvider)
            : this(projectContextProvider, 
                   serviceProvider, 
                   new JoinableTaskContextNode(ThreadHelper.JoinableTaskContext))
        {
        }

        public DependenciesGraphProvider(IDependenciesGraphProjectContextProvider projectContextProvider,
                                         SVsServiceProvider serviceProvider,
                                         JoinableTaskContextNode joinableTaskContextNode)
            : base(joinableTaskContextNode)
        {
            ProjectContextProvider = projectContextProvider;
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Remembers expanded graph nodes.
        /// </summary>
        protected WeakCollection<IGraphContext> ExpandedGraphContexts { get; set; } = new WeakCollection<IGraphContext>();
        protected HashSet<string> RegisteredSubTreeProviders { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private IDependenciesGraphProjectContextProvider ProjectContextProvider { get; }

        private SVsServiceProvider ServiceProvider { get; }

        /// <summary>
        /// All icons that are used tree graph, register their monikers once and refer to them by string id
        /// </summary>
        protected HashSet<ImageMoniker> Images { get; set; } = new HashSet<ImageMoniker>();

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            ProjectContextProvider.ProjectContextChanged += ProjectContextProvider_ProjectContextChanged;

            return Task.CompletedTask;
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            ProjectContextProvider.ProjectContextChanged -= ProjectContextProvider_ProjectContextChanged;

            return Task.CompletedTask;
        }

        private async Task EnsureInitializedAsync()
        {
            if (!IsInitializing || IsInitialized)
            {
                await InitializeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// IGraphProvider.BeginGetGraphData
        /// Entry point for progression. Gets called everytime when progression
        ///  - Needs to know if a node has children
        ///  - Wants to get children for a node
        ///  - During solution explorer search
        /// </summary>
        public void BeginGetGraphData(IGraphContext context)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await BeginGetGraphDataAsync(context).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// IGraphProvider.GetCommands
        /// </summary>
        public IEnumerable<GraphCommand> GetCommands(IEnumerable<GraphNode> nodes)
        {
            yield return ContainsGraphCommand;
        }

        /// <summary>
        /// IGraphProvider.GetExtension
        /// </summary>
        T IGraphProvider.GetExtension<T>(GraphObject graphObject, T previous)
        {
            return null;
        }

        /// <summary>
        /// IGraphProvider.Schema
        /// </summary>
        Graph IGraphProvider.Schema
        {
            get
            {
                return null;
            }
        }

        internal async Task BeginGetGraphDataAsync(IGraphContext context)
        {
            try
            {
                await EnsureInitializedAsync().ConfigureAwait(false);

                if (context.Direction == GraphContextDirection.Self 
                    && context.RequestedProperties.Contains(DgmlNodeProperties.ContainsChildren))
                {
                    await CheckChildrenAsync(context).ConfigureAwait(false);
                }
                else if (context.Direction == GraphContextDirection.Contains)
                {
                    await GetChildrenAsync(context).ConfigureAwait(false);
                }
                else if (context.Direction == GraphContextDirection.Custom)
                {
                    await SearchAsync(context).ConfigureAwait(false);
                }
            }
            finally
            {
                // OnCompleted must be called to display changes 
                context.OnCompleted();
            }
        }

        private async Task RegisterSubTreeProviderAsync(IProjectDependenciesSubTreeProvider subTreeProvider,
                                                        IGraphContext graphContext)
        {
            lock (_RegisteredSubTreeProvidersLock)
            {
                if (RegisteredSubTreeProviders.Contains(subTreeProvider.ProviderType))
                {
                    return;
                }

                RegisteredSubTreeProviders.Add(subTreeProvider.ProviderType);
            }

            var icons = subTreeProvider.Icons;
            if (icons == null)
            {
                return;
            }

            IVsImageService2 imageService = null;
            foreach (var icon in icons)
            {
                if (Images.Contains(icon))
                {
                    // already registered - next
                    continue;
                }

                // register icon 
                if (imageService == null)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    imageService = ServiceProvider.GetService<IVsImageService2, SVsImageService>();
                }

                imageService.TryAssociateNameWithMoniker(GetIconStringName(icon), icon);
                Images.Add(icon);
            }
        }

        /// <summary>
        /// Checks if given node has children and adds corresponding IDependencyDescription to the node.
        /// </summary>
        private async Task CheckChildrenAsync(IGraphContext graphContext)
        {
            foreach (var inputGraphNode in graphContext.InputNodes)
            {
                if (graphContext.CancelToken.IsCancellationRequested)
                {
                    return;
                }

                var projectPath = GetPartialValueFromGraphNodeId(inputGraphNode.Id, CodeGraphNodeIdName.Assembly);
                if (string.IsNullOrEmpty(projectPath))
                {
                    continue;
                }

                var (node, subTreeProvider) = await GetDependencyNodeInfoAsync(graphContext, inputGraphNode, projectPath)
                                                        .ConfigureAwait(false);
                if (node == null || subTreeProvider == null)
                {
                    continue;
                }

                // refresh node
                node = subTreeProvider.GetDependencyNode(node.Id);
                if (node == null)
                {
                    continue;
                }

                var trackChanges = false;
                using (var scope = new GraphTransactionScope())
                {
                    if (node.Flags.Contains(DependencyNode.DependsOnOtherProviders)
                        || subTreeProvider.ProviderType.Equals(SdkDependenciesSubTreeProvider.ProviderTypeString, StringComparison.OrdinalIgnoreCase))
                    {
                        trackChanges = true;
                    }

                    inputGraphNode.SetValue(DependenciesGraphSchema.ProviderProperty, subTreeProvider);
                    inputGraphNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, node);

                    if (node.HasChildren)
                    {
                        inputGraphNode.SetValue(DgmlNodeProperties.ContainsChildren, true);
                    }

                    scope.Complete();
                }

                lock (_ExpandedGraphContextsLock)
                {
                    if (trackChanges && !ExpandedGraphContexts.Contains(graphContext))
                    {
                        // Remember this graph context in order to track changes.
                        // When references change, we will adjust children of this graph as necessary
                        ExpandedGraphContexts.Add(graphContext);
                    }
                }
            }
        }

        /// <summary>
        /// Fills node with children when it is opened in Solution Explorer
        /// </summary>
        private async Task GetChildrenAsync(IGraphContext graphContext)
        {
            var trackChanges = false;
            foreach (var inputGraphNode in graphContext.InputNodes)
            {
                if (graphContext.CancelToken.IsCancellationRequested)
                {
                    return;
                }

                var projectPath = GetPartialValueFromGraphNodeId(inputGraphNode.Id, CodeGraphNodeIdName.Assembly);
                if (string.IsNullOrEmpty(projectPath))
                {
                    continue;
                }

                var (node, subTreeProvider) = await GetDependencyNodeInfoAsync(graphContext, inputGraphNode, projectPath)
                                                        .ConfigureAwait(false);
                if (node == null || subTreeProvider == null)
                {
                    continue;
                }

                if (!node.Flags.Contains(DependencyNode.PreFilledFolderNode))
                {
                    // Refresh reference, projectContext may have been changed since the last time CheckChildren was called
                    node = subTreeProvider.GetDependencyNode(node.Id);
                    if (node == null)
                    {
                        continue;
                    }
                }

                var nodeChildren = node.Children.ToArray();
                // get specific providers for child nodes outside of GraphTransactionScope since it does not support 
                // await and switch to other thread (exception "scope must be completed by the same thread it is created". 
                var childrenSubTreeProviders = new List<IProjectDependenciesSubTreeProvider>();
                foreach(var childNodeToAdd in nodeChildren)
                {
                    var childSubTreeProvider = await GetSubTreeProviderAsync(graphContext,
                                                        null /* inputGraphNode */,
                                                        projectPath,
                                                        childNodeToAdd.Id).ConfigureAwait(false);
                    childrenSubTreeProviders.Add(childSubTreeProvider);
                }

                using (var scope = new GraphTransactionScope())
                {

                    for (int i = 0; i < nodeChildren.Length; ++i)
                    {
                        var childNodeToAdd = nodeChildren[i];
                        var childSubTreeProvider = childrenSubTreeProviders?[i] ?? subTreeProvider;

                        // start tracking changes if needed
                        if (graphContext.TrackChanges)
                        {
                            trackChanges = true;
                        }

                        inputGraphNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, node);
                        var newGraphNode = AddGraphNode(graphContext, projectPath,
                                                        childSubTreeProvider, inputGraphNode, childNodeToAdd);

                        if (childNodeToAdd.Flags.Contains(DependencyNode.PreFilledFolderNode))
                        {                           
                            newGraphNode.SetValue(DgmlNodeProperties.ContainsChildren, true);
                        }
                    }

                    scope.Complete();
                }

                lock (_ExpandedGraphContextsLock)
                {
                    if (trackChanges && !ExpandedGraphContexts.Contains(graphContext))
                    {
                        // Remember this graph context in order to track changes.
                        // When references change, we will adjust children of this graph as necessary
                        ExpandedGraphContexts.Add(graphContext);
                    }
                }
            }
        }

        /// <summary>
        /// Generates search graph containing nodes matching search criteria in Solution Explorer 
        /// and attaches it to correct top level node.
        /// </summary>
        private async Task SearchAsync(IGraphContext graphContext)
        {
            var searchParametersTypeName = typeof(ISolutionSearchParameters).GUID.ToString();
            var searchParameters = graphContext.GetValue<ISolutionSearchParameters>(searchParametersTypeName);
            if (searchParameters == null)
            {
                return;
            }

            var searchTerm = searchParameters.SearchQuery.SearchString?.ToLowerInvariant();
            if (searchTerm == null)
            {
                return;
            }

            var projectContexts = ProjectContextProvider.GetProjectContexts();
            foreach (var projectContext in projectContexts)
            {
                var subTreeProviders = projectContext.GetProviders();
                foreach (var subTreeProvider in subTreeProviders)
                {
                    await RegisterSubTreeProviderAsync(subTreeProvider, graphContext).ConfigureAwait(false);

                    var rootNodeChildren = subTreeProvider.RootNode.Children;
                    foreach (var topLevelNode in rootNodeChildren)
                    {
                        var refreshedTopLevelNode = subTreeProvider.GetDependencyNode(topLevelNode.Id);
                        if (refreshedTopLevelNode == null)
                        {
                            continue;
                        }

                        var matchingNodes = await subTreeProvider.SearchAsync(refreshedTopLevelNode, searchTerm).ConfigureAwait(true);
                        if (matchingNodes == null || matchingNodes.Count() <= 0)
                        {
                            continue;
                        }

                        // Note: scope should start and complete on the same thread, so we have to do it for each provider.
                        using (var scope = new GraphTransactionScope())
                        {
                            // add provider's root node to display matching nodes                                                         
                            var topLevelGraphNode = AddTopLevelGraphNode(graphContext,
                                                                    projectContext.ProjectFilePath,
                                                                    subTreeProvider,
                                                                    nodeInfo: refreshedTopLevelNode);

                            // now recursively add graph nodes for provider nodes that match search criteria under 
                            // provider's root node
                            AddNodesToGraphRecursive(graphContext,
                                                        projectContext.ProjectFilePath,
                                                        subTreeProvider,
                                                        topLevelGraphNode,
                                                        matchingNodes);

                            // 'node' is a GraphNode for top level dependency (which is part of solution explorer tree)
                            // Setting ProjectItem category (and correct GraphNodeId) ensures that search graph appears 
                            // under right solution explorer hierarchy item                     
                            topLevelGraphNode.AddCategory(CodeNodeCategories.ProjectItem);

                            scope.Complete();
                        }
                    }
                }
            }

            graphContext.OnCompleted();
        }

        /// <summary>
        /// Gets IDependencyNode and IProjectDependenciesSubTreeProvider associated with given 
        /// GraphNode. Depending on the request from progression we might already associate 
        /// an IDependencyNode with the node, so first try to get it right away. If it is not available,
        /// try to get node id first, then IProjectDependenciesSubTreeProvider and then get node from 
        /// provider. 
        /// Note: in normal situation we first receive CheckChildrenAsync call where we associate node 
        /// with GraphNode and then when GetChildrenAsync is called we already have it. However when user 
        /// clicks on "Scope To This" context menu we get GetChildrenAsync call right away and need to be 
        /// able to discover IDependencyNode form the scratch.
        /// </summary>
        private async Task<(IDependencyNode node, IProjectDependenciesSubTreeProvider subTreeProvider)> 
            GetDependencyNodeInfoAsync(IGraphContext graphContext, GraphNode inputGraphNode, string projectPath)
        {
            IDependencyNode node = null;
            IProjectDependenciesSubTreeProvider subTreeProvider = null;

            // check if node is toplevel or lowerlevel hierarchy
            node = inputGraphNode.GetValue<IDependencyNode>(DependenciesGraphSchema.DependencyNodeProperty);
            if (node == null)
            {
                // this is a top level dependency node or unsupported node like some source file node
                var hierarchyItem = inputGraphNode.GetValue<IVsHierarchyItem>(HierarchyGraphNodeProperties.HierarchyItem);
                if (hierarchyItem == null)
                {
                    // unknown node
                    return (node, subTreeProvider);
                }

                // now check node file path if it is a DependencyNodeId 
                var nodeFilePath = GetPartialValueFromGraphNodeId(inputGraphNode.Id, CodeGraphNodeIdName.File);
                var nodeId = DependencyNodeId.FromString(nodeFilePath);
                if (nodeId == null)
                {
                    // check parent node - it will be a IProjectTree node which can have file path to be 
                    // in our DependencyNodeId format. If it is a top level dependency, it's parent will 
                    // be provider root node: Dependencies\NuGet\<top level dependency>. In this sample
                    // parent for <top level dependency> will be NuGet node, which will be not IProjectItemTree,
                    // but IProjecTree and thus has FilePath containing actual DependencyNodeId. 
                    var parentFilePath = hierarchyItem.Parent.CanonicalName;
                    nodeId = DependencyNodeId.FromString(parentFilePath);
                }

                if (nodeId == null)
                {
                    // unknown node
                    return (node, subTreeProvider);
                }

                subTreeProvider = await GetSubTreeProviderAsync(graphContext,
                                                                inputGraphNode,
                                                                projectPath,
                                                                nodeId).ConfigureAwait(false);

                if (subTreeProvider == null)
                {
                    return (node, subTreeProvider);
                }

                // now get real DependencyNodeId - find the actual node in the provider's top 
                // level node. Note: nodeId above we need only to get correct sub tree provider, but
                // it would not contain our top level node's ItemSpec, ItemType etc. We need to get it here.
                var caption = hierarchyItem.Text;
                var rootNodeChildren = subTreeProvider.RootNode.Children;
                node = rootNodeChildren.FirstOrDefault(x => x.Caption.Equals(caption, StringComparison.OrdinalIgnoreCase));
                if (node == null)
                {
                    // node is not ours or does node exist anymore
                    return (node, subTreeProvider);
                }
            }
            else
            {
                // this is lower level graph node that is created by this provider, just get it's subTreeProvider
                subTreeProvider = await GetSubTreeProviderAsync(graphContext,
                                                                inputGraphNode,
                                                                projectPath,
                                                                node.Id).ConfigureAwait(false);
            }

            return (node, subTreeProvider);
        }

        private void AddNodesToGraphRecursive(IGraphContext graphContext,
                                              string projectPath,
                                              IProjectDependenciesSubTreeProvider subTreeProvider,
                                              GraphNode parentNode, 
                                              IEnumerable<IDependencyNode> nodes)
        {
            foreach(var nodeInfo in nodes)
            {
                var graphNode = AddGraphNode(graphContext, projectPath, subTreeProvider, parentNode, nodeInfo);
                graphContext.Graph.Links.GetOrCreate(parentNode, graphNode, null, GraphCommonSchema.Contains);

                var nodeChildren = nodeInfo.Children;
                AddNodesToGraphRecursive(graphContext, projectPath, subTreeProvider, graphNode, nodeChildren);
            }
        }

        /// <summary>
        /// ReferencesGraphProvider supports tracking changes. 
        /// ProjectContextChanged gets fired everytime dependencies change. TrackChangesAsync updates
        /// ExpandedGraphContexts as necessary to reflect changes.
        /// </summary>
        private void ProjectContextProvider_ProjectContextChanged(object sender, ProjectContextEventArgs e)
        {
            lock(_changedContextsQueueLock)
            {
                _changedContextsQueue[e.Context.ProjectFilePath] = e;

                // schedule new track changes request in the queue
                if (_trackChangesTask == null || _trackChangesTask.IsCompleted)
                {
                    _trackChangesTask = RunTrackChangesAsync();
                }
                else
                {
                    _trackChangesTask = _trackChangesTask.ContinueWith(t => RunTrackChangesAsync(), TaskScheduler.Default);
                }
            }
        }

        /// <summary>
        /// Does process queue of track changes requests.
        /// </summary>
        private Task RunTrackChangesAsync()
        {
            return ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                // TODO We might want to check if project or solution unloaded and cancel updates 
                // here, does not meet the bar at the moment.
                List<ProjectContextEventArgs> queue = null;

                lock(_changedContextsQueueLock)
                {
                    queue = _changedContextsQueue.Values.ToList();
                    _changedContextsQueue.Clear();
                }

                foreach (var context in queue)
                {
                    await TrackChangesAsync(context).ConfigureAwait(false);
                }
            }).Task;
        }

        /// <summary>
        /// Property ExpandedGraphContexts remembers graph expanded or checked so far.
        /// Each context represents one level in the graph, i.e. a node and its first level dependencies
        /// Tracking changes over all expanded contexts ensures that all levels are processed
        /// and updated when there are any changes in nodes data.
        /// </summary>
        internal async Task TrackChangesAsync(ProjectContextEventArgs updatedProjectContext)
        {
            foreach (var graphContext in ExpandedGraphContexts.ToList())
            {
                try
                {
                    await TrackChangesOnGraphContextAsync(graphContext, updatedProjectContext).ConfigureAwait(false);
                }
                finally
                {
                    // Calling OnCompleted ensures that the changes are reflected in UI
                    graphContext.OnCompleted();
                }
            }
        }

        /// <summary>
        /// Tries to apply changes to corresponding node in given GraphContext for given changes
        /// </summary>
        private async Task TrackChangesOnGraphContextAsync(IGraphContext graphContext,
                                                     ProjectContextEventArgs changes)
        {
            var updatedProjectContext = changes.Context;
            foreach (var inputGraphNode in graphContext.InputNodes.ToList())
            {
                var existingNodeInfo = inputGraphNode.GetValue<IDependencyNode>(
                                            DependenciesGraphSchema.DependencyNodeProperty);
                if (existingNodeInfo == null)
                {
                    continue;
                }

                var projectPath = GetPartialValueFromGraphNodeId(inputGraphNode.Id, CodeGraphNodeIdName.Assembly);
                if (string.IsNullOrEmpty(projectPath))
                {
                    continue;
                }
                
                if (!ShouldTrackChangesForNode(existingNodeInfo, projectPath, updatedProjectContext.ProjectFilePath))
                {
                    continue;
                }

                var subTreeProvider = await GetSubTreeProviderAsync(graphContext,
                                                                inputGraphNode,
                                                                projectPath,
                                                                existingNodeInfo.Id).ConfigureAwait(false);
                if (subTreeProvider == null)
                {
                    continue;
                }

                var updatedNode = subTreeProvider.GetDependencyNode(existingNodeInfo.Id);
                if (updatedNode == null)
                {
                    continue;
                }

                var newNode = DependencyNode.Clone(existingNodeInfo);
                bool anyChanges = AnyChangesToTrack(newNode,
                                                    updatedNode,
                                                    changes.Diff,
                                                    out IEnumerable<IDependencyNode> nodesToAdd,
                                                    out IEnumerable<IDependencyNode> nodesToRemove);
                if (!anyChanges)
                {
                    continue;
                }

                // register providers for new nodes outside of scope using (it can not have await there)
                await RegisterProviders(graphContext, projectPath, nodesToAdd).ConfigureAwait(false);

                using (var scope = new GraphTransactionScope())
                {
                    foreach (var nodeToRemove in nodesToRemove)
                    {
                        RemoveGraphNode(graphContext, projectPath, nodeToRemove, inputGraphNode);
                        newNode.RemoveChild(nodeToRemove);
                    }

                    foreach (var nodeToAdd in nodesToAdd)
                    {
                        AddGraphNode(graphContext, projectPath, null, inputGraphNode, nodeToAdd);
                        newNode.AddChild(nodeToAdd);
                    }

                    // Update the node info saved on the 'inputNode'
                    inputGraphNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, newNode);

                    scope.Complete();                    
                }
            }
        }

        /// <summary>
        /// Discovers if there any changes to apply for existing node, after it's context changed.
        /// </summary>
        private bool AnyChangesToTrack(IDependencyNode node,
                                       IDependencyNode updatedNode,
                                       IDependenciesChangeDiff diff,
                                       out IEnumerable<IDependencyNode> nodesToAdd,
                                       out IEnumerable<IDependencyNode> nodesToRemove)
        {
            var existingChildren = node.Children;
            if (node.Flags.Contains(DependencyNode.DependsOnOtherProviders))
            {
                var remove = new HashSet<IDependencyNode>(diff.RemovedNodes);
                var add = new HashSet<IDependencyNode>(diff.AddedNodes);
                foreach (var changedNode in diff.UpdatedNodes)
                {
                    remove.Add(changedNode);
                    add.Add(changedNode);
                }

                nodesToAdd = add;
                nodesToRemove = remove;
            }
            else
            {
                var updatedChildren = updatedNode.Children;
                var comparer = new DependencyNodeResolvedStateComparer();
                nodesToRemove = existingChildren.Except(updatedChildren, comparer).ToList();
                nodesToAdd = updatedChildren.Except(existingChildren, comparer).ToList();
            }

            return nodesToAdd.Any() || nodesToRemove.Any();
        }

        /// <summary>
        /// Tries to request each provider in the given list, which would register and cache provider if
        /// it was requested first time.
        /// </summary>
        private async Task RegisterProviders(IGraphContext graphContext, 
                                             string projectPath, 
                                             IEnumerable<IDependencyNode> forNodes)
        {
            foreach (var node in forNodes)
            {
                await GetSubTreeProviderAsync(graphContext, null, projectPath, node.Id).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Depending on the node info, determines if changes should be tracked or not.
        /// </summary>
        private bool ShouldTrackChangesForNode(IDependencyNode node, string projectPath, string updatedProjectPath)
        {
            bool shouldProcess;
            if (node.Flags.Contains(DependencyNode.DependsOnOtherProviders))
            {
                // if node depends on other nodes, we track changes only if node's ContextProject matches
                // updated project
                shouldProcess = !string.IsNullOrEmpty(node.Id.ContextProject)
                                 && node.Id.ContextProject.Equals(updatedProjectPath, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // if node is regular project dependency node, we track changes when project was updated
                shouldProcess = projectPath.Equals(updatedProjectPath, StringComparison.OrdinalIgnoreCase);
            }

            return shouldProcess;
        }

        private async Task<IProjectDependenciesSubTreeProvider> GetSubTreeProviderAsync(
                                                                    IGraphContext graphContext,
                                                                    GraphNode inputGraphNode, 
                                                                    string projectPath,
                                                                    DependencyNodeId nodeId)
        {
            // Check if node has ProviderProperty set. It will be set for GraphNodes we created in 
            // this class, but root nodes at first would have it set to null so e ould have to use
            // fallback File is part to get provider type for them.
            var subTreeProvider = inputGraphNode?.GetValue<IProjectDependenciesSubTreeProvider>(
                                    DependenciesGraphSchema.ProviderProperty);
            if (subTreeProvider == null)
            {                
                var projectContext = ProjectContextProvider.GetProjectContext(projectPath);
                if (projectContext != null)
                {
                    subTreeProvider = projectContext.GetProvider(nodeId.ProviderType);
                }
            }

            if (subTreeProvider != null)
            {
                await RegisterSubTreeProviderAsync(subTreeProvider, graphContext).ConfigureAwait(false);
            }

            return subTreeProvider;
        }

        private string GetPartialValueFromGraphNodeId(GraphNodeId id, GraphNodeIdName idPartName)
        {
            if (idPartName == CodeGraphNodeIdName.Assembly || idPartName == CodeGraphNodeIdName.File)
            {
                try
                {
                    var value = id.GetNestedValueByName<Uri>(idPartName);

                    // for idPartName == CodeGraphNodeIdName.File it can be null, avoid unnecessary exception
                    if (value == null)
                    {
                        return null;
                    }

                    // Assembly and File are represented by a Uri, extract LocalPath string from Uri
                    return (value.IsAbsoluteUri ? value.LocalPath : value.ToString()).Trim('/');
                }
                catch
                {
                    // for some node ids Uri might throw format exception, thus try to get string at least
                    return id.GetNestedValueByName<string>(idPartName);
                }
            }
            else
            {
                return id.GetNestedValueByName<string>(idPartName);
            }                
        }

        private GraphNodeId GetGraphNodeId(string projectPath, IDependencyNode nodeInfo, GraphNode parentNode)
        {
            var partialValues = new List<GraphNodeId>
            {
                GraphNodeId.GetPartial(CodeGraphNodeIdName.Assembly,
                                                     new Uri(projectPath, UriKind.RelativeOrAbsolute)),
                GraphNodeId.GetPartial(CodeGraphNodeIdName.File,
                                                     new Uri(nodeInfo.Id.ToString().ToLowerInvariant(),
                                                             UriKind.RelativeOrAbsolute))
            };

            // to ensure Graph id for node is unique we add a hashcodes for node's parents separated by ';'
            var parents = parentNode.Id.GetNestedValueByName<string>(CodeGraphNodeIdName.Namespace);
            if (string.IsNullOrEmpty(parents))
            {
                parents = projectPath.GetHashCode().ToString();
            }

            parents = parents + ";" + nodeInfo.Id.GetHashCode();
            partialValues.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.Namespace, parents));

            return GraphNodeId.GetNested(partialValues.ToArray());
        }

        private GraphNodeId GetTopLevelGraphNodeId(string projectPath, IDependencyNode nodeInfo)
        {
            var partialValues = new List<GraphNodeId>
            {
                GraphNodeId.GetPartial(CodeGraphNodeIdName.Assembly, new Uri(projectPath, UriKind.RelativeOrAbsolute))
            };

            var projectFolder = Path.GetDirectoryName(projectPath)?.ToLowerInvariant();
            if (nodeInfo.Flags.Contains(DependencyNode.CustomItemSpec))
            {
                if (nodeInfo.Name != null)
                {
                    partialValues.Add(GraphNodeId.GetPartial(
                        CodeGraphNodeIdName.File,
                        new Uri(Path.Combine(projectFolder, nodeInfo.Name.ToLowerInvariant()),
                        UriKind.RelativeOrAbsolute)));
                }
            }
            else
            {
                var fullItemSpecPath = MakeRooted(projectFolder, nodeInfo.Id.ItemSpec);
                if (!string.IsNullOrEmpty(fullItemSpecPath))
                {
                    partialValues.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.File,
                        new Uri(fullItemSpecPath.ToLowerInvariant(), UriKind.RelativeOrAbsolute)));
                }
            }

            return GraphNodeId.GetNested(partialValues.ToArray());
        }

        private GraphNode AddGraphNode(IGraphContext graphContext,
                                       string projectPath,
                                       IProjectDependenciesSubTreeProvider subTreeProvider,
                                       GraphNode parentNode,
                                       IDependencyNode nodeInfo)
        {
            var newNodeId = GetGraphNodeId(projectPath, nodeInfo, parentNode);

            return DoAddGraphNode(newNodeId, graphContext, projectPath, subTreeProvider, parentNode, nodeInfo);
        }

        private GraphNode AddTopLevelGraphNode(IGraphContext graphContext,
                                               string projectPath,
                                               IProjectDependenciesSubTreeProvider subTreeProvider,
                                               IDependencyNode nodeInfo)
        {
            var newNodeId = GetTopLevelGraphNodeId(projectPath, nodeInfo);

            return DoAddGraphNode(newNodeId, graphContext, projectPath, subTreeProvider, null, nodeInfo);
        }

        private GraphNode DoAddGraphNode(GraphNodeId graphNodeId,
                                         IGraphContext graphContext,
                                         string projectPath,
                                         IProjectDependenciesSubTreeProvider subTreeProvider,
                                         GraphNode parentNode,
                                         IDependencyNode nodeInfo)
        {
            var newNode = graphContext.Graph.Nodes.GetOrCreate(graphNodeId, nodeInfo.Caption, null);

            newNode.SetValue(DgmlNodeProperties.Icon, GetIconStringName(nodeInfo.Icon));
            newNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, nodeInfo);
            // priority sets correct order among peers
            newNode.SetValue(CodeNodeProperties.SourceLocation,
                             new SourceLocation(projectPath, new Position(nodeInfo.Priority, 0)));
            newNode.SetValue(DependenciesGraphSchema.ProviderProperty, subTreeProvider);

            newNode.AddCategory(DependenciesGraphSchema.CategoryDependency);

            graphContext.OutputNodes.Add(newNode);

            if (parentNode != null)
            {
                graphContext.Graph.Links.GetOrCreate(parentNode, newNode, /*label*/ null, CodeLinkCategories.Contains);
            }

            return newNode;
        }

        private void RemoveGraphNode(IGraphContext graphContext, 
                                     string projectPath, 
                                     IDependencyNode treeNodeInfo,
                                     GraphNode parentNode)
        {
            var id = GetGraphNodeId(projectPath, treeNodeInfo, parentNode);
            var nodeToRemove = graphContext.Graph.Nodes.Get(id);

            if (nodeToRemove != null)
            {
                graphContext.OutputNodes.Remove(nodeToRemove);
                graphContext.Graph.Nodes.Remove(nodeToRemove);
            }
        }

        private static string GetIconStringName(ImageMoniker icon)
        {
            return $"{icon.Guid.ToString()};{icon.Id}";
        }

        private static string MakeRooted(string projectFolder, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (ManagedPathHelper.IsRooted(path))
            {
                return path;
            }
            else
            {
                return ManagedPathHelper.TryMakeRooted(projectFolder, path);
            }
        }
    }
}
