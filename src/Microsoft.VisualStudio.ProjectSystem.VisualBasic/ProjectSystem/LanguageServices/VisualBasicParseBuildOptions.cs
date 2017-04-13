// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(IParseBuildOptions))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicParseBuildOptions : IParseBuildOptions
    {
        public BuildOptions Parse(IEnumerable<string> args, string baseDirectory)
        {
            return BuildOptions.FromCommonCommandLineArguments(
                VisualBasicCommandLineParser.Default.Parse(args, baseDirectory, sdkDirectory: null, additionalReferenceDirectories: null));
        }
    }
}
