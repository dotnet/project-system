// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(ICommandLineHandler))]
    [AppliesTo(ProjectCapability.FSharp)]
    internal class FSharpCommandLineHandler : ICommandLineHandler
    {
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public FSharpCommandLineHandler(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));
            _project = project;
        }


        [ImportMany]
        IEnumerable<Action<string, ImmutableArray<CommandLineSourceFile>, ImmutableArray<CommandLineReference>, ImmutableArray<string>>> Handlers = null;

        public void Handle(IComparable version, BuildOptions added, BuildOptions removed, bool isActiveContext, IProjectLogger logger)
        {
            if (added is FSharpBuildOptions fscAdded)
            {
                foreach (var handler in Handlers)
                {
                    handler?.Invoke(_project.FullPath, fscAdded.SourceFiles, fscAdded.MetadataReferences, fscAdded.CompileOptions);
                }
            }
        }
    }
}

