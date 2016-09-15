// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal class VisualBasicCommandLineParserProvider
    {
        [ImportingConstructor]
        public VisualBasicCommandLineParserProvider()
        {
        }

        [Export(typeof(CommandLineParser))]
        [AppliesTo(ProjectCapability.VisualBasic)]
        public CommandLineParser Parser
        {
            get { return VisualBasicCommandLineParser.Default; }
        }
    }
}
