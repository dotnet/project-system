// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.GraphModel;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    // Progression imports IGraphProviders as non-shared parts, which means if they themselves export other interfaces
    // imports of those in the same scope/container will see a different instance to what progression sees. This class works 
    // around that by importing DependenciesGraphProvider as shared and simply delegating onto it.

    [GraphProvider(Name = "Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.DependenciesGraphProvider",
                   ProjectCapability = ProjectCapability.DependenciesTree)]
    internal class DependenciesGraphProviderFactory : IGraphProvider
    {
        private readonly DependenciesGraphProvider _provider;

        [ImportingConstructor]
        internal DependenciesGraphProviderFactory(DependenciesGraphProvider provider)
        {
            _provider = provider;
        }

        public Graph? Schema => _provider.Schema;

        public void BeginGetGraphData(IGraphContext context) => _provider.BeginGetGraphData(context);

        public IEnumerable<GraphCommand> GetCommands(IEnumerable<GraphNode> nodes) => _provider.GetCommands(nodes);

        public T? GetExtension<T>(GraphObject graphObject, T previous) where T : class => _provider.GetExtension(graphObject, previous);
    }
}
