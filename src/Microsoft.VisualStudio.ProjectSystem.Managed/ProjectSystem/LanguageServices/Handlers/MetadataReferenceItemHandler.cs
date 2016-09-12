// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to references that are passed to the compiler during design-time builds.
    /// </summary>
    [Export(typeof(ILanguageServiceCommandLineHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService2)]
    internal class MetadataReferenceFilesLanguageServiceItemHandler : ILanguageServiceCommandLineHandler
    {
        private IWorkspaceProjectContext _context;

        [ImportingConstructor]
        public MetadataReferenceFilesLanguageServiceItemHandler(UnconfiguredProject project)
        {
        }

        public void SetContext(IWorkspaceProjectContext context)
        {
            Requires.NotNull(context, nameof(context));

            _context = context;
        }

        public void Handle(CommandLineArguments added, CommandLineArguments removed)
        {
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));

            foreach (CommandLineReference reference in removed.MetadataReferences)
            {
                _context.RemoveMetadataReference(reference.Reference);
            }

            foreach (CommandLineReference reference in added.MetadataReferences)
            {
                _context.AddMetadataReference(reference.Reference, reference.Properties);
            }
        }
    }
}

