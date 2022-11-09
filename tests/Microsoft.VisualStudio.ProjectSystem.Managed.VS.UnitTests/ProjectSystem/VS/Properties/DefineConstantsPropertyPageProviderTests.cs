// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using System.Text.RegularExpressions;
using System.Text;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class DefineConstantsPropertyPageProviderTests
    {
        /* Define Constants Encoding */
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
        public void DefineConstantsEncodingFormatTest(string input, string expectedOutput)
        {
            Dictionary<string, string> constantsDictionary = StringListEncoding.ParseIntoDictionary(input);
            var formatValue = KeyQuotedValuePairListEncoding.Instance.Format(StringListEncoding.EnumerateDictionary(constantsDictionary));
            Assert.Equal(expected: expectedOutput, actual: formatValue);
        }

        [Theory]
        [InlineData("key1=\"value1\"", "key1=value1")]
        [InlineData("key1=\"value1\",key2=\"value2\"", "key1=value1,key2=value2")]
        [InlineData("a=\"b\",c=\"easy\",as=\"123\"", "a=b,c=easy,as=123")]
        [InlineData("key1=\"value1\",key2=\"value2\",key3=\"value3\"", "key1=value1,key2=value2,key3=value3")]
        [InlineData("something=\"equals/=this\"", "something=equals/=this")]
        [InlineData("oh=\"hey/=there\",hi=\"didnt/=see\",you=\"there\"", "oh=hey/=there,hi=didnt/=see,you=there")]
        [InlineData("path=\"//path//to//somewhere//\"", "path=//path//to//somewhere//")]
        [InlineData("file=\"//path//is//here//\"", "file=//path//is//here//")]
        [InlineData("files=\"path/=//path//to//somewhere//\"", "files=path/=//path//to//somewhere//")]
        public void DefineConstantsEncodingDisplayFormatTest(string input, string expectedOutput)
        {
            Dictionary<string, string> constantsDictionary = StringListEncoding.ParseIntoDictionary(input);
            var formatValue = KeyValuePairListEncoding.Instance.Format(StringListEncoding.EnumerateDictionary(constantsDictionary));
            Assert.Equal(expected: expectedOutput, actual: formatValue);
        }


        /* Encoding Tests */
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
        public void KeyValuePairEncodingParseFormatTest(string input, string expectedOutput)
        {
            var encodedValue = KeyQuotedValuePairListEncoding.Instance.Parse(input);
            var formatValue = KeyQuotedValuePairListEncoding.Instance.Format(encodedValue);
            Assert.Equal(expected: expectedOutput, actual: formatValue);
        }

        [Theory]
        [InlineData("key1=\"value1\"", "key1=value1,")]
        [InlineData("key1=\"value1\",key2=\"value2\"", "key1=value1,key2=value2,")]
        [InlineData("a=\"b\",c=\"easy\",as=\"123\"", "a=b,c=easy,as=123,")]
        [InlineData("key1=\"value1\",key2=\"value2\",key3=\"value3\"", "key1=value1,key2=value2,key3=value3,")]
        [InlineData("something=\"equals/=this\"", "something=equals/=this,")]
        [InlineData("oh=\"hey/=there\",hi=\"didnt/=see\",you=\"there\"", "oh=hey/=there,hi=didnt/=see,you=there,")]
        [InlineData("path=\"//path//to//somewhere//\"", "path=//path//to//somewhere//,")]
        [InlineData("file=\"//path//is//here//\"", "file=//path//is//here//,")]
        [InlineData("files=\"path/=//path//to//somewhere//\"", "files=path/=//path//to//somewhere//,")]
        public void KeyValuePairDecodeTest(string input, string expectedOuput)
        {
            var decodedValue = KeyQuotedValuePairListEncoding.Instance.Decode(input);
            Assert.Equal(expected: expectedOuput, actual: decodedValue);
        }


        /* OnSetPropertyValue Tests */

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
        public async Task TestOnSetPropertyValueAsync(string input, string expectedOutput)
        {
            var provider = new DefineConstantsValueProvider();
            var defaultProperties = Mock.Of<IProjectProperties>();

            var setResultValue = await provider.OnSetPropertyValueAsync(DefineConstantsValueProvider.DefineConstantsPropertyName, input, defaultProperties);

            Assert.NotNull(setResultValue);
            Assert.True(ValidEncoding(setResultValue));

            Assert.Equal(expected: expectedOutput, actual: setResultValue);
        }

        /* End to end define constants tests */

        [Fact]
        public async Task NoConstantsDefined()
        {
            var provider = new DefineConstantsValueProvider();
            var defaultProperties = IProjectPropertiesFactory.CreateWithPropertyAndValue(DefineConstantsValueProvider.DefineConstantsPropertyName, string.Empty);

            var unevaluatedResult = await provider.OnGetUnevaluatedPropertyValueAsync(DefineConstantsValueProvider.DefineConstantsPropertyName, string.Empty, defaultProperties);

            Assert.Equal(string.Empty, actual: unevaluatedResult);

            var evaluatedResult = await provider.OnGetEvaluatedPropertyValueAsync(DefineConstantsValueProvider.DefineConstantsPropertyName, string.Empty, defaultProperties);

            Assert.Equal(string.Empty, actual: evaluatedResult);
        }

        [Theory]              
        [InlineData( "key1=value1")]
        [InlineData( "key1=value1,key2=value2")]
        [InlineData("a=b,c=easy,as=123")]
        [InlineData("key1=value1,key2=value2,key3=value3")]
        [InlineData( "something=equals=this")]
        [InlineData( "oh=hey=there,hi=didnt=see,you=there")]
        [InlineData("path=/path/to/somewhere/")]
        [InlineData( "file=/path/is/here/,")]
        [InlineData( "files=path=/path/to/somewhere/,")]
        [InlineData("files=are\\here,")]
        [InlineData("here=is\\,comma,equals=here\\=now")]
        public async Task ValidConstantsDefined(string input)
        {
            var provider = new DefineConstantsValueProvider();
            var defaultProperties = Mock.Of<IProjectProperties>();

            var setResultValue = await provider.OnSetPropertyValueAsync(DefineConstantsValueProvider.DefineConstantsPropertyName, input, defaultProperties);

            Assert.NotNull(setResultValue);
            Assert.True(ValidEncoding(setResultValue));

            Assert.Equal(expected: EncodeOutputFormat(input, true), actual: setResultValue);

            var getResultValue = await provider.OnGetUnevaluatedPropertyValueAsync(DefineConstantsValueProvider.DefineConstantsPropertyName, input, defaultProperties);
            Assert.NotNull(getResultValue);
            Assert.Equal(expected: EncodeOutputFormat(input, false), actual: getResultValue);

        }

        [Theory]
        [InlineData( "oop")]
        [InlineData( "oop=")]
        [InlineData("=oop")]
        [InlineData( "oop=see,daisy=oh,well=")]
        [InlineData("=oop,daisy=")]
        public async Task InvalidConstantsDefined(string input)
        {
            var provider = new DefineConstantsValueProvider();
            var defaultProperties = Mock.Of<IProjectProperties>();

            var setResultValue = await provider.OnSetPropertyValueAsync(DefineConstantsValueProvider.DefineConstantsPropertyName, input, defaultProperties);

            string expectedResult = parseValidInput(input.Split(','));
            Assert.Equal(expected: EncodeOutputFormat(expectedResult, true), actual: setResultValue);

            var getResultValue = await provider.OnGetUnevaluatedPropertyValueAsync(DefineConstantsValueProvider.DefineConstantsPropertyName, input, defaultProperties);
            Assert.Equal(expected: EncodeOutputFormat(expectedResult, false), actual: getResultValue);
        }

        [Theory]
        [InlineData("oh=hey,oh=hello,oh=hi")]
        [InlineData("oh=hey,there=mmm,oh=hi")]
        [InlineData("ok=hi,oh=hi,oh=3")]
        public async Task RepeatKeysDefined(string input)
        {
            var provider = new DefineConstantsValueProvider();
            var defaultProperties = Mock.Of<IProjectProperties>();

            var setResultValue = await provider.OnSetPropertyValueAsync(DefineConstantsValueProvider.DefineConstantsPropertyName, input, defaultProperties);

            Assert.NotNull(setResultValue);
            Assert.True(ValidEncoding(setResultValue));

            var intendedInput = getDefinedRepeat(input);

            Assert.Equal(expected: EncodeOutputFormat(intendedInput, true), actual: setResultValue);

            var getResultValue = await provider.OnGetUnevaluatedPropertyValueAsync(DefineConstantsValueProvider.DefineConstantsPropertyName, input, defaultProperties);
            Assert.NotNull(getResultValue);
            Assert.Equal(expected: EncodeOutputFormat(intendedInput, false), actual: getResultValue);
        }

        private string parseValidInput(string[] input)
        {
            string valid = "";
            for (int i = 0; i < input.Length; i++)
            {
                if (ValidInput(input[i]))
                {
                    valid += input[i] + ",";
                }
            }
            if (valid.Length > 0)
            {
                return valid.Remove(valid.Length - 1);                          //remove the trailing comma
            }
            else
            {
                return valid;
            }
        }

        //determines if input is in valid format
        private bool ValidInput(string input)
        {
            var regex = new Regex(@"(\S+?\s*)\=(\S+?\s*)");
            return regex.IsMatch(input);
        }

        //determines if encoding is in valid format
        private bool ValidEncoding(string encoding)
        {
            var regex = new Regex(@"(\S+?\s*)\=[\x22](\S+?\s*)[\x22],?");
            return regex.IsMatch(encoding);
        }

        // outputFormat true for the format key="value", false for key=value
        private string EncodeOutputFormat(string input, bool outputFormat)
        {
            input += ",";
            var regex = new Regex(@"(\S+?\s*)\=(\S+?\s*),");
            string encoded = "";
            foreach (System.Text.RegularExpressions.Match match in regex.Matches(input))
            {
                string key = match.Groups[1].Value.Replace("/", "//").Replace("=", "/=").Replace(",", "/,");
                string value = match.Groups[2].Value.Replace("/", "//").Replace("=", "/=").Replace(",", "/,");
                encoded += outputFormat ? key + "=\"" + value + "\"," : key + "=" + value + ",";
            }
            return encoded.TrimEnd(',');
        }

        //parses repeated keys inputted key value pairs
        private string getDefinedRepeat(string input)
        {
            Dictionary<string, string> uniqueKeyValuePairs = new Dictionary<string, string>();
            var regex = new Regex(@"(\S+?\s*)\=(\S+?\s*),");
            foreach (System.Text.RegularExpressions.Match match in regex.Matches(input+","))
            {
                string key = match.Groups[1].Value;
                uniqueKeyValuePairs[key] = match.Groups[2].Value;
            }

            StringBuilder formattedInput = new();
            foreach ((string key, string value) in uniqueKeyValuePairs)
            {
                formattedInput.Append(key).Append('=').Append(value).Append(',');
            }

            if (formattedInput.Length > 0)
            {
                return formattedInput.ToString(0, formattedInput.Length - 1);           //remove the trailing comma
            }
            else
            {
                return string.Empty;
            }
        }

    }
}
