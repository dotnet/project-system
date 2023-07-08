// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public sealed class MetadataExtensionsTests
    {
        [Fact]
        public void GetStringProperty_WhenNotFound()
        {
            var dic = ImmutableDictionary<string, string>.Empty;
            Assert.Null(dic.GetStringProperty("key"));
        }

        [Fact]
        public void GetStringProperty_WhenFound()
        {
            var dic = ImmutableDictionary<string, string>.Empty.Add("key", "value");
            Assert.Equal("value", dic.GetStringProperty("key"));
        }

        [Fact]
        public void GetStringProperty_WhenEmpty()
        {
            var dic = ImmutableDictionary<string, string>.Empty.Add("key", "");
            Assert.Null(dic.GetStringProperty("key"));
        }

        [Fact]
        public void GetStringProperty_WhenWhiteSpace()
        {
            var dic = ImmutableDictionary<string, string>.Empty.Add("key", " ");
            Assert.Equal(" ", dic.GetStringProperty("key"));
        }

        [Fact]
        public void TryGetStringProperty_WhenNotFound()
        {
            var dic = ImmutableDictionary<string, string>.Empty;
            Assert.False(dic.TryGetStringProperty("key", out string? value));
            Assert.Null(value);
        }

        [Fact]
        public void TryGetStringProperty_WhenFound()
        {
            var dic = ImmutableDictionary<string, string>.Empty.Add("key", "value");
            Assert.True(dic.TryGetStringProperty("key", out string? value));
            Assert.Equal("value", value);
        }

        [Fact]
        public void GetBoolProperty_WhenNotFound()
        {
            var dic = ImmutableDictionary<string, string>.Empty;
            Assert.Null(dic.GetBoolProperty("key"));
        }

        [Theory]
        [InlineData("True",   true)]
        [InlineData("true",   true)]
        [InlineData("TRUE",   true)]
        [InlineData("False",  false)]
        [InlineData("false",  false)]
        [InlineData("FALSE",  false)]
        [InlineData("banana", null)]
        [InlineData("",       null)]
        [InlineData("    ",   null)]
        public void GetBoolProperty_WhenFound(string actual, bool? expected)
        {
            var dic = ImmutableDictionary<string, string>.Empty.Add("key", actual);
            Assert.Equal(expected, dic.GetBoolProperty("key"));
        }

        [Fact]
        public void TryGetBoolProperty_WhenNotFound()
        {
            var dic = ImmutableDictionary<string, string>.Empty;
            Assert.False(dic.TryGetBoolProperty("key", out bool value));
            Assert.False(value);
        }

        [Theory]
        [InlineData("True",   true,  true)]
        [InlineData("true",   true,  true)]
        [InlineData("TRUE",   true,  true)]
        [InlineData("False",  true,  false)]
        [InlineData("false",  true,  false)]
        [InlineData("FALSE",  true,  false)]
        [InlineData("banana", false, false)]
        [InlineData("",       false, false)]
        [InlineData("    ",   false, false)]
        public void TryGetBoolProperty_WhenFound(string actual, bool returnValue, bool outValue)
        {
            var dic = ImmutableDictionary<string, string>.Empty.Add("key", actual);
            Assert.Equal(returnValue, dic.TryGetBoolProperty("key", out bool value));
            Assert.Equal(outValue, value);
        }

        [Fact]
        public void GetEnumProperty_WhenNotFound()
        {
            var dic = ImmutableDictionary<string, string>.Empty;
            Assert.Null(dic.GetEnumProperty<TestEnum>("key"));
        }

        [Theory]
        [InlineData("Member1", TestEnum.Member1)]
        [InlineData("Member2", TestEnum.Member2)]
        [InlineData("Member3", TestEnum.Member3)]
        [InlineData("MEMBER1", TestEnum.Member1)]
        [InlineData("member1", TestEnum.Member1)]
        [InlineData("",        null)]
        [InlineData("    ",    null)]
        public void GetEnumProperty_WhenFound(string actual, TestEnum? expected)
        {
            var dic = ImmutableDictionary<string, string>.Empty.Add("key", actual);
            Assert.Equal(expected, dic.GetEnumProperty<TestEnum>("key"));
        }

        [Fact]
        public void TryGetEnumProperty_WhenNotFound()
        {
            var dic = ImmutableDictionary<string, string>.Empty;
            Assert.False(dic.TryGetEnumProperty("key", out TestEnum value));
            Assert.Equal(default, value);
        }

        [Theory]
        [InlineData("Member1", true,  TestEnum.Member1)]
        [InlineData("Member2", true,  TestEnum.Member2)]
        [InlineData("Member3", true,  TestEnum.Member3)]
        [InlineData("MEMBER1", true,  TestEnum.Member1)]
        [InlineData("member1", true,  TestEnum.Member1)]
        [InlineData("",        false, default(TestEnum))]
        [InlineData("    ",    false, default(TestEnum))]
        public void TryGetEnumProperty_WhenFound(string actual, bool returnValue, TestEnum outValue)
        {
            var dic = ImmutableDictionary<string, string>.Empty.Add("key", actual);
            Assert.Equal(returnValue, dic.TryGetEnumProperty("key", out TestEnum value));
            Assert.Equal(outValue, value);
        }

        public enum TestEnum { Member1, Member2, Member3 }
    }
}
