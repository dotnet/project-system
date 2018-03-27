// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to references that are passed to the compiler during design-time builds.
    /// </summary>
    [Export(typeof(IWorkspaceContextHandler))]
    internal class MetadataReferenceItemHandler : ICommandLineHandler
    {
        // WORKAROUND: The language services through IWorkspaceProjectContext doesn't expect to see AddMetadataReference called more than
        // once with the same path and different properties. This dedups the references to work around this limitation.
        // See: https://github.com/dotnet/project-system/issues/2230

        private readonly UnconfiguredProject _project;
        private IWorkspaceProjectContext _context;
        private readonly Dictionary<string, MetadataReferenceProperties> _addedPathsWithMetadata = new Dictionary<string, MetadataReferenceProperties>(StringComparers.Paths);

        [ImportingConstructor]
        public MetadataReferenceItemHandler(UnconfiguredProject project)
            : this(project, null)
        {
        }

        public MetadataReferenceItemHandler(UnconfiguredProject project, IWorkspaceProjectContext context)
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

            foreach (CommandLineReference reference in removed.MetadataReferences)
            {
                string fullPath = _project.MakeRooted(reference.Reference);

                RemoveFromContextIfPresent(fullPath, reference.Properties, logger);
            }

            foreach (CommandLineReference reference in added.MetadataReferences)
            {
                string fullPath = _project.MakeRooted(reference.Reference);

                AddToContextIfNotPresent(fullPath, reference.Properties, logger);
            }
        }

        private void AddToContextIfNotPresent(string fullPath, MetadataReferenceProperties properties, IProjectLogger logger)
        {
            if (_addedPathsWithMetadata.TryGetValue(fullPath, out MetadataReferenceProperties existingProperties))
            {
                logger.WriteLine("Removing existing {0} '{1}' so we can update the aliases.", properties.EmbedInteropTypes ? "link" : "reference", fullPath);

                // The reference has already been added previously. The current implementation of IWorkspaceProjectContext
                // presumes that we'll only called AddMetadataReference once for a given path. Thus we have to remove the
                // existing one, compute merged properties, and add the new one.
                _context.RemoveMetadataReference(fullPath);

                ImmutableArray<string> combinedAliases = GetEmptyIfGlobalAlias(GetGlobalAliasIfEmpty(existingProperties.Aliases).AddRange(GetGlobalAliasIfEmpty(properties.Aliases)));
                properties = properties.WithAliases(combinedAliases);
            }

            logger.WriteLine("Adding {0} '{1}'", properties.EmbedInteropTypes ? "link" : "reference", fullPath);

            _context.AddMetadataReference(fullPath, properties);
            _addedPathsWithMetadata[fullPath] = properties;
        }

        private void RemoveFromContextIfPresent(string fullPath, MetadataReferenceProperties properties, IProjectLogger logger)
        {
            if (_addedPathsWithMetadata.TryGetValue(fullPath, out MetadataReferenceProperties existingProperties))
            {
                logger.WriteLine("Removing {0} '{1}'", properties.EmbedInteropTypes ? "link" : "reference", fullPath);

                _context.RemoveMetadataReference(fullPath);

                // Subtract any existing aliases out. This will be an empty list if we should remove the reference entirely
                ImmutableArray<string> resultantAliases = GetGlobalAliasIfEmpty(existingProperties.Aliases).RemoveRange(GetGlobalAliasIfEmpty(properties.Aliases));

                if (resultantAliases.IsEmpty)
                {
                    // There's nothing left here, completely remove it
                    Assumes.True(_addedPathsWithMetadata.Remove(fullPath));
                }
                else
                {
                    // resultantAliases might be the global alias. In that case, let's remove it again.
                    resultantAliases = GetEmptyIfGlobalAlias(resultantAliases);
                    properties = properties.WithAliases(resultantAliases);
                    logger.WriteLine("Adding {0} '{1}' back with remaining aliases", properties.EmbedInteropTypes ? "link" : "reference", fullPath);
                    _context.AddMetadataReference(fullPath, properties);
                    _addedPathsWithMetadata[fullPath] = properties;
                }
            }
        }

        private static readonly ImmutableArray<string> s_listWithGlobalAlias = ImmutableArray.Create(MetadataReferenceProperties.GlobalAlias);

        /// <summary>
        /// Returns the list of aliases, replacing an empty list with "global".
        /// </summary>
        private static ImmutableArray<string> GetGlobalAliasIfEmpty(ImmutableArray<string> aliases)
        {
            if (aliases.IsDefaultOrEmpty)
            {
                return s_listWithGlobalAlias;

            }
            return aliases;
        }

        /// <summary>
        /// Returns the list of aliases, replacing a list containing just "global" back to the empty list.
        /// </summary>
        private static ImmutableArray<string> GetEmptyIfGlobalAlias(ImmutableArray<string> aliases)
        {
            if (aliases.Length == 1 && aliases[0] == MetadataReferenceProperties.GlobalAlias)
            {
                return ImmutableArray<string>.Empty;
            }

            return aliases;
        }
    }
}

