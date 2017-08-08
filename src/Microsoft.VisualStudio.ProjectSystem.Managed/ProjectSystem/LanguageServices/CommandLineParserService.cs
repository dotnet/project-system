// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

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
            CommandLineParsers = new OrderPrecedenceImportCollection<IParseBuildOptions>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IParseBuildOptions> CommandLineParsers
        {
            get;
        }

        public BuildOptions Parse(IEnumerable<string> arguments)
        {
            Requires.NotNull(arguments, nameof(arguments));

            Lazy<IParseBuildOptions> parser = CommandLineParsers.FirstOrDefault();
            if (parser == null)
                throw new InvalidOperationException();

            return parser.Value.Parse(arguments, _project.FullPath);
        }
    }
}
