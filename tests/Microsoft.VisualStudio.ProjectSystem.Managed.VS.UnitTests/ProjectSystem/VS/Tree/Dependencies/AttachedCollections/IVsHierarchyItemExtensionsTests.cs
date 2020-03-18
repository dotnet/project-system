// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    public sealed class IVsHierarchyItemExtensionsTests
    {
        [Fact]
        public void TryGetFlagsString()
        {
            IVsHierarchyItem item = CreateItemWithFlags("A B C");

            Assert.True(item.TryGetFlagsString(out string? s));
            Assert.NotNull(s);
            Assert.Equal("A B C", s);
        }

        private static IVsHierarchyItem CreateItemWithFlags(string flagsString)
        {
            var hierarchy = IVsHierarchyFactory.Create();
            hierarchy.ImplementGetProperty((VsHierarchyPropID)__VSHPROPID7.VSHPROPID_ProjectTreeCapabilities, flagsString);

            var identity = new Mock<IVsHierarchyItemIdentity>(MockBehavior.Strict);
            identity.SetupGet(i => i.NestedHierarchy).Returns(hierarchy);
            identity.SetupGet(i => i.NestedItemID).Returns(0);

            var hierarchyItem = new Mock<IVsHierarchyItem>(MockBehavior.Strict);
            hierarchyItem.SetupGet(i => i.HierarchyIdentity).Returns(identity.Object);

            return hierarchyItem.Object;
        }
    }
}
