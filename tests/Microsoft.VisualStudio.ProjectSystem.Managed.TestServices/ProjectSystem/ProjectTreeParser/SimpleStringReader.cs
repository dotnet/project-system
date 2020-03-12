// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    // Simple, cheap, forward-only string reader
    internal class SimpleStringReader
    {
        private readonly string _input;
        private int _position;

        public SimpleStringReader(string input)
            : this(input, 0)
        {
        }

        private SimpleStringReader(string input, int startIndex)
        {
            Assumes.NotNull(input);
            Assumes.True(input.Length > 0);
            Assumes.True(startIndex >= 0);
            Assumes.True(startIndex <= input.Length);

            _input = input;
            _position = startIndex;
        }

        public bool CanRead
        {
            get
            {
                if (_position < _input.Length)
                {
                    // Treat null as end of string
                    return PeekChar() != '\0';
                }

                return false;
            }
        }

        public char Peek()
        {
            Assumes.True(CanRead);

            return PeekChar();
        }

        public char Read()
        {
            char c = Peek();

            _position++;

            return c;
        }

        private char PeekChar()
        {
            return _input[_position];
        }

        public SimpleStringReader Clone()
        {
            return new SimpleStringReader(_input, _position);
        }
    }
}
