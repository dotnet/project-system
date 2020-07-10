// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
                                    ImmutableArray<CommandLineAnalyzerReference>.Empty,
                                    ImmutableArray<string>.Empty);
        }
    }
}
