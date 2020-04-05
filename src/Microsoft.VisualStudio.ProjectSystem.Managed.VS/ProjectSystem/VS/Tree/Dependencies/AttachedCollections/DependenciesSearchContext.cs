// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using Microsoft.Internal.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Provides services throughout the lifetime of a search operation.
    /// </summary>
    internal sealed class DependenciesSearchContext : IDisposable
    {
        private readonly string _searchString;
        private readonly int _maximumResults;
        private readonly Action<ISearchResult> _resultAccumulator;
        private readonly CancellationTokenSource _cts;
        private int _submittedResultCount;

        public DependenciesSearchContext(IRelationshipSearchParameters parameters, Action<ISearchResult> resultAccumulator)
        {
            _searchString = parameters.SearchQuery.SearchString;
            _maximumResults = checked((int)parameters.MaximumResults);
            _resultAccumulator = resultAccumulator;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(parameters.CancellationToken);
        }

        /// <summary>
        /// Gets a token that aborts the search operation if cancelled.
        /// </summary>
        public CancellationToken CancellationToken => _cts.Token;

        /// <summary>
        /// Gets whether <paramref name="s"/> contains the search string.
        /// </summary>
        public bool IsMatch(string s) => s.IndexOf(_searchString, StringComparisons.UserEnteredSearchTermIgnoreCase) != -1;

        /// <summary>
        /// Submits <paramref name="item"/> as a search result.
        /// </summary>
        /// <remarks>
        /// The result is not submitted if:
        /// <list type="bullet">
        ///     <item><paramref name="item"/> is <see langword="null"/></item>
        ///     <item><see cref="CancellationToken"/> has been cancelled</item>
        ///     <item>the maximum number of items have already been returned, in which case <see cref="CancellationToken"/> is cancelled</item>
        /// </list>
        /// </remarks>
        public void SubmitResult(object? item)
        {
            if (item == null || CancellationToken.IsCancellationRequested)
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

        public void Dispose()
        {
            _cts.Dispose();
        }

        private sealed class DependenciesSearchResult : ISearchResult
        {
            private readonly object _item;

            public DependenciesSearchResult(object item) => _item = item;

            public object GetDisplayItem() => _item;
        }
    }
}
