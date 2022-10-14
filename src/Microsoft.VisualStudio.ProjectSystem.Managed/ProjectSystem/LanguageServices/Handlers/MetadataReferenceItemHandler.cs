// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to references that are passed to the compiler during design-time builds.
    /// </summary>
    [Export(typeof(IWorkspaceUpdateHandler))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class MetadataReferenceItemHandler : IWorkspaceUpdateHandler, ICommandLineHandler
    {
        // WORKAROUND: The language services through IWorkspaceProjectContext doesn't expect to see AddMetadataReference called more than
        // once with the same path and different properties. This dedupes the references to work around this limitation.
        // See: https://github.com/dotnet/project-system/issues/2230

        private readonly UnconfiguredProject _project;
        private readonly Dictionary<string, MetadataReferenceProperties> _addedPathsWithMetadata = new(StringComparers.Paths);

        [ImportingConstructor]
        public MetadataReferenceItemHandler(UnconfiguredProject project)
        {
            _project = project;
        }

        public void Handle(IWorkspaceProjectContext context, IComparable version, BuildOptions added, BuildOptions removed, ContextState state, IManagedProjectDiagnosticOutputService logger)
        {
            foreach (CommandLineReference reference in removed.MetadataReferences)
            {
                string fullPath = _project.MakeRooted(reference.Reference);

                RemoveFromContextIfPresent(context, fullPath, reference.Properties, logger);
            }

            foreach (CommandLineReference reference in added.MetadataReferences)
            {
                string fullPath = _project.MakeRooted(reference.Reference);

                AddToContextIfNotPresent(context, fullPath, reference.Properties, logger);
            }
        }

        private void AddToContextIfNotPresent(IWorkspaceProjectContext context, string fullPath, MetadataReferenceProperties properties, IManagedProjectDiagnosticOutputService logger)
        {
            if (_addedPathsWithMetadata.TryGetValue(fullPath, out MetadataReferenceProperties existingProperties))
            {
                logger.WriteLine("Removing existing {0} '{1}' so we can update the aliases.", properties.EmbedInteropTypes ? "link" : "reference", fullPath);

                // The reference has already been added previously. The current implementation of IWorkspaceProjectContext
                // presumes that we'll only called AddMetadataReference once for a given path. Thus we have to remove the
                // existing one, compute merged properties, and add the new one.
                context.RemoveMetadataReference(fullPath);

                ImmutableArray<string> combinedAliases = GetEmptyIfGlobalAlias(GetGlobalAliasIfEmpty(existingProperties.Aliases).AddRange(GetGlobalAliasIfEmpty(properties.Aliases)));
                properties = properties.WithAliases(combinedAliases);
            }

            logger.WriteLine("Adding {0} '{1}'", properties.EmbedInteropTypes ? "link" : "reference", fullPath);

            context.AddMetadataReference(fullPath, properties);
            _addedPathsWithMetadata[fullPath] = properties;
        }

        private void RemoveFromContextIfPresent(IWorkspaceProjectContext context, string fullPath, MetadataReferenceProperties properties, IManagedProjectDiagnosticOutputService logger)
        {
            if (_addedPathsWithMetadata.TryGetValue(fullPath, out MetadataReferenceProperties existingProperties))
            {
                logger.WriteLine("Removing {0} '{1}'", properties.EmbedInteropTypes ? "link" : "reference", fullPath);

                context.RemoveMetadataReference(fullPath);

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
                    context.AddMetadataReference(fullPath, properties);
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

