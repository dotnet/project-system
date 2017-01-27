// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// </summary>
    internal interface INuGetPackagesDataProvider
    {
        void UpdateNodeChildren(string packageItemSpec, IDependencyNode originalNode);
        Task<IEnumerable<IDependencyNode>> SearchAsync(string packageItemSpec, string searchTerm);
    }
}
