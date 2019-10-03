// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using Microsoft.VisualStudio.ProjectSystem.Rename;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to Compile items during project evaluations and items that are passed
    ///     to the compiler during design-time builds.
    /// </summary>
    [Export(typeof(IWorkspaceContextHandler))]
    internal class CompileItemHandler : AbstractEvaluationCommandLineHandler, IProjectEvaluationHandler, ICommandLineHandler, IProjectUpdatedHandler
    {
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public CompileItemHandler(UnconfiguredProject project)
            : base(project)
        {
            _project = project;
        }

        [ImportMany]
        private readonly IEnumerable<IFileRenameHandler> _fileRenameHandlers = null!;

        public string ProjectEvaluationRule
        {
            get { return Compile.SchemaName; }
        }

        public void Handle(IComparable version, IProjectChangeDescription projectChange, bool isActiveContext, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(projectChange, nameof(projectChange));
            Requires.NotNull(logger, nameof(logger));

            VerifyInitialized();

            ApplyProjectEvaluation(version, projectChange.Difference, projectChange.After.Items, isActiveContext, logger);
        }

        public void Handle(IComparable version, BuildOptions added, BuildOptions removed, bool isActiveContext, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));
            Requires.NotNull(logger, nameof(logger));

            VerifyInitialized();

            IProjectChangeDiff difference = ConvertToProjectDiff(added, removed);

            ApplyProjectBuild(version, difference, isActiveContext, logger);
        }

        public void HandleProjectUpdate(IComparable version, IProjectChangeDescription projectChange, bool isActiveContext, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(projectChange, nameof(projectChange));
            Requires.NotNull(logger, nameof(logger));

            VerifyInitialized();

            foreach ((string original, string renamed) in projectChange.Difference.RenamedItems)
            {
                HandleItemRename(original, renamed, logger);
            }
        }

        protected override void AddToContext(string fullPath, IImmutableDictionary<string, string> metadata, bool isActiveContext, IProjectLogger logger)
        {
            string[]? folderNames = FileItemServices.GetLogicalFolderNames(Path.GetDirectoryName(_project.FullPath), fullPath, metadata);

            logger.WriteLine("Adding source file '{0}'", fullPath);
            Context.AddSourceFile(fullPath, isInCurrentContext: isActiveContext, folderNames: folderNames);
        }

        protected override void RemoveFromContext(string fullPath, IProjectLogger logger)
        {
            logger.WriteLine("Removing source file '{0}'", fullPath);
            Context.RemoveSourceFile(fullPath);
        }

        protected override void HandleItemRename(string fullPathBefore, string fullPathAfter, IProjectLogger logger)
        {
            logger.WriteLine("Handling rename of source file '{0}' to {1}", fullPathBefore, fullPathAfter);
            foreach (IFileRenameHandler fileRenameHandler in _fileRenameHandlers)
            {
                fileRenameHandler.HandleRename(fullPathBefore, fullPathAfter);
            }
        }

        private IProjectChangeDiff ConvertToProjectDiff(BuildOptions added, BuildOptions removed)
        {
            var addedSet = GetFilePaths(added).ToImmutableHashSet(StringComparers.Paths);
            var removedSet = GetFilePaths(removed).ToImmutableHashSet(StringComparers.Paths);

            return new ProjectChangeDiff(addedSet, removedSet);
        }

        private IEnumerable<string> GetFilePaths(BuildOptions options)
        {
            return options.SourceFiles.Select(f => _project.MakeRelative(f.Path));
        }
    }
}
