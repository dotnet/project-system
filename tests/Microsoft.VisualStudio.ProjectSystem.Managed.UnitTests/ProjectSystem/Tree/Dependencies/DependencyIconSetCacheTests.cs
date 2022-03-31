// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies
{
    public sealed class DependencyIconSetCacheTests
    {
        [Fact]
        public void GetOrAddIconSet()
        {
            var cache = new DependencyIconSetCache();

            ImageMoniker icon1 = KnownMonikers.AboutBox;
            ImageMoniker icon2 = KnownMonikers.ZoomToggle;

            var iconSet1a = new DependencyIconSet(icon1, icon1, icon1, icon1, icon1, icon1);
            var iconSet1b = new DependencyIconSet(icon1, icon1, icon1, icon1, icon1, icon1);

            Assert.Equal(iconSet1a, iconSet1b);
            Assert.NotSame(iconSet1a, iconSet1b);

            Assert.Same(iconSet1a, cache.GetOrAddIconSet(iconSet1a));
            Assert.Same(iconSet1a, cache.GetOrAddIconSet(iconSet1b));
            Assert.Same(iconSet1a, cache.GetOrAddIconSet(icon1, icon1, icon1, icon1, icon1, icon1));

            var iconSet2 = new DependencyIconSet(icon2, icon2, icon2, icon2, icon2, icon2);

            Assert.NotEqual(iconSet1a, iconSet2);

            Assert.Same(iconSet2, cache.GetOrAddIconSet(iconSet2));
            Assert.Same(iconSet2, cache.GetOrAddIconSet(icon2, icon2, icon2, icon2, icon2, icon2));
        }
    }
}
