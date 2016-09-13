// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Contract responsible for providing data about project dependencies of a specific type,
    /// for example assemblies, projects, packages etc
    /// </summary>
    public interface IProjectDependenciesSubTreeProvider
    {
        /// <summary>
        /// Must be unique, represents a type of the provider that will be associated
        /// with provider's nodes (via project tree flags)
        /// </summary>
        string ProviderType { get; }

        /// <summary>
        /// Provider sub tree's root node info. It is created during provider's initialization.
        /// </summary>
        IDependencyNode RootNode { get; }

        /// <summary>
        /// Specifies if dependency sub node thinks that it is in error state. Different sub nodes
        /// can have different conditions for error state.
        /// </summary>
        bool IsInErrorState { get; }

        /// <summary>
        /// Allows sub node provider to explicitly show it's root node when it is empty
        /// </summary>
        bool ShouldBeVisibleWhenEmpty { get; }

        /// <summary>
        /// Returns a list of all icons used by nodes managed by given provider. It is used in 
        /// GraphProvider to register that in IVsImageService once and reuse later.
        /// </summary>
        IEnumerable<ImageMoniker> Icons { get; }

        /// <summary>
        /// Returns a node metadata for given nodeId.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        IDependencyNode GetDependencyNode(DependencyNodeId nodeId);

        /// <summary>
        /// Searches for given searchTerm under given node. It returns a hierarchy of nodes 
        /// that match search or null if nothing found.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        Task<IEnumerable<IDependencyNode>> SearchAsync(IDependencyNode node, string searchTerm);

        /// <summary>
        /// Raised when provider's dependencies changed 
        /// </summary>
        event EventHandler<DependenciesChangedEventArgs> DependenciesChanged;
    }

    public class DependenciesChangedEventArgs
    {
        public DependenciesChangedEventArgs(IProjectDependenciesSubTreeProvider provider,
                                            IDependenciesChangeDiff changes,
                                            IProjectCatalogSnapshot catalogs,
                                            IImmutableDictionary<NamedIdentity, IComparable> dataSourceVersions)
        {
            Provider = provider;
            Changes = changes;
            Catalogs = catalogs;
            DataSourceVersions = dataSourceVersions;
        }

        public IProjectDependenciesSubTreeProvider Provider { get; private set; }
        public IDependenciesChangeDiff Changes { get; private set; }
        public IProjectCatalogSnapshot Catalogs { get; private set; }
        public IImmutableDictionary<NamedIdentity, IComparable> DataSourceVersions { get; private set; }
    }
}
