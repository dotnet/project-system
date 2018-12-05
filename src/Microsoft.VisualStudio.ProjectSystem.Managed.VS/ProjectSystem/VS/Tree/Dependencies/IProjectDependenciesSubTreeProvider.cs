// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Contract responsible for providing data about project dependencies of a specific type,
    /// for example assemblies, projects, packages etc.
    /// </summary>
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
        /// Returns the root node for this provider's dependency nodes.
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
        [Obsolete("Constructor includes unused properties")]
        public DependenciesChangedEventArgs(
            IProjectDependenciesSubTreeProvider provider,
            string targetShortOrFullName,
            IDependenciesChanges changes,
            IProjectCatalogSnapshot catalogs,
            IImmutableDictionary<NamedIdentity, IComparable> dataSourceVersions)
            : this(provider, targetShortOrFullName, changes, CancellationToken.None)
        {
        }

        public DependenciesChangedEventArgs(
            IProjectDependenciesSubTreeProvider provider,
            string targetShortOrFullName,
            IDependenciesChanges changes,
            CancellationToken token)
        {
            Provider = provider;
            TargetShortOrFullName = targetShortOrFullName;
            Changes = changes;
            Token = token;
        }

        public IProjectDependenciesSubTreeProvider Provider { get; }
        public string TargetShortOrFullName { get; }
        public IDependenciesChanges Changes { get; }
        public CancellationToken Token { get; }
    }
}
