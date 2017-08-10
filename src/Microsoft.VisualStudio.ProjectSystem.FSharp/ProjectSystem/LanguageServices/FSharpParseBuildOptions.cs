// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private const string LongReferencePrefix = "--reference:";

        [ImportMany]
        IEnumerable<Action<string, ImmutableArray<CommandLineSourceFile>, ImmutableArray<CommandLineReference>>> Handlers =  null;

        public BuildOptions Parse(IEnumerable<string> commandLineArgs, string projectPath)
        {
            var sourceFiles = new List<CommandLineSourceFile>();
            var metadataReferences = new List<CommandLineReference>();

            foreach (var commandLineArgument in commandLineArgs)
            {
                var args = commandLineArgument.Split(';');
                foreach (var arg in args)
                {
                    if (arg.StartsWith(ReferencePrefix))
                    {
                        // e.g., -r:C:\Path\To\FSharp.Core.dll
                        metadataReferences.Add(new CommandLineReference(arg.Substring(ReferencePrefix.Length), MetadataReferenceProperties.Assembly));
                    }
                    else if (arg.StartsWith(LongReferencePrefix))
                    {
                        // e.g., --reference:C:\Path\To\FSharp.Core.dll
                        metadataReferences.Add(new CommandLineReference(arg.Substring(LongReferencePrefix.Length), MetadataReferenceProperties.Assembly));
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
                                break;
                        }
                    }
                }
            }

            return new BuildOptions(
                    sourceFiles: sourceFiles.ToImmutableArray(),
                    additionalFiles: ImmutableArray<CommandLineSourceFile>.Empty,
                    metadataReferences: metadataReferences.ToImmutableArray(),
                    analyzerReferences: ImmutableArray<CommandLineAnalyzerReference>.Empty);
        }

        [Export]
        [AppliesTo(ProjectCapability.FSharp)]
        public void HandleCommandLineNotifications(string projectPath, BuildOptions addedOptions, BuildOptions removedOptions)
        {
            foreach (var handler in Handlers)
            {
                handler?.Invoke(projectPath, addedOptions.SourceFiles, addedOptions.MetadataReferences);
            }
        }

    }
}
