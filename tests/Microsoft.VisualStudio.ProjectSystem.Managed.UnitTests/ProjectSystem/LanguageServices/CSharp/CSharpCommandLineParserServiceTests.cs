// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.CSharp
{
    public class CSharpCommandLineParserServiceTests : CommandLineParserServiceTestBase
    {
        // This isn't supposed to be a exhaustive set of tests as we assume that Roslyn has tested their 
        // command-line parsing code, just enough to make sure we're passing the data through correctly.

        [Theory]
        [InlineData("/r:Foo.dll",                                                                           "Foo.dll")]
        [InlineData("/r:Foo.dll|/r:Bar.dll",                                                                "Foo.dll|Bar.dll")]
        [InlineData("/r:Foo.dll|Foo.cs|/r:Bar.dll",                                                         "Foo.dll|Bar.dll")]
        public void Parse_SetsMetadataReferences(string arguments, string expected)
        {
            var service = CreateInstance();

            var results = service.Parse(arguments.Split('|'), @"C:\Project");

            Assert.Equal(expected.Split('|'), results.MetadataReferences.Select(r => r.Reference));
        }

        [Theory]
        [InlineData(@"Foo.cs",                                                                               @"C:\Project\Foo.cs")]
        [InlineData(@"Foo.cs|Bar.cs",                                                                        @"C:\Project\Foo.cs|C:\Project\Bar.cs")]
        [InlineData(@"C:\Foo\Foo.cs",                                                                        @"C:\Foo\Foo.cs")]
        [InlineData(@"..\Foo.cs",                                                                            @"C:\Project\..\Foo.cs")]
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
            return new CSharpCommandLineParserService();
        }
    }
}
