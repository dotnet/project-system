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
    // TODO what todo with ProjectKind here? Provide several implementations or request change to provide multiple project types?
    //      ProjectKind = ProjectSystemPackage.ProjectTypeGuidStringWithParentheses)]
    // without specifying ProjectType our GraphProvider will be called for all project types
    // which might cause some extra perf overhead.
    internal class DependenciesGraphProvider : OnceInitializedOnceDisposedAsync, IGraphProvider
    {
        private IDependenciesGraphProjectContextProvider ProjectContextProvider { get; set; }
        private SVsServiceProvider ServiceProvider { get; set; }

        private bool _IsInitialized;

        private object _initializationLock = new object();

        [ImportingConstructor]
        public DependenciesGraphProvider(IDependenciesGraphProjectContextProvider projectContextProvider,
                                             SVsServiceProvider serviceProvider)
            : base(new JoinableTaskContextNode(ThreadHelper.JoinableTaskContext))
        {
            ProjectContextProvider = projectContextProvider;
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Remembers expanded graph nodes.
        /// TODO: Check how to clean them when project is unloaded. (What graph is doing in general when project is killed?)
        /// </summary>
        private WeakCollection<IGraphContext> ExpandedGraphContexts { get; set; }
        private HashSet<string> RegisteredSubTreeProviders { get; set; }

        private object _RegisteredSubTreeProvidersLock = new object();

        private object _ExpandedGraphContextsLock = new object();

        /// <summary>
        /// All icons that are used tree graph, register their monikers once and refer to them by string id
        /// </summary>
        private HashSet<ImageMoniker> Images { get; set; }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            ExpandedGraphContexts = new WeakCollection<IGraphContext>();
            RegisteredSubTreeProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Images = new HashSet<ImageMoniker>();

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
            lock (_initializationLock)
            {
                if (_IsInitialized)
                {
                    return;
                }

                _IsInitialized = true;
            }

            await InitializeAsync().ConfigureAwait(false);
        }

        #region IGraphProvider

        /// <summary>
        /// IGraphProvider.BeginGetGraphData
        /// Entry point for progression. Gets called everytime when progression
        ///  - Needs to know if a node has children
        ///  - Wants to get children for a node
        ///  - During solution explorer search
        /// </summary>
        void IGraphProvider.BeginGetGraphData(IGraphContext context)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await BeginGetGraphDataAsync(context).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// IGraphProvider.GetCommands
        /// </summary>
        IEnumerable<GraphCommand> IGraphProvider.GetCommands(IEnumerable<GraphNode> nodes)
        {
            yield return new GraphCommand(
                GraphCommandDefinition.Contains,
                targetCategories: null,
                linkCategories: new[] { GraphCommonSchema.Contains },
                trackChanges: true);
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

        #endregion

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

            foreach (var icon in icons)
            {
                if (Images.Contains(icon))
                {
                    // already registered - next
                    continue;
                }

                await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    // register icon 
                    var imageService = ServiceProvider.GetService(typeof(SVsImageService)) as IVsImageService2;
                    imageService.TryAssociateNameWithMoniker(icon.Guid.ToString(), icon);
                });

                Images.Add(icon);
            }

            if (!ExpandedGraphContexts.Contains(graphContext))
            {
                // Remember this graph context in order to track changes.
                // When references change, we will adjust children of this graph as necessary
                ExpandedGraphContexts.Add(graphContext);
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

                var subTreeProvider = await GetSubTreeProviderAsync(graphContext, inputGraphNode, projectPath).ConfigureAwait(false);
                if (subTreeProvider == null)
                {
                    continue;
                }

                // All graph nodes generated here will have this unique node id, root node will have it equal to null
                var nodeId = GetPartialValueFromGraphNodeId(inputGraphNode.Id,
                                                            DependenciesGraphSchema.DependencyUniqueId);
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

                var projectContext = ProjectContextProvider.GetProjectContext(projectPath);
                if (projectContext == null)
                {
                    continue;
                }

                var subTreeProvider = await GetSubTreeProviderAsync(graphContext, inputGraphNode, projectPath).ConfigureAwait(false);
                if (subTreeProvider == null)
                {
                    continue;
                }

                var nodeInfo = inputGraphNode.GetValue<IDependencyNode>(
                                    DependenciesGraphSchema.DependencyNodeProperty);
                if (nodeInfo == null)
                {
                    continue;
                }

                // Refetch reference, projectContext may have been changed since the last time CheckChildren was called
                nodeInfo = subTreeProvider.GetDependencyNode(nodeInfo.Id);
                if (nodeInfo == null)
                {
                    continue;
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

                        AddGraphNode(graphContext, projectPath, inputGraphNode, childNodeToAdd);
                    }

                    scope.Complete();
                }

                if (trackChanges && !ExpandedGraphContexts.Contains(graphContext))
                {
                    // Remember this graph context in order to track changes.
                    // When references change, we will adjust children of this graph as necessary
                    ExpandedGraphContexts.Add(graphContext);
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
                        var matchingNodes = await subTreeProvider.SearchAsync(searchTerm).ConfigureAwait(false);
                        if (matchingNodes == null || matchingNodes.Count() <= 0)
                        {
                            continue;
                        }

                        // add provider's root node to display matching nodes
                        var providerRootNode = AddGraphNode(graphContext, 
                                                            projectContext.ProjectFilePath, 
                                                            parentNode: null, 
                                                            nodeInfo: subTreeProvider.RootNode);

                        // now recursively add graph nodes for provider nodes that match search criteria under 
                        // provider's root node
                        AddNodesToGraphRecursive(graphContext, 
                                                 projectContext.ProjectFilePath, 
                                                 providerRootNode, 
                                                 matchingNodes);

                        // 'node' is a GraphNode for top level dependency (which is part of solution explorer tree)
                        // Setting ProjectItem category (and correct GraphNodeId) ensures that search graph appears 
                        // under right solution explorer hierarchy item
                        //providerRootNode.AddCategory(CodeNodeCategories.ProjectFolder);
                        providerRootNode.AddCategory(CodeNodeCategories.ProjectItem);
                    }
                }

                scope.Complete();
            }

            graphContext.OnCompleted();
        }

        private void AddNodesToGraphRecursive(IGraphContext graphContext,
                                              string projectPath,
                                              GraphNode parentNode, 
                                              IEnumerable<IDependencyNode> nodes)
        {
            foreach(var nodeInfo in nodes)
            {
                var graphNode = AddGraphNode(graphContext, projectPath, parentNode, nodeInfo, "SEARCH_RESULT");

                AddNodesToGraphRecursive(graphContext, projectPath, graphNode, nodeInfo.Children);

                graphContext.Graph.Links.GetOrCreate(parentNode, graphNode, null, GraphCommonSchema.Contains);
            }
        }

        /// <summary>
        /// ReferencesGraphProvider supports tracking changes. 
        /// ProjectContextChanged gets fired everytime references change. TrackChangesAsync updates
        /// _expandedGraphContexts as necessary to reflect changes.
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

                var subTreeProvider = await GetSubTreeProviderAsync(graphContext, inputGraphNode, projectPath)
                                                .ConfigureAwait(false);
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
                    var nodesToRemove = GetDifferences(existingNodeInfo.Children, updatedNodeInfo.Children);
                    foreach (var nodeToRemove in nodesToRemove)
                    {
                        RemoveGraphNode(graphContext, projectPath, nodeToRemove);
                    }

                    // Diff updated node children and existing node children to get whats added
                    var nodesToAdd = GetDifferences(updatedNodeInfo.Children, existingNodeInfo.Children);
                    foreach (var nodeToAdd in nodesToAdd)
                    {
                        AddGraphNode(graphContext, projectPath, inputGraphNode, nodeToAdd);
                    }

                    // Update the node info saved on the 'inputNode'
                    inputGraphNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, updatedNodeInfo);

                    scope.Complete();
                }
            }
        }

        /// <summary>
        /// Returns all elements in first that are not in second
        /// </summary>
        private IEnumerable<IDependencyNode> GetDifferences(IEnumerable<IDependencyNode> first,
                                                            IEnumerable<IDependencyNode> second)
        {
            var result = new List<IDependencyNode>();
            foreach (var element in first)
            {
                if (!second.Contains(element))
                {
                    result.Add(element);
                }
            }

            return result;
        }

        private async Task<IProjectDependenciesSubTreeProvider> GetSubTreeProviderAsync(
                                                                    IGraphContext graphContext,
                                                                    GraphNode inputGraphNode, 
                                                                    string projectPath)
        {
            // Check if node has ProviderProperty set. It will be set for GraphNodes we created in 
            // this class, but root nodes at first would have it set to null so e ould have to use
            // fallback File is part to get provider type for them.
            var subTreeProvider = inputGraphNode.GetValue<IProjectDependenciesSubTreeProvider>(
                                    DependenciesGraphSchema.ProviderProperty);
            if (subTreeProvider == null)
            {
                var nodeProviderType = GetPartialValueFromGraphNodeId(inputGraphNode.Id,
                                            DependenciesGraphSchema.ProviderTypeGraphNodeValueName);
                if (nodeProviderType == null)
                {
                    // top level root dependency nodes don't have by default property set to 
                    // DependenciesNodeGraphSchema.ProviderTypeGraphNodeValueName, since there no way 
                    // to set it from CPS's IProjectTreeProvider. Thus try to get provider type key from
                    // node's file, which in case of root node should be set to a type (lowercased).
                    nodeProviderType = GetPartialValueFromGraphNodeId(inputGraphNode.Id, CodeGraphNodeIdName.File);
                }

                var projectContext = ProjectContextProvider.GetProjectContext(projectPath);
                subTreeProvider = projectContext.GetProvider(nodeProviderType);
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
                    return value.IsAbsoluteUri ? value.LocalPath : value.ToString();
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

        private GraphNodeId GetGraphNodeId(string projectPath, 
                                           IDependencyNode nodeInfo, 
                                           bool isProviderRootNode = false, 
                                           string postFix = "")
        {
            var partialValues = new List<GraphNodeId>();

            if (isProviderRootNode)
            {
                partialValues.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.Assembly,
                                                         new Uri(projectPath, UriKind.RelativeOrAbsolute)));
                partialValues.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.File,
                                                         new Uri(nodeInfo.Provider.ProviderType.ToLowerInvariant(),
                                                                 UriKind.RelativeOrAbsolute)));
            }
            else
            {
                partialValues.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.Assembly,
                                                         new Uri(projectPath, UriKind.RelativeOrAbsolute)));
                partialValues.Add(GraphNodeId.GetPartial(DependenciesGraphSchema.ProviderTypeGraphNodeValueName,
                                                         nodeInfo.Provider.ProviderType));
                partialValues.Add(GraphNodeId.GetPartial(DependenciesGraphSchema.DependencyUniqueId, 
                                                         nodeInfo.Id + postFix)); // postfix for search results
            }

            return GraphNodeId.GetNested(partialValues.ToArray());
        }

        private GraphNode AddGraphNode(IGraphContext graphContext,
                                       string projectPath,
                                       GraphNode parentNode,
                                       IDependencyNode nodeInfo,
                                       string postFix = "")
        {
            var newNodeId = GetGraphNodeId(projectPath, nodeInfo, isProviderRootNode: parentNode == null, postFix: postFix);
            var newNode = graphContext.Graph.Nodes.GetOrCreate(newNodeId, nodeInfo.Caption, null);

            newNode.SetValue(DgmlNodeProperties.Icon, nodeInfo.Icon.Guid);
            newNode.SetValue(DependenciesGraphSchema.DependencyNodeProperty, nodeInfo);
            // priority sets correct order among peers
            newNode.SetValue(CodeNodeProperties.SourceLocation, 
                             new SourceLocation(projectPath, new Position(nodeInfo.Priority, 0)));

            graphContext.OutputNodes.Add(newNode);

            if (parentNode != null)
            {
                newNode.AddCategory(DependenciesGraphSchema.CategoryDependency);
                graphContext.Graph.Links.GetOrCreate(parentNode, newNode, null, CodeLinkCategories.Contains);
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
    }
}
