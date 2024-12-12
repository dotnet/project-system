﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.InterceptingProjectProperties.BuildPropertyPage;

public class VBDefineConstantsEncodingTests
{
    [Theory]
    [InlineData("key1=\"value1\"", "key1=/\"value1/\"")]
    [InlineData("key1=\"value1\",key2=\"value2\"", "key1=/\"value1/\",key2=/\"value2/\"")]
    [InlineData("a=\"b\",c=\"easy\",as=123", "a=/\"b/\",c=/\"easy/\",as=123")]
    [InlineData("key1=\"value1\",key2=\"value2\",key3=\"value3\"", "key1=/\"value1/\",key2=/\"value2/\",key3=/\"value3/\"")]
    [InlineData("something=\"equals=this\"", "something=/\"equals/=this/\"")]
    [InlineData("oh=\"hey=there\",hi=\"didnt=see\",you=\"there\"", "oh=/\"hey/=there/\",hi=/\"didnt/=see/\",you=/\"there/\"")]
    [InlineData("path=\"/path/to/somewhere/\"", "path=/\"//path//to//somewhere/\"")]
    [InlineData("file=\"/path/is/here/\"", "file=/\"//path//is//here/\"")]
    [InlineData("files=\"path=/path/to/somewhere/\"", "files=/\"path/=//path//to//somewhere/\"")]
    [InlineData("a=1,a=2", "a=1,a=2")]
    [InlineData("key1  =\"value1  \"", "key1  =/\"value1  /\"")]
    [InlineData("  key1=\"  value1\"", "  key1=/\"  value1/\"")]
    [InlineData(" key1 =\" value1 \"", " key1 =/\" value1 /\"")]
    [InlineData(" =\" \"", " =/\" /\"")]
    [InlineData("key1=\"a b c d\"", "key1=/\"a b c d/\"")]
    [InlineData("key1=\"a=b=c=d\"", "key1=/\"a/=b/=c/=d/\"")]
    [InlineData("key1=\"a=b\"\"c=d\"", "key1=/\"a/=b/\"/\"c/=d/\"")]
    [InlineData("key1=\"ab\"\"cd\"", "key1=/\"ab/\"/\"cd/\"")]
    [InlineData("key1=\"\"ab\"\"cd\"\"ef\"", "key1=/\"/\"ab/\"/\"cd/\"/\"ef/\"")]
    [InlineData("key1=\"\"ab\"\"cd\"", "key1=/\"/\"ab/\"/\"cd/\"")]
    [InlineData("a b c d=\"value1\"", "a b c d=/\"value1/\"")]
    [InlineData("a=b=c=d=\"value1\"", "a=b/=c/=d/=/\"value1/\"")]
    [InlineData("a=\"b\"\"c=d=value1\"", "a=/\"b/\"/\"c/=d/=value1/\"")]
    [InlineData("\"ab\"\"cd=\"value1\"", "/\"ab/\"/\"cd=/\"value1/\"")]
    [InlineData("\"\"ab\"\"cd\"\"ef=\"value1\"", "/\"/\"ab/\"/\"cd/\"/\"ef=/\"value1/\"")]
    [InlineData("\"\"ab\"\"cd=\"value1\"", "/\"/\"ab/\"/\"cd=/\"value1/\"")]
    [InlineData("\"\"ab\"\"cd=\"     value1     \"", "/\"/\"ab/\"/\"cd=/\"     value1     /\"")]
    [InlineData("\"\"ab\"\"cd=\"value1     \"", "/\"/\"ab/\"/\"cd=/\"value1     /\"")]
    [InlineData("\"\"ab\"\"cd=\"     value1\"", "/\"/\"ab/\"/\"cd=/\"     value1/\"")]
    [InlineData("", "")]
    [InlineData("val/=ue", "val/=ue")]
    [InlineData("value","value")]
    [InlineData("value1,value2,value3", "value1,value2,value3")]
    [InlineData("value1,value2,value3,key4=\"value4\"", "value1,value2,value3,key4=/\"value4/\"")]
    [InlineData("value1,key2=\"value2\",value3,value4", "value1,key2=/\"value2/\",value3,value4")]
    [InlineData("value1,key2=\"value2\",value3,key4=\"value4\"", "value1,key2=/\"value2/\",value3,key4=/\"value4/\"")]
    [InlineData("key1=\"value1\",value2, value3, value4", "key1=/\"value1/\",value2, value3, value4")]
    public void VerifyValidEncoding(string input, string expectedOutput)
    {
        VBDefineConstantsEncoding _encoding = new();
        Assert.Equal(expected: expectedOutput, actual: _encoding.Format(_encoding.Parse(input)));
    }

