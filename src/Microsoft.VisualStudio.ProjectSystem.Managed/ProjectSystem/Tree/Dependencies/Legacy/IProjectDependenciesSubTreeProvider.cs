// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Contract responsible for providing data about project dependencies of a specific type,
    /// for example assemblies, projects, packages etc.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
    public interface IProjectDependenciesSubTreeProvider
    {
        /// <summary>
        /// Gets a string that uniquely identifies the type of dependency nodes emitted by this provider.
        /// </summary>
        /// <remarks>
        /// This string will be associated with provider's nodes (via project tree flags).
        /// </remarks>
        string ProviderType { get; }

        /// <summary>
        /// Returns the root (group) node for this provider's dependency nodes.
        /// </summary>
        /// <remarks>
        /// Despite the method's name, implementations may return the same instance for repeated
        /// calls, so long as the returned value is immutable.
        /// </remarks>
        IDependencyModel CreateRootDependencyNode();

        /// <summary>
        /// Raised when this provider's dependencies changed.
        /// </summary>
        event EventHandler<DependenciesChangedEventArgs> DependenciesChanged;
    }

    public sealed class DependenciesChangedEventArgs
    {
        [Obsolete("Constructor includes unused properties. Use the non-obsolete overload instead.")]
        public DependenciesChangedEventArgs(
            IProjectDependenciesSubTreeProvider provider,
            string? targetShortOrFullName,
            IDependenciesChanges changes,
            IProjectCatalogSnapshot? catalogs,
            IImmutableDictionary<NamedIdentity, IComparable>? dataSourceVersions)
            : this(provider, changes, CancellationToken.None)
        {
        }

        [Obsolete("Constructor includes unused properties. Use the non-obsolete overload instead.")]
        public DependenciesChangedEventArgs(
            IProjectDependenciesSubTreeProvider provider,
            string? targetShortOrFullName,
            IDependenciesChanges changes,
            CancellationToken token)
            : this(provider, changes, CancellationToken.None)
        {
            Provider = provider;
            Changes = changes;
            Token = token;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DependenciesChangedEventArgs"/>.
        /// </summary>
        /// <param name="provider">The subtree provider whose dependencies changed.</param>
        /// <param name="changes">The collection of dependency changes.</param>
        /// <param name="token">A cancellation token that allows cancelling the update.</param>
        public DependenciesChangedEventArgs(
            IProjectDependenciesSubTreeProvider provider,
            IDependenciesChanges changes,
            CancellationToken token)
        {
            Provider = provider;
            Changes = changes;
            Token = token;
        }

        public IProjectDependenciesSubTreeProvider Provider { get; }

        [Obsolete("It is not possible to model configured dependencies via IProjectDependenciesSubTreeProvider.")]
        public string? TargetShortOrFullName => null;

        public IDependenciesChanges Changes { get; }

        public CancellationToken Token { get; }
    }
}
