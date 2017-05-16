// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to references that are passed to the compiler during design-time builds.
    /// </summary>
    [Export(typeof(ICommandLineHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharpLanguageService)]
    internal class MetadataReferenceItemHandler : ICommandLineHandler
    {
        private readonly UnconfiguredProject _unconfiguredProject;

        [ImportingConstructor]
        public MetadataReferenceItemHandler(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            _unconfiguredProject = project;
        }

        public void Handle(BuildOptions added, BuildOptions removed, IWorkspaceProjectContext context, bool isActiveContext)
        {
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));

            foreach (CommandLineReference reference in removed.MetadataReferences)
            {
                var fullPath = _unconfiguredProject.MakeRooted(reference.Reference);
                context.RemoveMetadataReference(fullPath);
            }

            foreach (CommandLineReference reference in added.MetadataReferences)
            {
                var fullPath = _unconfiguredProject.MakeRooted(reference.Reference);
                context.AddMetadataReference(fullPath, reference.Properties);
            }
        }
    }
}

