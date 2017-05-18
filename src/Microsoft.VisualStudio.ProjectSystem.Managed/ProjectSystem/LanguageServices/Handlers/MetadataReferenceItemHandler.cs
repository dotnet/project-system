﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to references that are passed to the compiler during design-time builds.
    /// </summary>
    [Export(typeof(ILanguageServiceCommandLineHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharpLanguageService)]
    internal class MetadataReferenceItemHandler : ILanguageServiceCommandLineHandler
    {
        private readonly UnconfiguredProject _unconfiguredProject;

        [ImportingConstructor]
        public MetadataReferenceItemHandler(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            _unconfiguredProject = project;
        }

        public void Handle(BuildOptions added, BuildOptions removed, IWorkspaceProjectContext context, bool isActiveContext, ProjectLoggerContext loggerContext)
        {
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));

            foreach (CommandLineReference reference in removed.MetadataReferences)
            {
                var fullPath = _unconfiguredProject.MakeRooted(reference.Reference);

                loggerContext.WriteLine("Removing reference {0}", fullPath);

                context.RemoveMetadataReference(fullPath);
            }

            foreach (CommandLineReference reference in added.MetadataReferences)
            {
                var fullPath = _unconfiguredProject.MakeRooted(reference.Reference);

                loggerContext.WriteLine("Adding reference {0}", fullPath);

                context.AddMetadataReference(fullPath, reference.Properties);
            }
        }
    }
}

