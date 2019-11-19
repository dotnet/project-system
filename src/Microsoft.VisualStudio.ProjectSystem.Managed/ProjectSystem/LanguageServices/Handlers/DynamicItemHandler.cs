// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to dynamic items, such as Razor CSHTML files.
    /// </summary>
    [Export(typeof(IWorkspaceContextHandler))]
    internal class DynamicItemHandler : AbstractWorkspaceContextHandler, IProjectEvaluationHandler
    {
        private const string RazorPagesExtension = ".cshtml";
        private const string RazorComponentsExtension = ".razor";
        private readonly UnconfiguredProject _project;
        private readonly HashSet<string> _paths = new HashSet<string>(StringComparers.Paths);

        [ImportingConstructor]
        public DynamicItemHandler(UnconfiguredProject project)
        {
            _project = project;
        }

        public string ProjectEvaluationRule
        {
            get { return Content.SchemaName; }
        }

        public void Handle(IComparable version, IProjectChangeDescription projectChange, bool isActiveContext, IProjectLogger logger)
        {
            Requires.NotNull(version, nameof(version));
            Requires.NotNull(projectChange, nameof(projectChange));
            Requires.NotNull(logger, nameof(logger));

            VerifyInitialized();

            IProjectChangeDiff difference = HandlerServices.NormalizeRenames(projectChange.Difference);

            foreach (string includePath in difference.RemovedItems)
            {
                if (IsDynamicFile(includePath))
                {
                    RemoveFromContextIfPresent(includePath, logger);
                }
            }

            foreach (string includePath in difference.AddedItems)
            {
                if (IsDynamicFile(includePath))
                {
                    IImmutableDictionary<string, string> metadata = projectChange.After.Items.GetValueOrDefault(includePath, ImmutableStringDictionary<string>.EmptyOrdinal);

                    AddToContextIfNotPresent(includePath, metadata, logger);
                }
            }

            // We Remove then Add changed items to pick up the Linked metadata
            foreach (string includePath in difference.ChangedItems)
            {
                if (IsDynamicFile(includePath))
                {
                    IImmutableDictionary<string, string> metadata = projectChange.After.Items.GetValueOrDefault(includePath, ImmutableStringDictionary<string>.EmptyOrdinal);

                    RemoveFromContextIfPresent(includePath, logger);
                    AddToContextIfNotPresent(includePath, metadata, logger);
                }
            }
        }

        private void AddToContextIfNotPresent(string includePath, IImmutableDictionary<string, string> metadata, IProjectLogger logger)
        {
            string fullPath = _project.MakeRooted(includePath);

            if (!_paths.Contains(fullPath))
            {
                string[]? folderNames = FileItemServices.GetLogicalFolderNames(Path.GetDirectoryName(_project.FullPath), fullPath, metadata);

                logger.WriteLine("Adding dynamic file '{0}'", fullPath);
                Context.AddDynamicFile(fullPath, folderNames);
                bool added = _paths.Add(fullPath);
                Assumes.True(added);
            }
        }

        private void RemoveFromContextIfPresent(string includePath, IProjectLogger logger)
        {
            string fullPath = _project.MakeRooted(includePath);

            if (_paths.Contains(fullPath))
            {
                logger.WriteLine("Removing dynamic file '{0}'", fullPath);
                Context.RemoveDynamicFile(fullPath);

                bool removed = _paths.Remove(fullPath);
                Assumes.True(removed);
            }
        }

        private static bool IsDynamicFile(string includePath)
        {
            // Note a file called just '.cshtml' is still considered a Razor file
            return includePath.EndsWith(RazorPagesExtension, StringComparisons.Paths) ||
                   includePath.EndsWith(RazorComponentsExtension, StringComparisons.Paths);
        }
    }
}
