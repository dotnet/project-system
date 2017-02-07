// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
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
        /// Must be unique, represents a type of the provider that will be associated
        /// with provider's nodes (via project tree flags)
        /// </summary>
        string ProviderType { get; }

        /// <summary>
        /// Allows provider's root node when it is empty
        /// </summary>
        bool ShouldBeVisibleWhenEmpty { get; }

        /// <summary>
        /// Returns a node metadata for given nodeId.
        /// </summary>
        IDependencyModel CreateRootDependencyNode();

        /// <summary>
        /// Raised when provider's dependencies changed 
        /// </summary>
        event EventHandler<DependenciesChangedEventArgs> DependenciesChanged;
    }

    public class DependenciesChangedEventArgs
    {
        public DependenciesChangedEventArgs(IProjectDependenciesSubTreeProvider provider,
                                            string targetShortOrFullName,
                                            IDependenciesChanges changes,
                                            IProjectCatalogSnapshot catalogs,
                                            IImmutableDictionary<NamedIdentity, IComparable> dataSourceVersions)
        {
            Provider = provider;
            TargetShortOrFullName = targetShortOrFullName;
            Changes = changes;
            Catalogs = catalogs;
            DataSourceVersions = dataSourceVersions;
        }

        public IProjectDependenciesSubTreeProvider Provider { get; }

        public string TargetShortOrFullName { get; }
        public IDependenciesChanges Changes { get; }
        public IProjectCatalogSnapshot Catalogs { get; }
        public IImmutableDictionary<NamedIdentity, IComparable> DataSourceVersions { get; }
    }
}
