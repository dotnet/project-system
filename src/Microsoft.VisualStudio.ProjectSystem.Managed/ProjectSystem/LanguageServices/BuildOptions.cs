// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal class BuildOptions
    {
        public BuildOptions(
            ImmutableArray<CommandLineSourceFile> sourceFiles,
            ImmutableArray<CommandLineSourceFile> additionalFiles,
            ImmutableArray<CommandLineReference> metadataReferences,
            ImmutableArray<CommandLineAnalyzerReference> analyzerReferences,
            ImmutableArray<string> analyzerConfigFiles)
        {
            SourceFiles = sourceFiles;
            AdditionalFiles = additionalFiles;
            MetadataReferences = metadataReferences;
            AnalyzerReferences = analyzerReferences;
            AnalyzerConfigFiles = analyzerConfigFiles;
        }

        public ImmutableArray<CommandLineSourceFile> SourceFiles
        {
            get;
        }

        public ImmutableArray<CommandLineSourceFile> AdditionalFiles
        {
            get;
        }

        public ImmutableArray<CommandLineReference> MetadataReferences
        {
            get;
        }

        public ImmutableArray<CommandLineAnalyzerReference> AnalyzerReferences
        {
            get;
        }

        public ImmutableArray<string> AnalyzerConfigFiles
        {
            get;
        }

        public static BuildOptions FromCommandLineArguments(CommandLineArguments commandLineArguments)
        {
            Requires.NotNull(commandLineArguments, nameof(commandLineArguments));

            return new BuildOptions(
                commandLineArguments.SourceFiles,
                commandLineArguments.AdditionalFiles,
                commandLineArguments.MetadataReferences,
                commandLineArguments.AnalyzerReferences,
                commandLineArguments.AnalyzerConfigPaths);
        }
    }
}
