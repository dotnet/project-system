// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.CSharp
{
    [Export(typeof(ICommandLineParserService))]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpCommandLineParserService : ICommandLineParserService
    {
        public BuildOptions Parse(IEnumerable<string> arguments, string baseDirectory)
        {
            Requires.NotNull(arguments, nameof(arguments));
            Requires.NotNullOrEmpty(baseDirectory, nameof(baseDirectory));

            return BuildOptions.FromCommandLineArguments(
                CSharpCommandLineParser.Default.Parse(arguments, baseDirectory, sdkDirectory: null, additionalReferenceDirectories: null));
        }
    }
}
