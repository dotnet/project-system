// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    public sealed class FlagsStringMatcherTests
    {
        [Fact]
        public void Matches_ThrowsWhenNullString()
        {
            var detector = new FlagsStringMatcher(ProjectTreeFlags.Empty);

            Assert.Throws<ArgumentNullException>(() => detector.Matches(null!));
        }

        [Theory]
        [InlineData("")]
        [InlineData("APPLE")]
        [InlineData("BANANA")]
        [InlineData("APPLE BANANA")]
        public void Matches_EmptyFlags(string s)
        {
            var detector = new FlagsStringMatcher(ProjectTreeFlags.Empty);

            Assert.True(detector.Matches(s));
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("APPLE", true)]
        [InlineData("APPLE APPLE", true)]
        [InlineData("apple", false)]
        [InlineData("BANANA", false)]
        [InlineData("SNAPPLE", false)]
        [InlineData("APPLE BANANA", true)]
        [InlineData("BANANA APPLE", true)]
        [InlineData("BANANA APPLE CARROT", true)]
        [InlineData("APPLE BANANA APPLE", true)]
        public void Matches_SingleFlag(string s, bool expected)
        {
            var detector = new FlagsStringMatcher(ProjectTreeFlags.Create("APPLE"));

            Assert.Equal(expected, detector.Matches(s));
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("APPLE", true)]
        [InlineData("apple", true)]
        [InlineData("BANANA", false)]
        [InlineData("SNAPPLE", false)]
        [InlineData("APPLE BANANA", true)]
        [InlineData("BANANA APPLE", true)]
        [InlineData("BANANA apple", true)]
        [InlineData("BANANA APPLE CARROT", true)]
        [InlineData("APPLE BANANA APPLE", true)]
        public void Matches_SingleFlag_CaseInsensitive(string s, bool expected)
        {
            var detector = new FlagsStringMatcher(ProjectTreeFlags.Create("APPLE"), RegexOptions.IgnoreCase);

            Assert.Equal(expected, detector.Matches(s));
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("APPLE", false)]
        [InlineData("BANANA", false)]
        [InlineData("APPLE BANANA", true)]
        [InlineData("BANANA APPLE", true)]
        [InlineData("BANANA APPLE CARROT", true)]
        public void Matches_MultipleFlags(string s, bool expected)
        {
            var detector = new FlagsStringMatcher(ProjectTreeFlags.Create("APPLE") + ProjectTreeFlags.Create("BANANA"));

            Assert.Equal(expected, detector.Matches(s));
        }
    }
}
