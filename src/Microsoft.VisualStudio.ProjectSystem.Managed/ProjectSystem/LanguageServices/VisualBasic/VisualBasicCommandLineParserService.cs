﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.VisualBasic;

[Export(typeof(ICommandLineParserService))]
[AppliesTo(ProjectCapability.VisualBasic)]
internal class VisualBasicCommandLineParserService : ICommandLineParserService
{
    public BuildOptions Parse(IEnumerable<string> arguments, string baseDirectory)
    {
        Requires.NotNull(arguments);
        Requires.NotNullOrEmpty(baseDirectory);

        return BuildOptions.FromCommandLineArguments(
            VisualBasicCommandLineParser.Default.Parse(arguments, baseDirectory, sdkDirectory: null, additionalReferenceDirectories: null));
    }
}
