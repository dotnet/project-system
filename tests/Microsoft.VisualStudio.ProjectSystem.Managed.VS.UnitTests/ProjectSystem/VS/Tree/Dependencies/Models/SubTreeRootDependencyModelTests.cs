// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class SubTreeRootDependencyModelTests
    {
        [Fact]
        public void SubTreeRootDependencyModelTest()
        {
            var iconSet = new DependencyIconSet(
                icon: KnownMonikers.AboutBox,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition);

            var model = new SubTreeRootDependencyModel(
                "myProvider",
                "myRoot",
                iconSet);

            Assert.Equal("myProvider", model.ProviderType);
            Assert.Equal("myRoot", model.Path);
            Assert.Equal("myRoot", model.OriginalItemSpec);
            Assert.Equal("myRoot", model.Caption);
            Assert.Same(iconSet, model.IconSet);
            Assert.Equal(KnownMonikers.AboutBox, model.Icon);
            Assert.Equal(KnownMonikers.AboutBox, model.ExpandedIcon);
            Assert.Equal(KnownMonikers.AbsolutePosition, model.UnresolvedIcon);
            Assert.Equal(KnownMonikers.AbsolutePosition, model.UnresolvedExpandedIcon);
            Assert.Equal(DependencyTreeFlags.SubTreeRootNode, model.Flags);
        }
    }
}
