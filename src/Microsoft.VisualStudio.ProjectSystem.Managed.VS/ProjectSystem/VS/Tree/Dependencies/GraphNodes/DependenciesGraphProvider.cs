// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    /// <summary>
    /// Provides actual dependencies nodes under Dependencies\[DependencyType]\[TopLevel]\[....] sub nodes. 
    /// Note: when dependency has ProjectTreeFlags.Common.BrokenReference flag, GraphProvider API are not 
    /// called for that node.
    /// </summary>
    [GraphProvider(Name = "Microsoft.VisualStudio.ProjectSystem.VS.Tree.DependenciesNodeGraphProvider",
                   ProjectCapability = ProjectCapability.DependenciesTree)]
    [Export(typeof(IDependenciesGraphBuilder))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependenciesGraphProvider : OnceInitializedOnceDisposedAsync, IGraphProvider, IDependenciesGraphBuilder
    {
        [ImportingConstructor]
        public DependenciesGraphProvider(IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider,
                                         SVsServiceProvider serviceProvider)
            : this(aggregateSnapshotProvider,
                   serviceProvider,
                   new JoinableTaskContextNode(ThreadHelper.JoinableTaskContext))
        {
        }

        public DependenciesGraphProvider(IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider,
                                         SVsServiceProvider serviceProvider,
                                         JoinableTaskContextNode joinableTaskContextNode)
            : base(joinableTaskContextNode)
        {
            AggregateSnapshotProvider = aggregateSnapshotProvider;
            ServiceProvider = serviceProvider;
            GraphActionHandlers = new OrderPrecedenceImportCollection<IDependenciesGraphActionHandler>(
                                    ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast);
        }

        private static readonly GraphCommand[] s_containsGraphCommand = new[] { new GraphCommand(
                GraphCommandDefinition.Contains,
                targetCategories: null,
                linkCategories: new[] { GraphCommonSchema.Contains },
                trackChanges: true) };

        private readonly object _knownIconsLock = new object();

        /// <summary>
        /// All icons that are used tree graph, register their monikers once to avoid extra UI thread switches.
        /// </summary>
        private HashSet<ImageMoniker> KnownIcons { get; } = new HashSet<ImageMoniker>();

        [ImportMany]
        private OrderPrecedenceImportCollection<IDependenciesGraphActionHandler> GraphActionHandlers { get; }

        private readonly object _changedContextsQueueLock = new object();
        private Dictionary<string, SnapshotChangedEventArgs> _changedContextsQueue =
            new Dictionary<string, SnapshotChangedEventArgs>(StringComparer.OrdinalIgnoreCase);
        private Task _trackChangesTask;
        private readonly object _expandedGraphContextsLock = new object();

        /// <summary>
        /// Remembers expanded graph nodes to track changes in their children.
        /// </summary>
        protected WeakCollection<IGraphContext> ExpandedGraphContexts { get; set; } = new WeakCollection<IGraphContext>();

        private IAggregateDependenciesSnapshotProvider AggregateSnapshotProvider { get; }

        private SVsServiceProvider ServiceProvider { get; }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            AggregateSnapshotProvider.SnapshotChanged += OnSnapshotChanged;

            return Task.CompletedTask;
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            AggregateSnapshotProvider.SnapshotChanged -= OnSnapshotChanged;

            return Task.CompletedTask;
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
            return s_containsGraphCommand;
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
                await InitializeAsync().ConfigureAwait(false);

                var actionHandlers = GraphActionHandlers.Where(x => x.Value.CanHandleRequest(context));
                var shouldTrackChanges = actionHandlers.Aggregate(
                    false, (previousTrackFlag, handler) => previousTrackFlag || handler.Value.HandleRequest(context));

                lock (_expandedGraphContextsLock)
                {
                    if (shouldTrackChanges && !ExpandedGraphContexts.Contains(context))
                    {
                        // Remember this graph context in order to track changes.
                        // When references change, we will adjust children of this graph as necessary
                        ExpandedGraphContexts.Add(context);
                    }
                }
            }
            finally
            {
                // OnCompleted must be called to display changes 
                context.OnCompleted();
            }
        }

        /// <summary>
        /// ProjectContextChanged gets fired everytime dependencies change for projects accross solution.
        /// ExpandedGraphContexts contain all nodes that we need to check for potential updates in their 
        /// children dependencies.
        /// </summary>
        private void OnSnapshotChanged(object sender, SnapshotChangedEventArgs e)
        {
            var snapshot = e.Snapshot;
            if (snapshot == null)
            {
                return;
            }

            lock (_changedContextsQueueLock)
            {
                _changedContextsQueue[snapshot.ProjectPath] = e;

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
                List<SnapshotChangedEventArgs> queue = null;

                lock (_changedContextsQueueLock)
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
        internal Task TrackChangesAsync(SnapshotChangedEventArgs updatedProjectContext)
        {
            IList<IGraphContext> expandedContexts;
            lock (_expandedGraphContextsLock)
            {
                expandedContexts = ExpandedGraphContexts.ToList();
            }

            if (expandedContexts.Count == 0)
            {
                return Task.CompletedTask;
            }

            var actionHandlers = GraphActionHandlers.Where(x => x.Value.CanHandleChanges());
            if (!actionHandlers.Any())
            {
                return Task.CompletedTask;
            }

            foreach (var graphContext in expandedContexts.ToList())
            {
                try
                {
                    actionHandlers.ForEach(x => x.Value.HandleChanges(graphContext, updatedProjectContext));
                }
                finally
                {
                    // Calling OnCompleted ensures that the changes are reflected in UI
                    graphContext.OnCompleted();
                }
            }

            return Task.CompletedTask;
        }

        private void RegisterIcons(IEnumerable<ImageMoniker> icons)
        {
            lock (_knownIconsLock)
            {
                if (icons == null)
                {
                    return;
                }

                icons = icons.Where(x => !KnownIcons.Contains(x)).ToList();
                if (!icons.Any())
                {
                    return;
                }

                KnownIcons.AddRange(icons);
            }

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                IVsImageService2 imageService = null;
                foreach (var icon in icons)
                {
                    // register icon 
                    if (imageService == null)
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        imageService = ServiceProvider.GetService<IVsImageService2, SVsImageService>();
                    }

                    imageService.TryAssociateNameWithMoniker(GetIconStringName(icon), icon);
                }
            });
        }

        public GraphNode AddGraphNode(
            IGraphContext graphContext, 
            string projectPath, 
            GraphNode parentNode, 
            IDependencyViewModel viewModel)
        {
            var modelId = viewModel.OriginalModel == null ? viewModel.Caption : viewModel.OriginalModel.Id;
            var newNodeId = GetGraphNodeId(projectPath, parentNode, modelId);
            return DoAddGraphNode(newNodeId, graphContext, projectPath, parentNode, viewModel);
        }

        public GraphNode AddTopLevelGraphNode(
            IGraphContext graphContext,
            string projectPath,
            IDependencyViewModel viewModel)
        {
            var newNodeId = GetTopLevelGraphNodeId(projectPath, viewModel.OriginalModel.GetTopLevelId());
            return DoAddGraphNode(newNodeId, graphContext, projectPath, parentNode: null, viewModel:viewModel);
        }

        private GraphNode DoAddGraphNode(
            GraphNodeId graphNodeId,
            IGraphContext graphContext,
            string projectPath,
            GraphNode parentNode,
            IDependencyViewModel viewModel)
        {
            RegisterIcons(viewModel.GetIcons());

            var newNode = graphContext.Graph.Nodes.GetOrCreate(graphNodeId, viewModel.Caption, null);
            newNode.SetValue(DgmlNodeProperties.Icon, GetIconStringName(viewModel.Icon));
            // priority sets correct order among peers
            newNode.SetValue(CodeNodeProperties.SourceLocation,
                             new SourceLocation(projectPath, new Position(viewModel.Priority, 0)));
            newNode.AddCategory(DependenciesGraphSchema.CategoryDependency);

            if (viewModel.OriginalModel != null)
            {
                newNode.SetValue(DependenciesGraphSchema.DependencyIdProperty, viewModel.OriginalModel.Id);
                newNode.SetValue(DependenciesGraphSchema.ResolvedProperty, viewModel.OriginalModel.Resolved);
            }

            graphContext.OutputNodes.Add(newNode);

            if (parentNode != null)
            {
                graphContext.Graph.Links.GetOrCreate(parentNode, newNode, /*label*/ null, CodeLinkCategories.Contains);
            }

            return newNode;
        }

        public void RemoveGraphNode(IGraphContext graphContext,
                                     string projectPath,
                                     string modelId,
                                     GraphNode parentNode)
        {
            var id = GetGraphNodeId(projectPath, parentNode, modelId);
            var nodeToRemove = graphContext.Graph.Nodes.Get(id);

            if (nodeToRemove != null)
            {
                graphContext.OutputNodes.Remove(nodeToRemove);
                graphContext.Graph.Nodes.Remove(nodeToRemove);
            }
        }

        private GraphNodeId GetGraphNodeId(string projectPath, GraphNode parentNode, string modelId)
        {
            var partialValues = new List<GraphNodeId>
            {
                GraphNodeId.GetPartial(CodeGraphNodeIdName.Assembly,
                                       new Uri(projectPath, UriKind.RelativeOrAbsolute)),
                GraphNodeId.GetPartial(CodeGraphNodeIdName.File,
                                       new Uri(modelId.ToLowerInvariant(), UriKind.RelativeOrAbsolute))
            };

            var parents = string.Empty;
            if (parentNode != null)
            {
                // to ensure Graph id for node is unique we add a hashcodes for node's parents separated by ';'
                parents = parentNode.Id.GetNestedValueByName<string>(CodeGraphNodeIdName.Namespace);
                if (string.IsNullOrEmpty(parents))
                {
                    var currentProject = parentNode.Id.GetValue(CodeGraphNodeIdName.Assembly) ?? projectPath;
                    parents = currentProject.GetHashCode().ToString();
                }
            }
            else
            {
                parents = projectPath.GetHashCode().ToString();
            }

            parents = parents + ";" + modelId.GetHashCode();
            partialValues.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.Namespace, parents));

            return GraphNodeId.GetNested(partialValues.ToArray());
        }

        private GraphNodeId GetTopLevelGraphNodeId(string projectPath, string modelId)
        {
            var partialValues = new List<GraphNodeId>
            {
                GraphNodeId.GetPartial(CodeGraphNodeIdName.Assembly, new Uri(projectPath, UriKind.RelativeOrAbsolute))
            };

            var projectFolder = Path.GetDirectoryName(projectPath)?.ToLowerInvariant() ?? string.Empty;
            var filePath = new Uri(Path.Combine(projectFolder, modelId.ToLowerInvariant()), UriKind.RelativeOrAbsolute);

            partialValues.Add(GraphNodeId.GetPartial(CodeGraphNodeIdName.File, filePath));

            return GraphNodeId.GetNested(partialValues.ToArray());
        }

        private static string GetIconStringName(ImageMoniker icon)
        {
            return $"{icon.Guid.ToString()};{icon.Id}";
        }
    }
}
