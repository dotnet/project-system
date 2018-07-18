// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to the  &lt;AdditionalFiles/&gt; item during design-time builds.
    /// </summary>
    [Export(typeof(IWorkspaceContextHandler))]
    internal class AdditionalFilesItemHandler : ICommandLineHandler
    {
        // WORKAROUND: To avoid Roslyn throwing when we add duplicate additonal files, we remember what 
        // sent to them and avoid sending on duplicates.
        // See: https://github.com/dotnet/project-system/issues/2230

        private readonly UnconfiguredProject _project;
        private IWorkspaceProjectContext _context;
        private readonly HashSet<string> _paths = new HashSet<string>(StringComparers.Paths);

        [ImportingConstructor]
        public AdditionalFilesItemHandler(UnconfiguredProject project)
            : this(project, null)
        {
        }

        public AdditionalFilesItemHandler(UnconfiguredProject project, IWorkspaceProjectContext context)
        {
            Requires.NotNull(project, nameof(project));
            
            _project = project;
            _context = context;
        }

        public void Initialize(IWorkspaceProjectContext context)
        {
            _context = context;
        }

        public void Handle(IComparable version, BuildOptions added, BuildOptions removed, bool isActiveContext, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));
            Requires.NotNull(logger, nameof(logger));

            foreach (CommandLineSourceFile additionalFile in removed.AdditionalFiles)
            {
                string fullPath = _project.MakeRooted(additionalFile.Path);

                RemoveFromContextIfPresent(fullPath, logger);
            }

            foreach (CommandLineSourceFile additionalFile in added.AdditionalFiles)
            {
                string fullPath = _project.MakeRooted(additionalFile.Path);

                AddToContextIfNotPresent(fullPath, isActiveContext, logger);
            }
        }

        private void AddToContextIfNotPresent(string fullPath, bool isActiveContext, IProjectLogger logger)
        {
            if (!_paths.Contains(fullPath))
            {
                logger.WriteLine("Adding additional file '{0}'", fullPath);
                _context.AddAdditionalFile(fullPath, isActiveContext);
                bool added = _paths.Add(fullPath);
                Assumes.True(added);
            }
        }

        private void RemoveFromContextIfPresent(string fullPath, IProjectLogger logger)
        {
            if (_paths.Contains(fullPath))
            {
                logger.WriteLine("Removing additional file '{0}'", fullPath);
                _context.RemoveAdditionalFile(fullPath);
                bool removed = _paths.Remove(fullPath);
                Assumes.True(removed);
            }
        }
    }
}
