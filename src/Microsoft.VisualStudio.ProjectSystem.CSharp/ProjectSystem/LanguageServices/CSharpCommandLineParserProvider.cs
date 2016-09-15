// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal class CSharpCommandLineParserProvider
    {
        [ImportingConstructor]
        public CSharpCommandLineParserProvider()
        {
        }

        [Export(typeof(CommandLineParser))]
        [AppliesTo(ProjectCapability.CSharp)]
        public CommandLineParser Parser
        {
            get { return CSharpCommandLineParser.Default; }
        }
    }
}
