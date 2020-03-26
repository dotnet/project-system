// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Dependency tree items may implement this interface to handle their own <see cref="KnownRelationships.Contains"/> queries.
    /// </summary>
    internal interface IContainsAttachedItems
    {
        /// <summary>
        /// Gets the source for this item's contained items.
        /// May return <see langword="null"/> if no items will ever be contained.
        /// </summary>
        /// <remarks>
        /// If there are currently no contained items, but there may later be, then
        /// an empty source should be returned so that it may later notify the tree
        /// when it contains items.
        /// </remarks>
        IAttachedCollectionSource? ContainsAttachedCollectionSource { get; }
    }
}
