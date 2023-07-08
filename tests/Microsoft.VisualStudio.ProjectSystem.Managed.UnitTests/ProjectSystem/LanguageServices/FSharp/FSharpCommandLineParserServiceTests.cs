// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.FSharp
{
    public class FSharpCommandLineParserServiceTests : CommandLineParserServiceTestBase
    {
        [Theory]
        [InlineData("/r:Foo.dll",                                                                           "Foo.dll")]
        [InlineData("/r:Foo.dll|/r:Bar.dll",                                                                "Foo.dll|Bar.dll")]
        [InlineData("-r:Foo.dll",                                                                           "Foo.dll")]
        [InlineData("--reference:Foo.dll",                                                                  "Foo.dll")]
        [InlineData("/r:Foo.dll;/r:Bar.dll",                                                                "Foo.dll|Bar.dll")]
        [InlineData("/r:Foo.dll;--reference:Bar.dll",                                                       "Foo.dll|Bar.dll")]
        [InlineData("/r:Foo.dll;--reference:Bar.dll;-r:Baz.dll",                                            "Foo.dll|Bar.dll|Baz.dll")]
        [InlineData("/r:Foo.dll;--reference:Bar.dll;-r:Baz.dll|/r:System.dll",                              "Foo.dll|Bar.dll|Baz.dll|System.dll")]
        public void Parse_SetsMetadataReferences(string arguments, string expected)
        {
            var service = CreateInstance();

            var results = service.Parse(arguments.Split('|'), @"C:\Project");

            Assert.Equal(expected.Split('|'), results.MetadataReferences.Select(r => r.Reference));
        }

        [Theory]
        [InlineData("Foo.fs",                                                                               "Foo.fs")]
        [InlineData("Foo.fs;Bar.fs",                                                                        "Foo.fs|Bar.fs")]
        [InlineData("Foo.fs;Bar.fs|Baz.fs",                                                                 "Foo.fs|Bar.fs|Baz.fs")]
        [InlineData("/r:Foo.dll|Foo.fs;Bar.fs|Baz.fs",                                                      "Foo.fs|Bar.fs|Baz.fs")]
        [InlineData("Foo.fs;Bar.fs|/r:Foo.dll|Baz.fs",                                                      "Foo.fs|Bar.fs|Baz.fs")]
        [InlineData("Foo.fs;Bar.fs|Baz.fs|/r:Foo.dll",                                                      "Foo.fs|Bar.fs|Baz.fs")]
        public void Parse_SetsSourceFiles(string arguments, string expected)
        {
            var service = CreateInstance();

            var results = service.Parse(arguments.Split('|'), @"C:\Project");

            Assert.Equal(expected.Split('|'), results.SourceFiles.Select(r => r.Path));
        }

        [Theory]
        [InlineData("Foo.unknown")]
        [InlineData("Foo.cs")]
        [InlineData("Foo.vb")]
        public void Parse_IgnoresUnrecognizedExtensions(string arguments)
        {
            var service = CreateInstance();

            var results = service.Parse(arguments.Split('|'), @"C:\Project");

            Assert.Empty(results.SourceFiles);
        }

        [Theory]
        [InlineData("Foo.fsx",                                                                              true)]
        [InlineData("Foo.fsscript",                                                                         true)]
        [InlineData("Foo.fsi",                                                                              false)]
        [InlineData("Foo.fs",                                                                               false)]
        [InlineData("Foo.ml",                                                                               false)]
        [InlineData("Foo.mli",                                                                              false)]
        public void Parse_SetsIsScriptOnScriptFiles(string arguments, bool isScript)
        {
            var service = CreateInstance();

            var results = service.Parse(arguments.Split('|'), @"C:\Project");

            Assert.Single(results.SourceFiles.Select(r => r.IsScript), isScript);
        }

        [Theory]
        [InlineData("-switch",                                                                              "-switch")]
        [InlineData("-switch:bar",                                                                          "-switch:bar")]
        [InlineData("--switch",                                                                             "--switch")]
        [InlineData("--switch:bar",                                                                         "--switch:bar")]
        [InlineData("/switch",                                                                              "/switch")]
        [InlineData("/switch:bar",                                                                          "/switch:bar")]
        [InlineData("/switch:bar|foo.fsi|-switch",                                                          "/switch:bar|-switch")]
        [InlineData("/switch:bar|/r:foo.dll|-switch",                                                       "/switch:bar|-switch")]
        public void Parse_SetsCompileOptionsToUnrecognizedSwitches(string arguments, string expected)
        {
            var service = CreateInstance();

            var results = (FSharpBuildOptions)service.Parse(arguments.Split('|'), @"C:\Project");

            Assert.Equal(expected.Split('|'), results.CompileOptions);
        }

        [Theory]
        [InlineData("/r:Foo.dll")]
        [InlineData("Foo.fs")]
        [InlineData("-a:Foo.dll")]
        [InlineData("-analyzer:Foo.dll")]
        [InlineData("-additionalfile:Foo.dll")]
        public void Parse_SetsAdditionalFilesAndAnalyzerReferencesToEmpty(string arguments)
        {
            var service = CreateInstance();

            var results = service.Parse(arguments.Split('|'), @"C:\Project");

            Assert.Empty(results.AdditionalFiles);
            Assert.Empty(results.AnalyzerReferences);
        }

        internal override ICommandLineParserService CreateInstance()
        {
            return new FSharpCommandLineParserService();
        }
    }
}
