// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.FSharp
{
    [Export(typeof(ICommandLineParserService))]
    [AppliesTo(ProjectCapability.FSharp)]
    internal class FSharpCommandLineParserService : ICommandLineParserService
    {
        private const string HyphenReferencePrefix = "-r:";
        private const string SlashReferencePrefix = "/r:";
        private const string LongReferencePrefix = "--reference:";

        [ImportMany]
        private readonly IEnumerable<Action<string, ImmutableArray<CommandLineSourceFile>, ImmutableArray<CommandLineReference>, ImmutableArray<string>>> _handlers = null!;

        [ImportingConstructor]
        public FSharpCommandLineParserService()
        {
        }

        public BuildOptions Parse(IEnumerable<string> arguments, string baseDirectory)
        {
            Requires.NotNull(arguments, nameof(arguments));
            Requires.NotNullOrEmpty(baseDirectory, nameof(baseDirectory));

            var sourceFiles = new List<CommandLineSourceFile>();
            var metadataReferences = new List<CommandLineReference>();
            var commandLineOptions = new List<string>();

            foreach (string commandLineArgument in arguments)
            {
                foreach (string arg in new LazyStringSplit(commandLineArgument, ';'))
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
                        string extension = Path.GetExtension(arg).ToLowerInvariant();
                        if (extension is
                            ".fs" or
                            ".fsi" or
                            ".fsx" or
                            ".fsscript" or
                            ".ml" or
                            ".mli")
                        {
                            sourceFiles.Add(new CommandLineSourceFile(arg, isScript: (extension == ".fsx") || (extension == ".fsscript")));
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
        public void HandleCommandLineNotifications(string binPath, BuildOptions added, BuildOptions removed)
        {
            if (added is FSharpBuildOptions fscAdded)
            {
                foreach (Action<string, ImmutableArray<CommandLineSourceFile>, ImmutableArray<CommandLineReference>, ImmutableArray<string>> handler in _handlers)
                {
                    handler?.Invoke(binPath, fscAdded.SourceFiles, fscAdded.MetadataReferences, fscAdded.CompileOptions);
                }
            }
        }
    }
}
