// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
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

        public ImmutableArray<string> CompileOptions
        {
            get;
        }
    }
}
