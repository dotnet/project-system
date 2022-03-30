// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.FSharp
{
    internal class FSharpBuildOptions : BuildOptions
    {
        public FSharpBuildOptions(ImmutableArray<CommandLineSourceFile> sourceFiles,
                                  ImmutableArray<CommandLineSourceFile> additionalFiles,
                                  ImmutableArray<CommandLineReference> metadataReferences,
                                  ImmutableArray<CommandLineAnalyzerReference> analyzerReferences,
                                  ImmutableArray<string> compileOptions)
            : base(sourceFiles, additionalFiles, metadataReferences, analyzerReferences, ImmutableArray<string>.Empty)
        {
            CompileOptions = compileOptions;
        }

        public ImmutableArray<string> CompileOptions { get; }
    }
}
