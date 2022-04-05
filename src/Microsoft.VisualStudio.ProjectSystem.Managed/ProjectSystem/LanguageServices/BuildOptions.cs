// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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

        public ImmutableArray<CommandLineSourceFile> SourceFiles { get; }

        public ImmutableArray<CommandLineSourceFile> AdditionalFiles { get; }

        public ImmutableArray<CommandLineReference> MetadataReferences { get; }

        public ImmutableArray<CommandLineAnalyzerReference> AnalyzerReferences { get; }

        public ImmutableArray<string> AnalyzerConfigFiles { get; }

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
