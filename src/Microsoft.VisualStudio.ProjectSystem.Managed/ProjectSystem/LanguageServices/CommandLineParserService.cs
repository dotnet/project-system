// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(ICommandLineParserService))]
    internal class CommandLineParserService : ICommandLineParserService
    {
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public CommandLineParserService(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            _project = project;
            CommandLineParsers = new OrderPrecedenceImportCollection<CommandLineParser>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<CommandLineParser> CommandLineParsers
        {
            get;
        }

        public CommandLineArguments Parse(IEnumerable<string> arguments)
        {
            Requires.NotNull(arguments, nameof(arguments));

            Lazy<CommandLineParser> parser = CommandLineParsers.FirstOrDefault();
            if (parser == null)
                throw new InvalidOperationException();

            return parser.Value.Parse(arguments, Path.GetDirectoryName(_project.FullPath), sdkDirectory: null, additionalReferenceDirectories: null);
        }
    }
}
