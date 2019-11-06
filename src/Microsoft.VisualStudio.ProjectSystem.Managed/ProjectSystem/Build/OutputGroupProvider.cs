// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel.Composition;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.ProjectSystem;
    using Microsoft.VisualStudio.Threading;

    [Export(typeof(IOutputGroupProvider))]
    [AppliesTo(ProjectCapabilities.VisualStudioWellKnownOutputGroups)]
    [Order(1000)]
    internal class OutputGroupProvider : IOutputGroupProvider
    {
        /// <summary>
        /// List of well known output groups names and their associated target and description
        /// </summary>
        private static readonly ImmutableHashSet<IOutputGroup> s_outputGroups = ImmutableHashSet.Create<IOutputGroup>()
            .Add(NewGroup("PublishItems", "PublishItemsOutputGroup", Resources.OutputGroupPublishItemsDisplayName, Resources.OutputGroupPublishItemsDescription));

        /// <summary>
        /// The set of output groups that apply to this project.
        /// </summary>
        private readonly AsyncLazy<IImmutableSet<IOutputGroup>> _outputGroups;

        private readonly IProjectAccessor _projectAccessor;
        private readonly ConfiguredProject _configuredProject;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputGroupProvider"/> class.
        /// </summary>
        /// <param name="projectAccessor">Imported <see cref="IProjectAccessor"/>.</param>
        /// <param name="configuredProject">Imported <see cref="ConfiguredProject"/>.</param>
        [ImportingConstructor]
        private OutputGroupProvider(IProjectAccessor projectAccessor, ConfiguredProject configuredProject)
        {
            _projectAccessor = projectAccessor;
            _configuredProject = configuredProject;
            _outputGroups = new AsyncLazy<IImmutableSet<IOutputGroup>>(GetOutputGroupMetadataAsync);
        }

        #region IOutputGroupProvider Members

        /// <inheritdoc/>
        public Task<IImmutableSet<IOutputGroup>> OutputGroups
        {
            get { return _outputGroups.GetValueAsync(); }
        }

        #endregion

        /// <summary>
        /// Gets a collection of names of targets in this project.
        /// </summary>
        /// <returns>Collection of the names of targets in this project.</returns>
        private Task<ImmutableHashSet<string>> GetProjectTargetsAsync()
        {
            return _projectAccessor.OpenProjectForReadAsync(_configuredProject, project =>
                project.Targets.Keys.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Produces a set of output groups that is a subset of all well known output groups.
        /// </summary>
        /// <returns>Set of all known output groups.</returns>
        private async Task<IImmutableSet<IOutputGroup>> GetOutputGroupMetadataAsync()
        {
            // Start with the comment set of output groups.
            var result = s_outputGroups;

            // Remove any well known output group for which no target is defined.
            var targets = await GetProjectTargetsAsync();
            foreach (var outputGroup in result)
            {
                if (!targets.Contains(outputGroup.TargetName))
                {
                    result = result.Remove(outputGroup);
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a new output group with metadata only.
        /// </summary>
        /// <param name="name">Output group name.</param>
        /// <param name="targetName">Output group target name.</param>
        /// <param name="displayName">Output group display name.</param>
        /// <param name="description">Optional output group description.</param>
        /// <return>New <see cref="IOutputGroup"/>.</return>
        private static IOutputGroup NewGroup(string name, string targetName, string displayName, string description)
        {
            return new OutputGroup(name, targetName, displayName, description, ImmutableList<KeyValuePair<string, IImmutableDictionary<string, string>>>.Empty, false);
        }
    }
}
