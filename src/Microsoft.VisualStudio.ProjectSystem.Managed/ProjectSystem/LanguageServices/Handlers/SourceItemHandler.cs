// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to sources files during project evaluations and sources files that are passed
    ///     to the compiler during design-time builds.
    /// </summary>
    [Export(typeof(IWorkspaceContextHandler))]
    internal partial class SourceItemHandler : AbstractEvaluationCommandLineHandler, IProjectEvaluationHandler, ICommandLineHandler
    {
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public SourceItemHandler(UnconfiguredProject project)
            : base(project)
        {
            _project = project;
        }

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

        protected override void AddToContext(string fullPath, IImmutableDictionary<string, string> metadata, bool isActiveContext, IProjectLogger logger)
        {
            string[] folderNames = FileItemServices.GetLogicalFolderNames(Path.GetDirectoryName(_project.FullPath), fullPath, metadata);

            logger.WriteLine("Adding source file '{0}'", fullPath);
            Context.AddSourceFile(fullPath, isInCurrentContext: isActiveContext, folderNames: folderNames);
        }

        protected override void RemoveFromContext(string fullPath, IProjectLogger logger)
        {
            logger.WriteLine("Removing source file '{0}'", fullPath);
            Context.RemoveSourceFile(fullPath);
        }

        private IProjectChangeDiff ConvertToProjectDiff(BuildOptions added, BuildOptions removed)
        {
            var addedSet = ImmutableHashSet.ToImmutableHashSet(GetFilePaths(added), StringComparers.Paths);
            var removedSet = ImmutableHashSet.ToImmutableHashSet(GetFilePaths(removed), StringComparers.Paths);

            return new ProjectChangeDiff(addedSet, removedSet);
        }

        private IEnumerable<string> GetFilePaths(BuildOptions options)
        {
            return options.SourceFiles.Select(f => _project.MakeRelative(f.Path));
        }
    }
}
