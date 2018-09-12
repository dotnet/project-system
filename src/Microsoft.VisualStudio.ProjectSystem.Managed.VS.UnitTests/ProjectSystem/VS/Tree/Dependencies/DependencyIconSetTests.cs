// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
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
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition);
            var iconSet2 = new DependencyIconSet(
                icon: KnownMonikers.AboutBox,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition);

            Assert.True(iconSet1.Equals(iconSet2));
        }

        [Fact]
        public void WhenIconSetsHaveSameIcons_ShouldHaveSameHashCode()
        {
            var iconSet1 = new DependencyIconSet(
                icon: KnownMonikers.AboutBox,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition);
            var iconSet2 = new DependencyIconSet(
                icon: KnownMonikers.AboutBox,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition);

            Assert.True(iconSet1.GetHashCode() == iconSet2.GetHashCode());
        }

        [Fact]
        public void WhenIconSetsHaveDifferentIcons_ShouldNotBeEqual()
        {
            var iconSet1 = new DependencyIconSet(
                icon: KnownMonikers.AboutBox,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition);
            var iconSet2 = new DependencyIconSet(
                icon: KnownMonikers.PackageReference,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition);

            Assert.False(iconSet1.Equals(iconSet2));
        }

        [Fact]
        public void WhenIconSetsHaveDifferentIcons_ShouldHaveDifferentHashCode()
        {
            var iconSet1 = new DependencyIconSet(
                icon: KnownMonikers.AboutBox,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition);
            var iconSet2 = new DependencyIconSet(
                icon: KnownMonikers.PackageReference,
                expandedIcon: KnownMonikers.AboutBox,
                unresolvedIcon: KnownMonikers.AbsolutePosition,
                unresolvedExpandedIcon: KnownMonikers.AbsolutePosition);

            Assert.False(iconSet1.GetHashCode() == iconSet2.GetHashCode());
        }
    }
}
