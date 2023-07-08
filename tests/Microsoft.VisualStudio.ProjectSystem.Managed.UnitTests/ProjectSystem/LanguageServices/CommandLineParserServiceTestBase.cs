// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
                service.Parse(null!, @"C:\Project");
            });
        }

        [Fact]
        public void Parse_NullAsBaseDirectory_ThrowsArgumentNull()
        {
            var service = CreateInstance();

            var arguments = Enumerable.Empty<string>();

            Assert.Throws<ArgumentNullException>("baseDirectory", () =>
            {
                service.Parse(arguments, null!);
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
