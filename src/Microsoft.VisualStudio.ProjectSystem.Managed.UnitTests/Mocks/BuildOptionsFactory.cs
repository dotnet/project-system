// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class BuildOptionsFactory
    {
        public static BuildOptions CreateEmpty()
        {
            return new BuildOptions(ImmutableArray<CommandLineSourceFile>.Empty,
                                    ImmutableArray<CommandLineSourceFile>.Empty,
                                    ImmutableArray<CommandLineReference>.Empty,
                                    ImmutableArray<CommandLineAnalyzerReference>.Empty);
        }
    }
}
