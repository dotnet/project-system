// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.GraphModel;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    /// <summary>
    /// Keeps registered graph contexts up to date with project dependency changes.
    /// </summary>
    /// <remarks>
    /// Listens to aggregate snapshot changes and updates registered graph contexts accordingly.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IDependenciesGraphChangeTracker : IDisposable
    {
        /// <summary>
        /// Registers <paramref name="context"/> to be updated as project dependencies change.
        /// </summary>
        /// <remarks>
        /// There is no way to unregister these contexts. Internally, weak references are held
        /// so that registered context objects may still be garbage collected.
        /// </remarks>
        void RegisterGraphContext(IGraphContext context);
    }
}
