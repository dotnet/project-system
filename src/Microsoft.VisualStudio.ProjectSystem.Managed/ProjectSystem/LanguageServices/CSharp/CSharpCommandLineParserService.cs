// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
