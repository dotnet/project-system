// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class PackageFrameworkAssembliesViewModelTests
    {
        [Fact]
        public void Resolved()
        {
            var model = new PackageFrameworkAssembliesViewModel();

            Assert.Equal("Framework Assemblies", model.Caption);
            Assert.Equal(GraphNodePriority.FrameworkAssembly, model.Priority);
            Assert.Equal(KnownMonikers.Library, model.Icon);
            Assert.Equal(KnownMonikers.Library, model.ExpandedIcon);
            Assert.Equal(DependencyTreeFlags.FrameworkAssembliesNode, model.Flags);
        }
    }
}
