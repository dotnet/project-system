// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Buffers.PooledObjects;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug;

internal static class CommandEscaping
{
    /// <summary>
    /// Escapes command-line arguments to ensure they're passed to the executable rather than interpreted by cmd.exe.
    /// </summary>
    internal static string EscapeString(string unescaped)
    {
        if (Strings.IsNullOrWhiteSpace(unescaped))
        {
            return unescaped;
        }

        static bool ShouldEscape(char c) => c is '^' or '<' or '>' or '&';

        StringState currentState = StringState.NormalCharacter;
        var finalBuilder = PooledStringBuilder.GetInstance();
        foreach (char currentChar in unescaped)
        {
            switch (currentState)
            {
                case StringState.NormalCharacter:
                    // If we're currently not in a quoted string, then we need to escape anything in toEscape.
                    // The valid transitions are to EscapedCharacter (for a '\', such as '\"'), and QuotedString.
                    if (currentChar == '\\')
                    {
                        currentState = StringState.EscapedCharacter;
                    }
                    else if (currentChar == '"')
                    {
                        currentState = StringState.QuotedString;
                    }
                    else if (ShouldEscape(currentChar))
                    {
                        finalBuilder.Append('^');
                    }

                    break;
                case StringState.EscapedCharacter:
                    // If a '\' was the previous character, then we blindly append to the string, escaping if necessary,
                    // and move back to NormalCharacter. This handles '\"'
                    if (ShouldEscape(currentChar))
                    {
                        finalBuilder.Append('^');
                    }

                    currentState = StringState.NormalCharacter;
                    break;
                case StringState.QuotedString:
                    // If we're in a string, we don't escape any characters. If the current character is a '\',
                    // then we move to QuotedStringEscapedCharacter. This handles '\"'. If the current character
                    // is a '"', then we're out of the string. Otherwise, we stay in the string.
                    if (currentChar == '\\')
                    {
                        currentState = StringState.QuotedStringEscapedCharacter;
                    }
                    else if (currentChar == '"')
                    {
                        currentState = StringState.NormalCharacter;
                    }

                    break;
                case StringState.QuotedStringEscapedCharacter:
                    // If we have one slash, then we blindly append to the string, no escaping, and move back to
                    // QuotedString. This handles escaped '"' inside strings.
                    currentState = StringState.QuotedString;
                    break;
                default:
                    // We can't get here.
                    throw new InvalidOperationException();
            }

            finalBuilder.Append(currentChar);
        }

        return finalBuilder.ToStringAndFree();
    }

    private enum StringState
    {
        NormalCharacter, EscapedCharacter, QuotedString, QuotedStringEscapedCharacter
    }
}
