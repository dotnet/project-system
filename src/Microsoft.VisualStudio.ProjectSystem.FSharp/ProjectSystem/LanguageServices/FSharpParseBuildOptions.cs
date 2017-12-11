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
        private const string HyphenReferencePrefix = "-r:";
        private const string SlashReferencePrefix  = "/r:";
        private const string LongReferencePrefix   = "--reference:";

        private readonly UnconfiguredProject _project;

        [ImportMany]
        IEnumerable<Action<string, ImmutableArray<CommandLineSourceFile>, ImmutableArray<CommandLineReference>, ImmutableArray<string>>> _handlers =  null;

        [ImportingConstructor]
        public FSharpParseBuildOptions(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));
            _project = project;
        }

        public BuildOptions Parse(IEnumerable<string> commandLineArgs, string projectPath)
        {
            var sourceFiles = new List<CommandLineSourceFile>();
            var metadataReferences = new List<CommandLineReference>();
            var commandLineOptions = new List<String>();

            foreach (var commandLineArgument in commandLineArgs)
            {
                var args = commandLineArgument.Split(';');
                foreach (var arg in args)
                {
                    if (arg.StartsWith(HyphenReferencePrefix))
                    {
                        // e.g., /r:C:\Path\To\FSharp.Core.dll
                        metadataReferences.Add(new CommandLineReference(arg.Substring(HyphenReferencePrefix.Length), MetadataReferenceProperties.Assembly));
                    }
                    else if (arg.StartsWith(SlashReferencePrefix))
                    {
                        // e.g., -r:C:\Path\To\FSharp.Core.dll
                        metadataReferences.Add(new CommandLineReference(arg.Substring(SlashReferencePrefix.Length), MetadataReferenceProperties.Assembly));
                    }
                    else if (arg.StartsWith(LongReferencePrefix))
                    {
                        // e.g., --reference:C:\Path\To\FSharp.Core.dll
                        metadataReferences.Add(new CommandLineReference(arg.Substring(LongReferencePrefix.Length), MetadataReferenceProperties.Assembly));
                    }
                    else if (!(arg.StartsWith("-") || arg.StartsWith("/")))
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
                    else
                    {
                        // Neither a reference, nor a source file
                        commandLineOptions.Add(arg);
                    }
                }
            }

            return new FSharpBuildOptions(
                    sourceFiles: sourceFiles.ToImmutableArray(),
                    additionalFiles: ImmutableArray<CommandLineSourceFile>.Empty,
                    metadataReferences: metadataReferences.ToImmutableArray(),
                    analyzerReferences: ImmutableArray<CommandLineAnalyzerReference>.Empty,
                    compileOptions: commandLineOptions.ToImmutableArray());
        }

        [Export]
        [AppliesTo(ProjectCapability.FSharp)]
        public void HandleCommandLineNotifications(string projectPath, BuildOptions added, BuildOptions removed)
        {
            if (added is FSharpBuildOptions fscAdded)
            {
                foreach (var handler in _handlers)
                {
                    handler?.Invoke(_project.FullPath, fscAdded.SourceFiles, fscAdded.MetadataReferences, fscAdded.CompileOptions);
                }
            }
            return;
         }
    }
}
