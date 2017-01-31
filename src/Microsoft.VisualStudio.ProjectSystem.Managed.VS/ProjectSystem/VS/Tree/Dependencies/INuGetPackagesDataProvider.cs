// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// This contract allows other tree providers to reuse package nodes from Nuget nodes provider.
    /// See <see cref="SdkDependenciesSubTreeProvider"/> for example.
    /// </summary>
    internal interface INuGetPackagesDataProvider
    {
        /// <summary>
        /// For given node tries to find matching nuget package and adds direct package dependencies
        /// nodes to given node's children collection. Use case is, when other provider has a node,
        /// that is actually a package, it would call this method when GraphProvider is about to 
        /// check if node has children or not.
        /// </summary>
        /// <param name="packageItemSpec">Package reference items spec that is supposed to be associated 
        /// with the given node</param>
        /// <param name="originalNode">Node to fill children for, if it's is a package</param>
        void UpdateNodeChildren(string packageItemSpec, IDependencyNode originalNode);

        /// <summary>
        /// Allows to other providers to use nuget package dependencies search on a given node if it 
        /// turns out to be a nuget package.
        /// </summary>
        /// <param name="packageItemSpec">Package reference items spec for which we need to do a search</param>
        /// <param name="searchTerm">String to be searched</param>
        /// <returns></returns>
        Task<IEnumerable<IDependencyNode>> SearchAsync(string packageItemSpec, string searchTerm);
    }
}
