// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    public abstract class CommandLineParserServiceTestBase
    {
        [Fact]
        public void Parse_NullAsArguments_ThrowsArgumentNull()
        {
            var service = CreateInstance();

            Assert.Throws<ArgumentNullException>("arguments", () =>
            {
                service.Parse(null, @"C:\Project");
            });
        }

        [Fact]
        public void Parse_NullAsBaseDirectory_ThrowsArgumentNull()
        {
            var service = CreateInstance();

            var arguments = Enumerable.Empty<string>();

            Assert.Throws<ArgumentNullException>("baseDirectory", () =>
            {
                service.Parse(arguments, null);
            });
        }

        [Fact]
        public void Parse_EmptyAsBaseDirectory_ThrowsArgument()
        {
            var service = CreateInstance();

            var arguments = Enumerable.Empty<string>();

            Assert.Throws<ArgumentException>("baseDirectory", () =>
            {
                service.Parse(arguments, "");
            });
        }

        internal abstract ICommandLineParserService CreateInstance();
    }
}
