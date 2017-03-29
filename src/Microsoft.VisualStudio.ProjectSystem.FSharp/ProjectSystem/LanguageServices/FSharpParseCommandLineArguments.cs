// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(IParseCommandLineArguments))]
    [AppliesTo(ProjectCapability.FSharp)]
    internal class FSharpParseCommandLineArguments : IParseCommandLineArguments
    {
        public CommandLineArguments Parse(IEnumerable<string> args, string baseDirectory)
        {
            // TODO: replace with F# command line parser
            return CommandLineArguments.FromCommonCommandLineArguments(
                CSharpCommandLineParser.Default.Parse(args, baseDirectory, sdkDirectory: null, additionalReferenceDirectories: null));
        }
    }
}
