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

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Provides actual dependencies nodes under Dependencies\[DependencyType]\[TopLevel]\[....] sub nodes. 
    /// </summary>
    [GraphProvider(Name = "Microsoft.VisualStudio.ProjectSystem.VS.Tree.DependenciesNodeGraphProvider")]
    // TODO We are adding a way to filter GraphProviders by capability instead of ProjectKind. Specify 
    // capability when change is ready.
    //      ProjectKind = ProjectSystemPackage.ProjectTypeGuidStringWithParentheses)]
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

            return TplExtensions.CompletedTask;
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            ProjectContextProvider.ProjectContextChanged -= ProjectContextProvider_ProjectContextChanged;

            return TplExtensions.CompletedTask;
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

                // All graph nodes generated here will have this unique node id, root node will have it equal to null
                var nodeIdString = GetPartialValueFromGraphNodeId(inputGraphNode.Id, CodeGraphNodeIdName.File);
                var nodeId = DependencyNodeId.FromString(nodeIdString);
                if (nodeId == null)
                {
                    continue;
                }

                var projectPath = GetPartialValueFromGraphNodeId(inputGraphNode.Id, CodeGraphNodeIdName.Assembly);
                if (string.IsNullOrEmpty(projectPath))
                {
                    continue;
                }

                var subTreeProvider = await GetSubTreeProviderAsync(graphContext, 
                                                                    inputGraphNode, 
                                                                    projectPath,
                                                                    nodeId).ConfigureAwait(false);
                if (subTreeProvider == null)
                {
                    continue;
                }

                IDependencyNode nodeInfo = subTreeProvider.GetDependencyNode(nodeId);
                if (nodeInfo == null)
                {
                    continue;
                }

                using (var scope = new GraphTransactionScope())
                {
                    inputGraphNode.SetValue(DependenciesGraphSchema.ProviderProperty, subTreeProvider);
                    inputGraphNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, nodeInfo);

                    if (nodeInfo.HasChildren)
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

                var nodeInfo = inputGraphNode.GetValue<IDependencyNode>(
                                    DependenciesGraphSchema.DependencyNodeProperty);
                if (nodeInfo == null)
                {
                    continue;
                }

                var subTreeProvider = await GetSubTreeProviderAsync(graphContext, 
                                                                    inputGraphNode, 
                                                                    projectPath,
                                                                    nodeInfo.Id).ConfigureAwait(false);
                if (subTreeProvider == null)
                {
                    continue;
                }

                if (!nodeInfo.Flags.Contains(DependencyNode.PreFilledFolderNode))
                {
                    // Refresh reference, projectContext may have been changed since the last time CheckChildren was called
                    nodeInfo = subTreeProvider.GetDependencyNode(nodeInfo.Id);
                    if (nodeInfo == null)
                    {
                        continue;
                    }
                }

                using (var scope = new GraphTransactionScope())
                {
                    var nodeChildren = nodeInfo.Children;
                    foreach (var childNodeToAdd in nodeChildren)
                    {
                        // start tracking changes if needed
                        if (graphContext.TrackChanges)
                        {
                            trackChanges = true;
                        }

                        inputGraphNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, nodeInfo);

                        var newGraphNode = AddGraphNode(graphContext, projectPath, 
                                                subTreeProvider, inputGraphNode, childNodeToAdd);

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

            var searchTerm = searchParameters.SearchQuery.SearchString;
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
                            var matchingNodes = await subTreeProvider.SearchAsync(topLevelNode, searchTerm).ConfigureAwait(true);
                            if (matchingNodes == null || matchingNodes.Count() <= 0)
                            {
                                continue;
                            }

                            // add provider's root node to display matching nodes                                                         
                            var topLevelGraphNode = AddGraphNode(graphContext,
                                                                 projectContext.ProjectFilePath,
                                                                 subTreeProvider,
                                                                 parentNode: null,
                                                                 nodeInfo: topLevelNode);

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
            foreach (var graphContext in ExpandedGraphContexts)
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
            foreach (var inputGraphNode in graphContext.InputNodes)
            {
                var projectPath = GetPartialValueFromGraphNodeId(inputGraphNode.Id, CodeGraphNodeIdName.Assembly);
                if (string.IsNullOrEmpty(projectPath) ||
                    !projectPath.Equals(updatedProjectContext.ProjectFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var existingNodeInfo = inputGraphNode.GetValue<IDependencyNode>(
                                            DependenciesGraphSchema.DependencyNodeProperty);
                if (existingNodeInfo == null)
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

                // Get updated reference from the new snapshot
                var updatedNodeInfo = subTreeProvider.GetDependencyNode(existingNodeInfo.Id);
                if (updatedNodeInfo == null)
                {
                    continue;
                }

                using (var scope = new GraphTransactionScope())
                {
                    // Diff existing node children and updated node children to get whats removed
                    var nodesToRemove = existingNodeInfo.Children.Except(updatedNodeInfo.Children);
                    foreach (var nodeToRemove in nodesToRemove)
                    {
                        RemoveGraphNode(graphContext, projectPath, nodeToRemove);
                    }

                    // Diff updated node children and existing node children to get whats added
                    var nodesToAdd = updatedNodeInfo.Children.Except(existingNodeInfo.Children);
                    foreach (var nodeToAdd in nodesToAdd)
                    {
                        AddGraphNode(graphContext, projectPath, subTreeProvider, inputGraphNode, nodeToAdd);
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
            var subTreeProvider = inputGraphNode.GetValue<IProjectDependenciesSubTreeProvider>(
                                    DependenciesGraphSchema.ProviderProperty);
            if (subTreeProvider == null)
            {                
                var projectContext = ProjectContextProvider.GetProjectContext(projectPath);
                subTreeProvider = projectContext.GetProvider(nodeId.ProviderType);
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

        private GraphNode AddGraphNode(IGraphContext graphContext,
                                       string projectPath,
                                       IProjectDependenciesSubTreeProvider subTreeProvider,
                                       GraphNode parentNode,
                                       IDependencyNode nodeInfo)
        {
            var newNodeId = GetGraphNodeId(projectPath, nodeInfo);
            var newNode = graphContext.Graph.Nodes.GetOrCreate(newNodeId, nodeInfo.Caption, null);

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
