// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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

        [Theory]
        [InlineData("TargetNode $TFM:FooBar", "FooBar")]
        [InlineData("$TFM:FooBar TargetNode", "FooBar")]
        [InlineData("$TFM:FooBar TargetNode OTHER", "FooBar")]
        [InlineData("OTHER $TFM:FooBar TargetNode", "FooBar")]
        [InlineData("TargetNode", null)]
        [InlineData("TARGETNODE $TFM:FooBar", null)]
        [InlineData("$TFM:FooBar", null)]
        [InlineData("TargetNode TFM:FooBar", null)]
        [InlineData("$TFM:", null)]
        [InlineData("", null)]
        public void TryFindTarget(string flagsString, string? expectedConfiguration)
        {
            IVsHierarchyItem item = CreateItemWithFlags(flagsString);

            var result = item.TryFindTarget(out string? actualConfiguration);

            Assert.Equal(actualConfiguration is not null, result);
            Assert.Equal(expectedConfiguration, actualConfiguration);
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
            hierarchyItem.SetupGet<IVsHierarchyItem?>(i => i.Parent).Returns((IVsHierarchyItem?)null);

            return hierarchyItem.Object;
        }
    }
}
