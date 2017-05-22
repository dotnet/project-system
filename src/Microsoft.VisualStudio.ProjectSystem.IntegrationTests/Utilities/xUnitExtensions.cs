using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.IntegrationTests
{
    public static class xUnitExtensions
    {
        public static void ShouldEqualWithDiff(this string[] actualValue, string[] expectedValue)
        {
            var message = $"string array length do not match.  actual length '{actualValue.Length}' expected length '{expectedValue.Length}'";
            Assert.True(actualValue.Length == expectedValue.Length, message);

            for (int i = 0; i < actualValue.Length; i++)
            {
                actualValue[i].ShouldEqualWithDiff(expectedValue[i]);
            }
        }

            public static void ShouldEqualWithDiff(this string actualValue, string expectedValue)
        {
            if (actualValue == null || expectedValue == null)
            {
                Assert.Equal(expectedValue, actualValue);
                return;
            }

            var areEqual = actualValue.Equals(expectedValue, StringComparison.Ordinal);
            if (areEqual)
            {
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("Excpected and Actual strings did not match");
            builder.AppendLine("  Idx Expected  Actual");
            builder.AppendLine("-------------------------");
            int maxLen = Math.Max(actualValue.Length, expectedValue.Length);
            int minLen = Math.Min(actualValue.Length, expectedValue.Length);
            for (int i = 0; i < maxLen; i++)
            {
                if (i >= minLen || actualValue[i] != expectedValue[i])
                {
                    var mark = (i < minLen && actualValue[i] == expectedValue[i] ? " " : "*");
                    var expectedCharacterIntegerValue = (i < expectedValue.Length ? ((int)expectedValue[i]).ToString() : "");
                    var expectedCharacterStringValue = (i < expectedValue.Length ? expectedValue[i].ReplaceWhiteSpaceCharacters() : "");
                    var actualCharacterIntegerValue = (i < actualValue.Length ? ((int)actualValue[i]).ToString() : "");
                    var actualCharacterStringValue = (i < actualValue.Length ? actualValue[i].ReplaceWhiteSpaceCharacters() : "");
                    builder.AppendLine($"{mark} {i,-3} {expectedCharacterIntegerValue,-4} {expectedCharacterStringValue,-3}  {actualCharacterIntegerValue,-4} {actualCharacterStringValue,-3}");
                }
            }
            builder.AppendLine();

            Assert.True(areEqual, builder.ToString());
        }

        private static string ReplaceWhiteSpaceCharacters(this char c)
        {
            if (Char.IsControl(c) || Char.IsWhiteSpace(c))
            {
                switch (c)
                {
                    case '\r':
                        return @"\r";
                    case '\n':
                        return @"\n";
                    case '\t':
                        return @"\t";
                    case '\a':
                        return @"\a";
                    case '\v':
                        return @"\v";
                    case '\f':
                        return @"\f";
                    default:
                        return String.Format("\\u{0:X};", (int)c);
                }
            }
            return c.ToString(CultureInfo.InvariantCulture);
        }
    }
}
