// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(IParseBuildOptions))]
    [AppliesTo(ProjectCapability.FSharp)]
    internal class FSharpParseBuildOptions : IParseBuildOptions
    {
        private const string ReferencePrefix = "-r:";

        public BuildOptions Parse(IEnumerable<string> args, string baseDirectory)
        {
            var sourceFiles = new List<CommandLineSourceFile>();
            var additionalFiles = new List<CommandLineSourceFile>();
            var metadataReferences = new List<CommandLineReference>();

            foreach (var arg in args)
            {
                if (arg.StartsWith(ReferencePrefix))
                {
                    // e.g., -r:C:\Path\To\FSharp.Core.dll
                    metadataReferences.Add(new CommandLineReference(arg.Substring(ReferencePrefix.Length), MetadataReferenceProperties.Assembly));
                }
                else if (!arg.StartsWith("-"))
                {
                    // not an option, should be a regular file
                    var extension = Path.GetExtension(arg).ToLowerInvariant();
                    switch (extension)
                    {
                        case ".fs":
                        case ".fsi":
                        case ".fsx":
                        case ".fsscript":
                        case ".ml":
                        case ".mli":
                            sourceFiles.Add(new CommandLineSourceFile(arg, isScript: (extension == ".fsx") || (extension == ".fsscript")));
                            break;
                        default:
                            additionalFiles.Add(new CommandLineSourceFile(arg, isScript: false));
                            break;
                    }
                }
            }

            return new BuildOptions(
                sourceFiles,
                additionalFiles,
                metadataReferences,
                new CommandLineAnalyzerReference[0]);
        }
    }
}
