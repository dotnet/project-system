// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal class FSharpCommandLineParserProvider
    {
        [ImportingConstructor]
        public FSharpCommandLineParserProvider()
        {
        }

        [Export(typeof(CommandLineParser))]
        [AppliesTo(ProjectCapability.FSharp)]
        public CommandLineParser Parser
        {
            // TODO: Create a FSharpCommandLineParser that calls into the F# compiler to parse the command line arguments.
            get { return CSharpCommandLineParser.Default; }
        }
    }
}
