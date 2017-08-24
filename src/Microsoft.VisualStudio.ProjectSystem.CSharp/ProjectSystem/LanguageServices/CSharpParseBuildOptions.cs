// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(IParseBuildOptions))]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpParseBuildOptions : IParseBuildOptions
    {
        public BuildOptions Parse(IEnumerable<string> args, string projectPath)
        {
            return BuildOptions.FromCommandLineArguments(
                CSharpCommandLineParser.Default.Parse(args, Path.GetDirectoryName(projectPath), sdkDirectory: null, additionalReferenceDirectories: null));
        }
    }
}
