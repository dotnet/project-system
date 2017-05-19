// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to references that are passed to the compiler during design-time builds.
    /// </summary>
    internal class MetadataReferenceItemHandler : ICommandLineHandler
    {
        // WORKAROUND: To avoid Roslyn throwing when we add duplicate references, we remember what 
        // sent to them and avoid sending on duplicates.
        // See: https://github.com/dotnet/project-system/issues/2230

        private readonly UnconfiguredProject _project;
        private readonly IWorkspaceProjectContext _context;
        private readonly HashSet<string> _paths = new HashSet<string>(StringComparers.Paths);

        public MetadataReferenceItemHandler(UnconfiguredProject project, IWorkspaceProjectContext context)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(context, nameof(context));

            _project = project;
            _context = context;
        }

        public void Handle(IComparable version, BuildOptions added, BuildOptions removed, bool isActiveContext)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));

            foreach (CommandLineReference reference in removed.MetadataReferences)
            {
                var fullPath = _project.MakeRooted(reference.Reference);

                RemoveFromContextIfPresent(fullPath);
            }

            foreach (CommandLineReference reference in added.MetadataReferences)
            {
                var fullPath = _project.MakeRooted(reference.Reference);

                AddToContextIfNotPresent(fullPath, reference.Properties);
            }
        }

        private void AddToContextIfNotPresent(string fullPath, MetadataReferenceProperties properties)
        {
            if (!_paths.Contains(fullPath))
            {
                _context.AddMetadataReference(fullPath, properties);
                bool added = _paths.Add(fullPath);
                Assumes.True(added);
            }
        }

        private void RemoveFromContextIfPresent(string fullPath)
        {
            if (_paths.Contains(fullPath))
            {
                _context.RemoveMetadataReference(fullPath);
                bool removed = _paths.Remove(fullPath);
                Assumes.True(removed);
            }
        }
    }
}

