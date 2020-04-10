// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.Actions
{
    /// <summary>
    ///     Base class for graph action handlers, providing access to snapshot dependency data and
    ///     instances of type <see cref="IDependenciesGraphViewProvider"/>.
    /// </summary>
    internal abstract class GraphActionHandlerBase : IDependenciesGraphActionHandler
    {
        [ImportMany] private readonly OrderPrecedenceImportCollection<IDependenciesGraphViewProvider> _viewProviders;

        protected GraphActionHandlerBase(IAggregateDependenciesSnapshotProvider aggregateSnapshotProvider)
        {
            AggregateSnapshotProvider = aggregateSnapshotProvider;

            _viewProviders = new OrderPrecedenceImportCollection<IDependenciesGraphViewProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst);
        }

        protected IAggregateDependenciesSnapshotProvider AggregateSnapshotProvider { get; }

        public abstract bool TryHandleRequest(IGraphContext graphContext);

        protected IDependenciesGraphViewProvider? FindViewProvider(IDependency dependency)
        {
            return _viewProviders.FirstOrDefaultValue((x, d) => x.SupportsDependency(d), dependency);
        }
    }
}
