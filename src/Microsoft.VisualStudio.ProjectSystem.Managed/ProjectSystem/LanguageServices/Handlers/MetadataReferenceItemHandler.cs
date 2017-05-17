// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to references that are passed to the compiler during design-time builds.
    /// </summary>
    internal class MetadataReferenceItemHandler : ICommandLineHandler
    {
        private readonly UnconfiguredProject _project;
        private readonly IWorkspaceProjectContext _context;

        public MetadataReferenceItemHandler(UnconfiguredProject project, IWorkspaceProjectContext context)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(context, nameof(context));

            _project = project;
            _context = context;
        }

        public void Handle(BuildOptions added, BuildOptions removed, bool isActiveContext)
        {
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));

            foreach (CommandLineReference reference in removed.MetadataReferences)
            {
                var fullPath = _project.MakeRooted(reference.Reference);
                _context.RemoveMetadataReference(fullPath);
            }

            foreach (CommandLineReference reference in added.MetadataReferences)
            {
                var fullPath = _project.MakeRooted(reference.Reference);
                _context.AddMetadataReference(fullPath, reference.Properties);
            }
        }
    }
}

