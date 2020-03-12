// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal partial class Tokenizer
    {
        // Represents a self-contained unit within a tokenized string
        private struct Token
        {
            private readonly char _value;
            private readonly bool _isDelimiter;

            private Token(char value, bool isDelimiter)
            {
                Assumes.True(value != '\0');

                _value = value;
                _isDelimiter = isDelimiter;
            }

            public bool IsDelimiter
            {
                get { return _isDelimiter; }
            }

            public bool IsLiteral
            {
                get { return !_isDelimiter; }
            }

            public char Value
            {
                get { return _value; }
            }

            public TokenType TokenType
            {
                get
                {
                    if (IsDelimiter)
                        return (TokenType)_value;

                    return TokenType.Literal;
                }
            }

            public static Token Literal(char value)
            {
                return new Token(value, isDelimiter: false);
            }

            public static Token Delimiter(char value)
            {
                return new Token(value, isDelimiter: true);
            }
        }
    }
}
