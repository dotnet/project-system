// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// A top-level search context, common to all projects and all <see cref="IDependenciesTreeSearchProvider"/>
    /// instances.
    /// </summary>
    internal sealed class DependenciesTreeSearchContext : IDisposable
    {
        private readonly string _searchString;
        private readonly uint _maximumResults;
        private readonly Action<ISearchResult> _resultAccumulator;
        private readonly CancellationTokenSource _cts;
        private long _submittedResultCount; // long as there's no interlocked increment for uint32

        public DependenciesTreeSearchContext(IRelationshipSearchParameters parameters, Action<ISearchResult> resultAccumulator)
        {
            _searchString = parameters.SearchQuery.SearchString;
            _maximumResults = parameters.MaximumResults;
            _resultAccumulator = resultAccumulator;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(parameters.CancellationToken);
        }

        public CancellationToken CancellationToken => _cts.Token;

        public bool IsMatch(string candidateText) => candidateText.IndexOf(_searchString, StringComparisons.UserEnteredSearchTermIgnoreCase) != -1;

        public void SubmitResult(IRelatableItem? item)
        {
            if (item is null || CancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (Interlocked.Increment(ref _submittedResultCount) >= _maximumResults)
            {
                _cts.Cancel();
                return;
            }

            _resultAccumulator(new DependenciesSearchResult(item));
        }

        public void Dispose() => _cts.Dispose();

        private sealed class DependenciesSearchResult : ISearchResult
        {
            private readonly object _item;

            public DependenciesSearchResult(object item) => _item = item;

            public object GetDisplayItem() => _item;
        }
    }
}
