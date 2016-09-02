// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    [ProjectSystemTrait]
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("test",     "\"test\"")]
        [InlineData("\"test\"", "\"test\"")]
        [InlineData("\"test",   "\"test\"")]
        [InlineData("test\"",   "\"test\"")]
        public void StringExtensions_QuoteStringTests(string input, string expected)
        {
            Assert.Equal(expected, input.QuoteString());
        }
    }
}
