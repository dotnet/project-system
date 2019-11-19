// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
