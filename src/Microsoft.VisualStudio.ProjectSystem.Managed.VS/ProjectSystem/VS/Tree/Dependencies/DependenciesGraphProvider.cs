// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
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
using System.IO;

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

                using (var scope = new GraphTransactionScope())
                {
                    inputGraphNode.SetValue(DependenciesGraphSchema.ProviderProperty, subTreeProvider);
                    inputGraphNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, node);

                    if (node.HasChildren)
                    {
                        inputGraphNode.SetValue(DgmlNodeProperties.ContainsChildren, true);
                    }

                    scope.Complete();
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

                if (!string.IsNullOrEmpty(node.Id.ContextProject))
                {
                    projectPath = node.Id.ContextProject;
                }

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
            using (var scope = new GraphTransactionScope())
            {
                foreach (var projectContext in projectContexts)
                {
                    var subTreeProviders = projectContext.GetProviders();
                    foreach (var subTreeProvider in subTreeProviders)
                    {
                        await RegisterSubTreeProviderAsync(subTreeProvider, graphContext).ConfigureAwait(false);

                        foreach (var topLevelNode in subTreeProvider.RootNode.Children)
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
                        }
                    }
                }

                scope.Complete();
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
                node = subTreeProvider.RootNode.Children.Where(x => x.Caption.Equals(caption, StringComparison.OrdinalIgnoreCase))
                                                 .FirstOrDefault();
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

                AddNodesToGraphRecursive(graphContext, projectPath, subTreeProvider, graphNode, nodeInfo.Children);                
            }
        }

        /// <summary>
        /// ReferencesGraphProvider supports tracking changes. 
        /// ProjectContextChanged gets fired everytime dependencies change. TrackChangesAsync updates
        /// ExpandedGraphContexts as necessary to reflect changes.
        /// </summary>
        private void ProjectContextProvider_ProjectContextChanged(object sender, ProjectContextEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await TrackChangesAsync(e.Context).ConfigureAwait(false);
            });
        }
       
        /// <summary>
        /// Property ExpandedGraphContexts remembers graph expanded so far.
        /// Each context represents one level in the graph, i.e. a node and its first level dependencies
        /// Tracking changes over all expanded contexts ensures that all levels are processed
        /// and updated when there are any changes in nodes data.
        /// </summary>
        internal async Task TrackChangesAsync(IDependenciesGraphProjectContext updatedProjectContext)
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

        private async Task TrackChangesOnGraphContextAsync(IGraphContext graphContext, 
                                                           IDependenciesGraphProjectContext updatedProjectContext)
        {
            foreach (var inputGraphNode in graphContext.InputNodes.ToList())
            {
                var existingNodeInfo = inputGraphNode.GetValue<IDependencyNode>(
                                            DependenciesGraphSchema.DependencyNodeProperty);
                if (existingNodeInfo == null)
                {
                    continue;
                }

                var projectPath = GetPartialValueFromGraphNodeId(inputGraphNode.Id, CodeGraphNodeIdName.Assembly);
                bool shouldProcess = !string.IsNullOrEmpty(projectPath) &&
                                     projectPath.Equals(updatedProjectContext.ProjectFilePath, StringComparison.OrdinalIgnoreCase);
                var contextProject = updatedProjectContext.ProjectFilePath;
                if (!shouldProcess)
                {
                    shouldProcess = !string.IsNullOrEmpty(existingNodeInfo.Id.ContextProject) &&
                                     existingNodeInfo.Id.ContextProject.Equals(updatedProjectContext.ProjectFilePath, 
                                                                    StringComparison.OrdinalIgnoreCase);
                }

                if (!shouldProcess)
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

                // store existing children, since existingNodeInfo instance might be updated 
                // (this is a side effect for top level nodes)
                var existingChildren = new HashSet<IDependencyNode>(existingNodeInfo.Children);

                // Get updated reference from the new snapshot
                var updatedNodeInfo = subTreeProvider.GetDependencyNode(existingNodeInfo.Id);
                if (updatedNodeInfo == null)
                {
                    continue;
                }

                using (var scope = new GraphTransactionScope())
                {
                    var comparer = new DependencyNodeResolvedStateComparer();
                    // Diff existing node children and updated node children to get whats removed
                    var nodesToRemove = existingChildren.Except(updatedNodeInfo.Children, comparer).ToList();
                    // Diff updated node children and existing node children to get whats added
                    var nodesToAdd = updatedNodeInfo.Children.Except(existingChildren, comparer).ToList();

                    foreach (var nodeToRemove in nodesToRemove)
                    {
                        RemoveGraphNode(graphContext, contextProject, nodeToRemove);
                        existingNodeInfo.RemoveChild(nodeToRemove);
                    }

                    foreach (var nodeToAdd in nodesToAdd)
                    {
                        AddGraphNode(graphContext, contextProject, null, inputGraphNode, nodeToAdd);
                        existingNodeInfo.AddChild(nodeToAdd);
                    }

                    // Update the node info saved on the 'inputNode'
                    inputGraphNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, updatedNodeInfo);

                    scope.Complete();
                }
            }
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

        private GraphNodeId GetGraphNodeId(string projectPath, IDependencyNode nodeInfo)
        {
            var partialValues = new List<GraphNodeId>();
            partialValues.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.Assembly,
                                                     new Uri(projectPath, UriKind.RelativeOrAbsolute)));
            partialValues.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.File,
                                                     new Uri(nodeInfo.Id.ToString().ToLowerInvariant(),
                                                             UriKind.RelativeOrAbsolute)));
            return GraphNodeId.GetNested(partialValues.ToArray());
        }

        private GraphNodeId GetTopLevelGraphNodeId(string projectPath, IDependencyNode nodeInfo)
        {
            var partialValues = new List<GraphNodeId>();
            partialValues.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.Assembly,
                                                     new Uri(projectPath, UriKind.RelativeOrAbsolute)));
            if (nodeInfo.Flags.Contains(DependencyNode.GenericDependencyFlags))
            {
                var projectFolder = Path.GetDirectoryName(projectPath)?.ToLowerInvariant();
                if (nodeInfo.Flags.Contains(DependencyNode.CustomItemSpec))
                {
                    var name = DependencyNode.GetName(nodeInfo);
                    if (name != null)
                    {
                        partialValues.Add(GraphNodeId.GetPartial(
                            CodeGraphNodeIdName.File,
                            new Uri(Path.Combine(projectFolder, name.ToLowerInvariant()),
                            UriKind.RelativeOrAbsolute)));
                    }
                }
                else
                {
                    var fullItemSpecPath = Path.GetFullPath(
                                            Path.Combine(projectFolder,
                                                         nodeInfo.Id.ItemSpec?.ToLowerInvariant()));
                    if (!string.IsNullOrEmpty(fullItemSpecPath))
                    {
                        partialValues.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.File,
                                                             new Uri(fullItemSpecPath, UriKind.RelativeOrAbsolute)));
                    }
                }
            }
            else
            {
                partialValues.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.File,
                                                         new Uri(nodeInfo.Id.ToString().ToLowerInvariant(),
                                                           UriKind.RelativeOrAbsolute)));
            }

            return GraphNodeId.GetNested(partialValues.ToArray());
        }

        private GraphNode AddGraphNode(IGraphContext graphContext,
                                       string projectPath,
                                       IProjectDependenciesSubTreeProvider subTreeProvider,
                                       GraphNode parentNode,
                                       IDependencyNode nodeInfo)
        {
            var newNodeId = GetGraphNodeId(projectPath, nodeInfo);

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
                                     IDependencyNode treeNodeInfo)
        {
            var id = GetGraphNodeId(projectPath, treeNodeInfo);
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
    }
}
