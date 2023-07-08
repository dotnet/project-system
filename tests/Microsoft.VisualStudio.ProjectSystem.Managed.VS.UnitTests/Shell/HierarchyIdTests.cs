// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Shell
{
    public class HierarchyIdTests
    {
        [Fact]
        public void Constructor1_ReturnsEmpty()
        {
            var hierarchyId = new HierarchyId();

            Assert.True(hierarchyId.IsEmpty);
        }

        [Theory]
        [InlineData(0)]
        [InlineData((uint)VSConstants.VSITEMID.Nil)]
        [InlineData((uint)VSConstants.VSITEMID.Root)]
        [InlineData((uint)VSConstants.VSITEMID.Selection)]
        public void Constructor_ValueAsId_SetsId(uint id)
        {
            var hierarchyId = new HierarchyId(id);

            Assert.Equal(id, hierarchyId.Id);
        }

        [Fact]
        public void IsRoot_WhenIdIsRoot_ReturnsTrue()
        {
            var hierarchyId = new HierarchyId((uint)VSConstants.VSITEMID.Root);

            Assert.True(hierarchyId.IsRoot);
        }

        [Theory]
        [InlineData(0)]
        [InlineData((uint)VSConstants.VSITEMID.Nil)]
        [InlineData((uint)VSConstants.VSITEMID.Selection)]
        public void IsRoot_WhenIdIsNotRoot_ReturnsFalse(uint id)
        {
            var hierarchyId = new HierarchyId(id);

            Assert.False(hierarchyId.IsRoot);
        }

        [Fact]
        public void IsSelection_WhenIdIsSelection_ReturnsTrue()
        {
            var hierarchyId = new HierarchyId((uint)VSConstants.VSITEMID.Selection);

            Assert.True(hierarchyId.IsSelection);
        }

        [Theory]
        [InlineData(0)]
        [InlineData((uint)VSConstants.VSITEMID.Nil)]
        [InlineData((uint)VSConstants.VSITEMID.Root)]
        public void IsSelection_WhenIdIsNotSelection_ReturnsFalse(uint id)
        {
            var hierarchyId = new HierarchyId(id);

            Assert.False(hierarchyId.IsSelection);
        }

        [Fact]
        public void IsEmpty_WhenIdIsEmpty_ReturnsTrue()
        {
            var hierarchyId = new HierarchyId(0);

            Assert.True(hierarchyId.IsEmpty);
        }

        [Theory]
        [InlineData((uint)VSConstants.VSITEMID.Nil)]
        [InlineData((uint)VSConstants.VSITEMID.Root)]
        [InlineData((uint)VSConstants.VSITEMID.Selection)]
        public void IsEmpty_WhenIdIsNotEmpty_ReturnsFalse(uint id)
        {
            var hierarchyId = new HierarchyId(id);

            Assert.False(hierarchyId.IsEmpty);
        }

        [Fact]
        public void IsNil_WhenIdIsNil_ReturnsTrue()
        {
            var hierarchyId = new HierarchyId((uint)VSConstants.VSITEMID.Nil);

            Assert.True(hierarchyId.IsNil);
        }

        [Theory]
        [InlineData(0)]
        [InlineData((uint)VSConstants.VSITEMID.Root)]
        [InlineData((uint)VSConstants.VSITEMID.Selection)]
        public void IsEmpty_WhenIdIsNotNil_ReturnsFalse(uint id)
        {
            var hierarchyId = new HierarchyId(id);

            Assert.False(hierarchyId.IsNil);
        }

        [Fact]
        public void IsNilOrEmpty_WhenIdIsNil_ReturnsTrue()
        {
            var hierarchyId = new HierarchyId((uint)VSConstants.VSITEMID.Nil);

            Assert.True(hierarchyId.IsNilOrEmpty);
        }

        [Fact]
        public void IsNilOrEmpty_WhenIdIsEmpty_ReturnsTrue()
        {
            var hierarchyId = new HierarchyId(0);

            Assert.True(hierarchyId.IsNilOrEmpty);
        }

        [Theory]
        [InlineData((uint)VSConstants.VSITEMID.Root)]
        [InlineData((uint)VSConstants.VSITEMID.Selection)]
        public void IsEmpty_WhenIdIsNotNilOrEmpty_ReturnsFalse(uint id)
        {
            var hierarchyId = new HierarchyId(id);

            Assert.False(hierarchyId.IsNilOrEmpty);
        }

        [Fact]
        public void Root_IsRoot_ReturnsTrue()
        {
            Assert.True(HierarchyId.Root.IsRoot);
        }

        [Fact]
        public void Selection_IsSelection_ReturnsTrue()
        {
            Assert.True(HierarchyId.Selection.IsSelection);
        }

        [Fact]
        public void Nil_IsNil_ReturnsTrue()
        {
            Assert.True(HierarchyId.Nil.IsNil);
        }

        [Theory]
        [InlineData(0)]
        [InlineData((uint)VSConstants.VSITEMID.Nil)]
        [InlineData((uint)VSConstants.VSITEMID.Root)]
        [InlineData((uint)VSConstants.VSITEMID.Selection)]
        public void Implicit_ToUInt32_ReturnsId(uint id)
        {
            var hierarchyId = new HierarchyId(id);

            uint result = hierarchyId;

            Assert.Equal(id, result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData((uint)VSConstants.VSITEMID.Nil)]
        [InlineData((uint)VSConstants.VSITEMID.Root)]
        [InlineData((uint)VSConstants.VSITEMID.Selection)]
        public void Implicit_ToHierarchyId_SetsId(uint id)
        {
            HierarchyId hierarchyId = id;

            Assert.Equal(id, hierarchyId.Id);
        }
    }
}
