// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies
{
    public class DependencyIconSetTests
    {
        [Fact]
        public void WhenIconSetsHaveSameIcons_ShouldBeEqual()
        {
            var iconSet1 = new DependencyIconSet(
                icon: KnownMonikers.AboutBox,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition,
                implicitIcon: KnownMonikers.AbsolutePosition,
                implicitExpandedIcon: KnownMonikers.AbsolutePosition);
            var iconSet2 = new DependencyIconSet(
                icon: KnownMonikers.AboutBox,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition,
                implicitIcon: KnownMonikers.AbsolutePosition,
                implicitExpandedIcon: KnownMonikers.AbsolutePosition);

            Assert.True(iconSet1.Equals(iconSet2));
        }

        [Fact]
        public void WhenIconSetsHaveSameIcons_ShouldHaveSameHashCode()
        {
            var iconSet1 = new DependencyIconSet(
                icon: KnownMonikers.AboutBox,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition,
                implicitIcon: KnownMonikers.AbsolutePosition,
                implicitExpandedIcon: KnownMonikers.AbsolutePosition);
            var iconSet2 = new DependencyIconSet(
                icon: KnownMonikers.AboutBox,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition,
                implicitIcon: KnownMonikers.AbsolutePosition,
                implicitExpandedIcon: KnownMonikers.AbsolutePosition);

            Assert.True(iconSet1.GetHashCode() == iconSet2.GetHashCode());
        }

        [Fact]
        public void WhenIconSetsHaveDifferentIcons_ShouldNotBeEqual()
        {
            var iconSet1 = new DependencyIconSet(
                icon: KnownMonikers.AboutBox,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition,
                implicitIcon: KnownMonikers.AbsolutePosition,
                implicitExpandedIcon: KnownMonikers.AbsolutePosition);
            var iconSet2 = new DependencyIconSet(
                icon: KnownMonikers.PackageReference,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition,
                implicitIcon: KnownMonikers.AbsolutePosition,
                implicitExpandedIcon: KnownMonikers.AbsolutePosition);

            Assert.False(iconSet1.Equals(iconSet2));
        }

        [Fact]
        public void WhenIconSetsHaveDifferentIcons_ShouldHaveDifferentHashCode()
        {
            var iconSet1 = new DependencyIconSet(
                icon: KnownMonikers.AboutBox,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition,
                implicitIcon: KnownMonikers.AbsolutePosition,
                implicitExpandedIcon: KnownMonikers.AbsolutePosition);
            var iconSet2 = new DependencyIconSet(
                icon: KnownMonikers.PackageReference,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition,
                implicitIcon: KnownMonikers.AbsolutePosition,
                implicitExpandedIcon: KnownMonikers.AbsolutePosition);

            Assert.False(iconSet1.GetHashCode() == iconSet2.GetHashCode());
        }
    }
}
