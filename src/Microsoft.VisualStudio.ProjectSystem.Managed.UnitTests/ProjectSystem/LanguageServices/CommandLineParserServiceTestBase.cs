// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    public abstract class CommandLineParserServiceTestBase
    {
        [Fact]
        public void Parse_NullAsArguments_ThrowsArgumentNull()
        {
            var instance = CreateInstance();

            Assert.Throws<ArgumentNullException>("arguments", () =>
            {
                instance.Parse((IEnumerable<string>)null, @"C:\Project");
            });
        }

        [Fact]
        public void Parse_NullAsBaseDirectory_ThrowsArgumentNull()
        {
            var instance = CreateInstance();

            var arguments = Enumerable.Empty<string>();

            Assert.Throws<ArgumentNullException>("baseDirectory", () =>
            {
                instance.Parse(arguments, (string)null);
            });
        }

        [Fact]
        public void Parse_EmptyAsBaseDirectory_ThrowsArgument()
        {
            var instance = CreateInstance();

            var arguments = Enumerable.Empty<string>();

            Assert.Throws<ArgumentException>("baseDirectory", () =>
            {
                instance.Parse(arguments, "");
            });
        }

        internal abstract ICommandLineParserService CreateInstance();
    }
}
