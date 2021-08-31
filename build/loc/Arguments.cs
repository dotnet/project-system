// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using CommandLine;

namespace OneLocBuildSetup
{
    internal class Arguments
    {
        [Option('r', "repo", Required = true, HelpText = "The repository's root path.")]
        public string RepositoryPath { get; set; } = string.Empty;

        [Option('o', "output", Required = true, HelpText = "The output path for the LocProject.json.")]
        public string OutputPath { get; set; } = string.Empty;
    }
}
