// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.
using Microsoft.VisualStudio.ProjectSystem.Properties;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class DefineConstantsPropertyPageProviderTests
    {

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

        //valid constants defined
        [Theory]                 // key value pairs
        [InlineData(new object[] { new[] { "key1=value1" } })]
        [InlineData(new object[] { new[] { "key1=value1", "key2=value2" } })]
        [InlineData(new object[] { new[] { "a=b", "c=easy", "as=123" } })]
        [InlineData(new object[] { new[] { "key1=value1", "key2=value2", "key3=value3" } })]
        [InlineData(new object[] { new[] { "something=equals=this" } })]
        [InlineData(new object[] { new[] { "oh=hey=there", "hi=didnt=see", "you=there" } })]
        [InlineData(new object[] { new[] { "path=/path/to/somewhere/" } })]
        [InlineData(new object[] { new[] { "file=/path/is/here/," } })]
        [InlineData(new object[] { new[] { "files=path=/path/to/somewhere/," } })]
        public async Task ConstantsDefined(string[] kvp)
        {
            string input = formatInput(kvp);
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

        //invalid constants defined
        [Theory]
        [InlineData(new object[] { new[] { "oop" } })]
        [InlineData(new object[] { new[] { "oop=" } })]
        [InlineData(new object[] { new[] { "=oop" } })]
        [InlineData(new object[] { new[] { "oop=see", "daisy=oh", "well=" } })]
        [InlineData(new object[] { new[] { "=oop", "daisy=" } })]
        public async Task InvalidConstantsDefined(string[] kvp)
        {
            string input = formatInput(kvp);
            var provider = new DefineConstantsValueProvider();
            var defaultProperties = Mock.Of<IProjectProperties>();

            var setResultValue = await provider.OnSetPropertyValueAsync(DefineConstantsValueProvider.DefineConstantsPropertyName, input, defaultProperties);

            string expectedResult = parseValidInput(kvp);
            Assert.Equal(expected: EncodeOutputFormat(expectedResult, true), actual: setResultValue);

            var getResultValue = await provider.OnGetUnevaluatedPropertyValueAsync(DefineConstantsValueProvider.DefineConstantsPropertyName, input, defaultProperties);
            Assert.Equal(expected: EncodeOutputFormat(expectedResult, false), actual: getResultValue);
        }

        //repeated keys defined
        [Theory]
        [InlineData(new object[] { new[] { "oh=hey", "oh=hello", "oh=hi" } })]
        [InlineData(new object[] { new[] { "oh=hey", "there=mmm", "oh=hi" } })]
        [InlineData(new object[] { new[] { "ok=hi", "oh=hi", "oh=3" } })]
        public async Task RepeatKeysDefined(string[] kvp)
        {
            string input = formatInput(kvp);
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


        private string formatInput(string[] input)
        {
            return string.Join(",", input);
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
            input += ",";
            List<KeyValuePair<string, string>> uniqueKeyValuePairs = new List<KeyValuePair<string, string>>();
            var regex = new Regex(@"(\S+?\s*)\=(\S+?\s*),");
            foreach (System.Text.RegularExpressions.Match match in regex.Matches(input))
            {
                string key = match.Groups[1].Value;
                string value = match.Groups[2].Value;
                var index = uniqueKeyValuePairs.FindIndex(kvp => kvp.Key == key);
                if (index >= 0)
                {
                    uniqueKeyValuePairs[index] = new KeyValuePair<string, string>(key, value);
                }
                else
                {
                    uniqueKeyValuePairs.Add(new KeyValuePair<string, string>(key, value));
                }
            }

            var formattedInput = "";
            foreach (var kvp in uniqueKeyValuePairs)
            {
                formattedInput += kvp.Key + "=" + kvp.Value + ",";
            }

            if (formattedInput.Length > 0)
            {
                return formattedInput.Remove(formattedInput.Length - 1);           //remove the trailing comma
            }
            else
            {
                return formattedInput;
            }
        }

    }
}
