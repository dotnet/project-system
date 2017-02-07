// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes.ViewProviders
{
    [Export(typeof(IDependenciesGraphViewProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class GenericGraphViewProvider : GraphViewProviderBase
    {
        public const int Order = 0;

        [ImportingConstructor]
        public GenericGraphViewProvider(IDependenciesGraphBuilder builder)
            : base(builder)
        {
        }
    }
}
