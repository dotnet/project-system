// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.VisualBasic
{
    public class VisualBasicCommandLineParserServiceTests : CommandLineParserServiceTestBase
    {
        // This isn't supposed to be a exhaustive set of tests as we assume that Roslyn has tested their 
        // command-line parsing code, just enough to make sure we're passing the data through correctly.

        [Theory]
        [InlineData("/r:Foo.dll",                                                                           "Foo.dll")]
        [InlineData("/r:Foo.dll|/r:Bar.dll",                                                                "Foo.dll|Bar.dll")]
        [InlineData("/r:Foo.dll|Foo.vb|/r:Bar.dll",                                                         "Foo.dll|Bar.dll")]
        public void Parse_SetsMetadataReferences(string arguments, string expected)
        {
            var service = CreateInstance();

            var results = service.Parse(arguments.Split('|'), @"C:\Project");

            Assert.Equal(expected.Split('|'), results.MetadataReferences.Select(r => r.Reference));
        }

        [Theory]
        [InlineData(@"Foo.vb",                                                                               @"C:\Project\Foo.vb")]
        [InlineData(@"Foo.vb|Bar.cs",                                                                        @"C:\Project\Foo.vb|C:\Project\Bar.cs")]
        [InlineData(@"C:\Foo\Foo.vb",                                                                        @"C:\Foo\Foo.vb")]
        [InlineData(@"..\Foo.vb",                                                                            @"C:\Project\..\Foo.vb")]
        public void Parse_SetsSourceFiles(string arguments, string expected)
        {
            var service = CreateInstance();

            var results = service.Parse(arguments.Split('|'), @"C:\Project");

            Assert.Equal(expected.Split('|'), results.SourceFiles.Select(r => r.Path));
        }

        [Theory]
        [InlineData(@"/a:Foo.dll",                                                                            @"Foo.dll")]
        [InlineData(@"/analyzer:C:\Foo.dll",                                                                  @"C:\Foo.dll")]
        public void Parse_SetsAnalyzerReferences(string arguments, string expected)
        {
            var service = CreateInstance();

            var results = service.Parse(arguments.Split('|'), @"C:\Project");

            Assert.Equal(expected.Split('|'), results.AnalyzerReferences.Select(r => r.FilePath));
        }

        [Theory]
        [InlineData(@"/additionalfile:Foo.txt",                                                               @"C:\Project\Foo.txt")]
        [InlineData(@"/additionalfile:C:\Foo.txt",                                                            @"C:\Foo.txt")]
        public void Parse_SetsAdditionalFiles(string arguments, string expected)
        {
            var service = CreateInstance();

            var results = service.Parse(arguments.Split('|'), @"C:\Project");

            Assert.Equal(expected.Split('|'), results.AdditionalFiles.Select(r => r.Path));
        }

        internal override ICommandLineParserService CreateInstance()
        {
            return new VisualBasicCommandLineParserService();
        }
    }
}
