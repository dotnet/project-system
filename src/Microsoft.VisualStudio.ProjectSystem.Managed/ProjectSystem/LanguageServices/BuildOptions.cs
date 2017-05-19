// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal class BuildOptions
    {
        public IEnumerable<CommandLineSourceFile> SourceFiles { get; }
        public IEnumerable<CommandLineSourceFile> AdditionalFiles { get; }
        public IEnumerable<CommandLineReference> MetadataReferences { get; }
        public IEnumerable<CommandLineAnalyzerReference> AnalyzerReferences { get; }

        public BuildOptions(IEnumerable<CommandLineSourceFile> sourceFiles, IEnumerable<CommandLineSourceFile> additionalFiles, IEnumerable<CommandLineReference> metadataReferences, IEnumerable<CommandLineAnalyzerReference> analyzerReferences)
        {
            Requires.NotNull(sourceFiles, nameof(sourceFiles));
            Requires.NotNull(additionalFiles, nameof(additionalFiles));
            Requires.NotNull(metadataReferences, nameof(metadataReferences));
            Requires.NotNull(analyzerReferences, nameof(analyzerReferences));

            SourceFiles = sourceFiles;
            AdditionalFiles = additionalFiles;
            MetadataReferences = metadataReferences;
            AnalyzerReferences = analyzerReferences;
        }

        public static BuildOptions FromCommonCommandLineArguments(CommandLineArguments commonCommandLineArguments)
        {
            Requires.NotNull(commonCommandLineArguments, nameof(commonCommandLineArguments));

            return new BuildOptions(
                commonCommandLineArguments.SourceFiles,
                commonCommandLineArguments.AdditionalFiles,
                commonCommandLineArguments.MetadataReferences,
                commonCommandLineArguments.AnalyzerReferences);
        }
    }
}
