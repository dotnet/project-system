// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Provides services for a specific project configuration in a given search operation.
    /// </summary>
    /// <remarks>
    /// Instances of this type are obtained via <see cref="IDependenciesTreeProjectSearchContext.ForConfiguredProjectAsync"/>.
    /// </remarks>
    public interface IDependenciesTreeConfiguredProjectSearchContext
    {
        /// <summary>
        /// Gets a token that signals cancellation of the ongoing search operation.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets whether <paramref name="candidateText"/> matches the user's search.
        /// </summary>
        bool IsMatch(string candidateText);

        /// <summary>
        /// Submits <paramref name="item"/> as a search result.
        /// </summary>
        /// <remarks>
        /// The result is not submitted if:
        /// <list type="bullet">
        ///   <item><paramref name="item"/> is <see langword="null"/></item>
        ///   <item><see cref="CancellationToken"/> has been cancelled</item>
        ///   <item>the maximum number of items have already been returned, in which case <see cref="CancellationToken"/> is cancelled</item>
        /// </list>
        /// </remarks>
        void SubmitResult(IRelatableItem? item);
    }
}