    [Theory]
    [InlineData("key1=/\"value1/\"", new[] { "key1", "\"value1\"" })]
    [InlineData("key1=/\"value1/\",key2=/\"value2/\"", new[] { "key1", "\"value1\"", "key2", "\"value2\"" })]
    [InlineData("a=/\"b/\",c=/\"easy/\",as=123", new[] { "a", "\"b\"", "c", "\"easy\"", "as", "123" })]
    [InlineData("key1=/\"value1/\",key2=/\"value2/\",key3=/\"value3/\"", new[] { "key1", "\"value1\"", "key2", "\"value2\"", "key3", "\"value3\"" })]
    [InlineData("something=/\"equals/=this/\"", new[] { "something", "\"equals=this\"" })]
    [InlineData("oh=/\"hey/=there/\",hi=/\"didnt/=see/\",you=/\"there/\"", new[] { "oh", "\"hey=there\"", "hi", "\"didnt=see\"", "you", "\"there\"" })]
    [InlineData("path=/\"//path//to//somewhere/\"", new[] { "path", "\"/path/to/somewhere\"" })]
    [InlineData("file=/\"//path//is//here/\"", new[] { "file", "\"/path/is/here\"" })]
    [InlineData("files=/\"path/=//path//to//somewhere/\"", new[] { "files", "\"path=/path/to/somewhere\"" })]
    [InlineData("a=1,a=2", new[] { "a", "1", "a", "2" })]
    [InlineData("key1  =/\"value1  /\"", new[] { "key1  ", "\"value1  \"" })]
    [InlineData("  key1=/\"  value1/\"", new[] { "  key1", "\"  value1\"" })]
    [InlineData("key1=/\"a b c d/\"", new[] { "key1", "\"a b c d\"" })]
    [InlineData("key1=/\"a/=b/=c/=d/\"", new[] { "key1", "\"a=b=c=d\"" })]
    [InlineData("key1=/\"a/=b/\"/\"c/=d/\"", new[] { "key1", "\"a=b\"\"c=d\"" })]
    [InlineData("key1=/\"/\"ab/\"/\"cd/\"", new[] { "key1", "\"\"ab\"\"cd\"" })]
    [InlineData("key1=/\"/\"/\"ab/\"/\"cd/\"/\"ef/\"", new[] { "key1", "\"\"\"ab\"\"cd\"\"ef\"" })]
    [InlineData("key1=/\"/\"/\"ab/\"/\"cd/\"", new[] { "key1", "\"\"\"ab\"\"cd\"" })]
    [InlineData("a b c d=/\"value1/\"", new[] { "a b c d", "\"value1\"" })]
    [InlineData("a=/\"b/=c/=d/=value1/\"", new[] { "a", "\"b=c=d=value1\"" })]
    [InlineData("a=/\"b/\"/\"c/=d/=value1/\"", new[] { "a", "\"b\"\"c=d=value1\"" })]
    [InlineData("/\"ab/\"/\"cd=/\"value1/\"", new[] { "\"ab\"\"cd", "\"value1\"" })]
    [InlineData("/\"/\"ab/\"/\"cd/\"/\"ef=/\"value1/\"", new[] { "\"\"ab\"\"cd\"\"ef", "\"value1\"" })]
    [InlineData("/\"/\"ab/\"/\"cd=/\"value1/\"", new[] { "\"\"ab\"\"cd", "\"value1\"" })]
    [InlineData("a//bc=/\"value1/\"", new[] { "a/bc", "\"value1\"" })]
    [InlineData("", new string[0])]

    [InlineData("a=1,b=2", new[] { "a", "1", "b", "2" })]
    [InlineData("a=1123,b=456", new[] { "a", "1123", "b", "456" })]
    [InlineData("a=1,b=/\"apple/\"", new[] { "a", "1", "b", "\"apple\"" })]
    [InlineData("a=1,b=/\"apple/\",c=456", new[] { "a", "1", "b", "\"apple\"", "c", "456" })]
    [InlineData("a=1,b=/\"apple/\",c=456,d=True", new[] { "a", "1", "b", "\"apple\"", "c", "456", "d", "True" })]

    [InlineData("a=True", new[] { "a", "True" })]
    [InlineData("a=False", new[] { "a", "False" })]
    [InlineData("a=True,b=False", new[] { "a", "True", "b", "False" })]
    [InlineData("a=True,b=False,c=123", new[] { "a", "True", "b", "False", "c", "123" })]
    public void ValidateValuesParsedFormatted(string encodedPairs, string[] pairs)
    {
        VBDefineConstantsEncoding _encoding = new();

        Assert.Equal(expected: pairs, actual: _encoding.Parse(encodedPairs).SelectMany(pair => new[] { pair.Name, pair.Value }));

        Assert.Equal(encodedPairs, _encoding.Format(ToNameValues(pairs)));

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

    [Theory]
    [InlineData("=key1=")]
    [InlineData("=key1")]
    [InlineData(",key1")]
    [InlineData(",,,key1")]
    [InlineData("key1,")]
    [InlineData("key1=value1,")]
    [InlineData("key1,,,")]
    [InlineData("=")]
    [InlineData("==")]
    [InlineData("===")]
    [InlineData("key1,=abcd")]
    [InlineData("key1,=,abcd")]
    [InlineData("=\"\"")]
    [InlineData(",")]
    [InlineData(",,,")]
    public void DetectInvalidInput(string input)
    {
        VBDefineConstantsEncoding _encoding = new();
        FormatException exception = Assert.Throws<FormatException>(() => (_encoding.Format(_encoding.Parse(input))));
        Assert.Equal("Expected valid name value pair for defining custom constants.", exception.Message);
    }
}
