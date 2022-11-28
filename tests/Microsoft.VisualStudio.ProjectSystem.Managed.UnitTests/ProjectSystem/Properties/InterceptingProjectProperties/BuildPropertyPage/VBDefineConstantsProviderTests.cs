// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.InterceptingProjectProperties.BuildPropertyPage
{
    public class VBDefineConstantsProviderTests
    {
        [Theory]
        [InlineData("key1=value1", "key1=\"value1\"")]
        [InlineData("key1=value1,key2=value2", "key1=\"value1\",key2=\"value2\"")]
        [InlineData("a=b,c=easy,as=123", "a=\"b\",c=\"easy\",as=\"123\"")]
        [InlineData("key1=value1,key2=value2,key3=value3", "key1=\"value1\",key2=\"value2\",key3=\"value3\"")]
        [InlineData("something=equals=this", "something=\"equals/=this\"")]
        [InlineData("oh=hey=there,hi=didnt=see,you=there", "oh=\"hey/=there\",hi=\"didnt/=see\",you=\"there\"")]
        [InlineData("path=/path/to/somewhere/", "path=\"//path//to//somewhere//\"")]
        [InlineData("file=/path/is/here/", "file=\"//path//is//here//\"")]
        [InlineData("files=path=/path/to/somewhere/", "files=\"path/=//path//to//somewhere//\"")]
        [InlineData("a=1,a=2", "a=\"1\",a=\"2\"")]
        [InlineData("key1  =value1  ", "key1  =\"value1  \"")]
        [InlineData("  key1=  value1", "  key1=\"  value1\"")]
        [InlineData(" key1 = value1 ", " key1 =\" value1 \"")]
        [InlineData(" = ", " =\" \"")]
        [InlineData("key1=a b c d", "key1=\"a b c d\"")]
        public void DefineConstantsEncodingFormatTest(string input, string expectedOutput)
        {
            NameQuotedValuePairListEncoding _encoding = new();
            Assert.Equal(expected: expectedOutput, actual: _encoding.Format(_encoding.Parse(input)));
        }

        [Theory]
        [InlineData("key1=\"value1\"", "key1=value1")]
        [InlineData("key1=\"value1\",key2=\"value2\"", "key1=value1,key2=value2")]
        [InlineData("a=\"b\",c=\"easy\",as=\"123\"", "a=b,c=easy,as=123")]
        [InlineData("key1=\"value1\",key2=\"value2\",key3=\"value3\"", "key1=value1,key2=value2,key3=value3")]
        [InlineData("something=\"equals/=this\"", "something=equals=this")]
        [InlineData("oh=\"hey/=there\",hi=\"didnt/=see\",you=\"there\"", "oh=hey=there,hi=didnt=see,you=there")]
        [InlineData("path=\"//path//to//somewhere//\"", "path=/path/to/somewhere/")]
        [InlineData("file=\"//path//is//here//\"", "file=/path/is/here/")]
        [InlineData("files=\"path/=//path//to//somewhere//\"", "files=path=/path/to/somewhere/")]
        [InlineData("a=\"1\",a=\"2\"", "a=1,a=2")]
        [InlineData("key1  =\"value1  \"", "key1  =value1  ")]
        [InlineData("  key1=\"  value1\"", "  key1=  value1")]
        [InlineData(" key1 =\" value1 \"", " key1 = value1 ")]
        [InlineData(" =\" \"", " = ")]
        [InlineData("key1=\"a b c d\"", "key1=a b c d")]
        public void DefineConstantsEncodingDisplayFormatTest(string input, string expectedOutput)
        {
            NameQuotedValuePairListEncoding _encoding = new();
            Assert.Equal(expected: expectedOutput, actual: _encoding.DisplayFormat(_encoding.Decode(input)));
        }

        [Theory]
        [InlineData("key1=value1")]
        [InlineData("key1=value1,key2=value2")]
        [InlineData("a=b,c=easy,as=123")]
        [InlineData("key1=value1,key2=value2,key3=value3")]
        [InlineData("something=equals=this")]
        [InlineData("oh=hey=there,hi=didnt=see,you=there")]
        [InlineData("path=/path/to/somewhere/")]
        [InlineData("file=/path/is/here/")]
        [InlineData("files=path=/path/to/somewhere/")]
        [InlineData("a=1,a=2")]
        [InlineData("key1  =value1  ")]
        [InlineData("  key1=  value1")]
        [InlineData(" key1 = value1 ")]
        [InlineData(" = ")]
        [InlineData("key1=a b c d")]
        public void DefineConstantsEncoding(string input)
        {
            NameQuotedValuePairListEncoding _encoding = new();
            Assert.Equal(expected: input, actual: _encoding.DisplayFormat(_encoding.Decode(_encoding.Format(_encoding.Parse(input)))));
        }
    }
}
