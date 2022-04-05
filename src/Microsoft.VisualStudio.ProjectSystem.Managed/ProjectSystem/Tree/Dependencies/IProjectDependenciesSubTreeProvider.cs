// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
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

    internal interface IProjectDependenciesSubTreeProvider2 : IProjectDependenciesSubTreeProvider
    {
        /// <summary>
        /// Gets a flag that uniquely identifies the group node among its siblings.
        /// </summary>
        /// <remarks>
        /// For example <see cref="DependencyTreeFlags.ProjectDependencyGroup"/>.
        /// </remarks>
        ProjectTreeFlags GroupNodeFlag { get; }
    }

    public sealed class DependenciesChangedEventArgs
    {
        [Obsolete("Constructor includes unused properties")]
        public DependenciesChangedEventArgs(
            IProjectDependenciesSubTreeProvider provider,
            string? targetShortOrFullName,
            IDependenciesChanges changes,
            IProjectCatalogSnapshot catalogs,
            IImmutableDictionary<NamedIdentity, IComparable> dataSourceVersions)
            : this(provider, targetShortOrFullName, changes, CancellationToken.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DependenciesChangedEventArgs"/>.
        /// </summary>
        /// <param name="provider">The subtree provider whose dependencies changed.</param>
        /// <param name="targetShortOrFullName">
        /// The short or full name of the target framework to which <paramref name="changes"/> apply.
        /// Optional if the project is not multi-targeting.
        /// </param>
        /// <param name="changes">The collection of dependency changes.</param>
        /// <param name="token">A cancellation token that allows cancelling the update.</param>
        public DependenciesChangedEventArgs(
            IProjectDependenciesSubTreeProvider provider,
            string? targetShortOrFullName,
            IDependenciesChanges changes,
            CancellationToken token)
        {
            Provider = provider;
            TargetShortOrFullName = targetShortOrFullName;
            Changes = changes;
            Token = token;
        }

        public IProjectDependenciesSubTreeProvider Provider { get; }
        public string? TargetShortOrFullName { get; }
        public IDependenciesChanges Changes { get; }
        public CancellationToken Token { get; }
    }
}
