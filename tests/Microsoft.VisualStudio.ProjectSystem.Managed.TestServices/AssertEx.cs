// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Text;
using Microsoft;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit.Sdk;

namespace Xunit;

internal static class AssertEx
{
    public static void CollectionLength<T>(IEnumerable<T> collection, int expectedCount)
    {
        var actualCount = collection.Count();
        if (expectedCount != actualCount)
        {
            throw CollectionException.ForMismatchedItemCount(expectedCount, actualCount, "Collection lengths not equal.");
        }
    }

    public static void CollectionLength(IEnumerable collection, int expectedCount)
    {
        CollectionLength(collection.Cast<object>(), expectedCount);
    }

    public static void SequenceEqual<T>(
        IEnumerable<T> expected,
        IEnumerable<T> actual,
        IEqualityComparer<T>? comparer = null,
        string? itemSeparator = null,
        Func<T, string>? itemInspector = null)
    {
        if (expected is null)
        {
            Assert.Null(actual);
        }
        else
        {
            Assert.NotNull(actual);
        }

        Assumes.NotNull(expected);
        Assumes.NotNull(actual);

        if (!expected.SequenceEqual(actual, comparer))
        {
            Assert.Fail(GetAssertMessage(expected, actual, itemInspector: itemInspector, itemSeparator: itemSeparator));
        }
    }

    public static string GetAssertMessage<T>(
        IEnumerable<T> expected,
        IEnumerable<T> actual,
        IEqualityComparer<T>? comparer = null,
        string? prefix = null,
        Func<T, string>? itemInspector = null,
        string? itemSeparator = null,
        string? expectedValueSourcePath = null,
        int expectedValueSourceLine = 0)
    {
        if (itemInspector is null)
        {
            if (typeof(T) == typeof(byte))
            {
                itemInspector = b => $"0x{b:X2}";
            }
            else
            {
                itemInspector = new Func<T, string>(obj => (obj is null) ? "<null>" : obj.ToString());
            }
        }

        if (itemSeparator is null)
        {
            if (typeof(T) == typeof(byte))
            {
                itemSeparator = ", ";
            }
            else
            {
                itemSeparator = "," + Environment.NewLine;
            }
        }

        var expectedString = string.Join(itemSeparator, expected.Take(10).Select(itemInspector));
        var actualString = string.Join(itemSeparator, actual.Select(itemInspector));
        var diffString = DiffUtil.DiffReport(expected, actual, itemSeparator, comparer, itemInspector);

        if (DifferOnlyInWhitespace(expectedString, actualString))
        {
            expectedString = VisualizeWhitespace(expectedString);
            actualString = VisualizeWhitespace(actualString);
            diffString = VisualizeWhitespace(diffString);
        }

        var message = new StringBuilder();

        if (!string.IsNullOrEmpty(prefix))
        {
            message.AppendLine(prefix);
            message.AppendLine();
        }

        message.AppendLine("Expected:");
        message.AppendLine(expectedString);
        if (expected.Count() > 10)
        {
            message.AppendLine("... truncated ...");
        }

        message.AppendLine("Actual:");
        message.AppendLine(actualString);
        message.AppendLine("Differences:");
        message.AppendLine(diffString);

        return message.ToString();
    }

    private static bool DifferOnlyInWhitespace(IEnumerable<char> expected, IEnumerable<char> actual)
        => expected.Where(c => !char.IsWhiteSpace(c)).SequenceEqual(actual.Where(c => !char.IsWhiteSpace(c)));

    private static string VisualizeWhitespace(string str)
    {
        var result = new StringBuilder(str.Length);

        var i = 0;
        while (i < str.Length)
        {
            var c = str[i++];
            if (c == '\r' && i < str.Length && str[i] == '\n')
            {
                result.Append("␍␊\r\n");
                i++;
            }
            else
            {
                result.Append(c switch
                {
                    ' ' => "·",
                    '\t' => "→",
                    '\r' => "␍\r",
                    '\n' => "␊\n",
                    _ => c,
                });
            }
        }

        return result.ToString();
    }
}
