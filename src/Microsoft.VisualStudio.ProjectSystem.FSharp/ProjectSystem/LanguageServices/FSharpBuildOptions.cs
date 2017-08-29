// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal class FSharpBuildOptions :BuildOptions
    {
        public FSharpBuildOptions(ImmutableArray<CommandLineSourceFile> sourceFiles,
                                  ImmutableArray<CommandLineSourceFile> additionalFiles,
                                  ImmutableArray<CommandLineReference> metadataReferences,
                                  ImmutableArray<CommandLineAnalyzerReference> analyzerReferences,
                                  ImmutableArray<string> compileOptions)
            : base(sourceFiles, additionalFiles, metadataReferences, analyzerReferences)
        {
            CompileOptions = compileOptions;
        }

        public ImmutableArray<string> CompileOptions
        {
            get;
        }
    }
}
