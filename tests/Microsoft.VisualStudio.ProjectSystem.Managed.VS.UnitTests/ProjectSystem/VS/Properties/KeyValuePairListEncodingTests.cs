// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties;

public class KeyValuePairListEncodingTests
{
    [Theory]
    [InlineData("key1=value1;key2=value2", new[] { "key1", "value1", "key2", "value2" })]
    [InlineData("key1=value1;;key2=value2", new[] { "key1", "value1", "key2", "value2" })]
    [InlineData("key1=value1;;;key2=value2", new[] { "key1", "value1", "key2", "value2" })]
    [InlineData("key1=value1;key2=value2;key3=value3", new[] { "key1", "value1", "key2", "value2", "key3", "value3" })]
    [InlineData("key1;key2=value2", new[] { "key1", "", "key2", "value2" })]
    [InlineData("key1;key2;key3=value3", new[] { "key1", "", "key2", "", "key3", "value3" })]
    [InlineData("key1;;;key3;;", new[] { "key1", "", "key3", "" })]
    [InlineData("", new string[0])]
    [InlineData(" ", new string[0])]
    [InlineData("=", new string[0])]
    [InlineData("/=", new[] { "=", "" })]
    [InlineData("key1=value1;/=value2=", new[] { "key1", "value1", "=value2", "" })]
    [InlineData("key1=value1;=value2", new[] { "key1", "value1" })]
    [InlineData("==", new string[0])]
    [InlineData("=/=", new string[0])]
    [InlineData("/==", new[] { "=", "" })]
    [InlineData(";", new string[0])]
    public void Parse_ValidInput_ReturnsExpectedPairs(string input, string[] expectedPairs)
    {
        var result = KeyValuePairListEncoding.Parse(input, ';').SelectMany(pair => new[] { pair.Name, pair.Value }).ToArray();
        Assert.Equal(expectedPairs, result);
    }

    [Theory]
    [InlineData(new[] { "key1", "value1", "key2", "value2" }, "key1=value1;key2=value2")]
    [InlineData(new[] { "key1", "value1", "key2", "value2", "key3", "value3" }, "key1=value1;key2=value2;key3=value3")]
    [InlineData(new[] { "key1", "", "key2", "value2" }, "key1;key2=value2")]
    [InlineData(new[] { "key1=", "", "key2=", "value2=" }, "key1/=;key2/==value2/=")]
    [InlineData(new[] { "key1", "", "key2", "", "key3", "value3" }, "key1;key2;key3=value3")]
    [InlineData(new string[0], "")]
    public void Format_ValidPairs_ReturnsExpectedString(string[] pairs, string expectedString)
    {
        var nameValuePairs = ToNameValues(pairs);
        var result = KeyValuePairListEncoding.Format(nameValuePairs, ';');
        Assert.Equal(expectedString, result);
        return;

        static IEnumerable<(string Name, string Value)> ToNameValues(IEnumerable<string> pairs)
        {
            using var e = pairs.GetEnumerator();
            while (e.MoveNext())
            {
                var name = e.Current;
                Assert.True(e.MoveNext());
                var value = e.Current;
                yield return (name, value);
            }
        }
    }
}
