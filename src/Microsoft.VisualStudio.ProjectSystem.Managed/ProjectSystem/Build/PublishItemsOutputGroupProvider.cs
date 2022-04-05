// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    [Export(typeof(IOutputGroupProvider))]
    [AppliesTo(ProjectCapabilities.VisualStudioWellKnownOutputGroups)]
    [Order(Order.Default)]
    internal class PublishItemsOutputGroupProvider : IOutputGroupProvider
    {
        /// <summary>
        /// List of well known output groups names and their associated target and description
        /// </summary>
        private static readonly ImmutableHashSet<IOutputGroup> s_outputGroups = ImmutableHashSet.Create<IOutputGroup>()
            .Add(NewGroup("PublishItems", "PublishItemsOutputGroup", Resources.OutputGroupPublishItemsDisplayName, Resources.OutputGroupPublishItemsDescription));

        private readonly AsyncLazy<IImmutableSet<IOutputGroup>> _outputGroups;
        private readonly IProjectAccessor _projectAccessor;
        private readonly ConfiguredProject _configuredProject;

        [ImportingConstructor]
        internal PublishItemsOutputGroupProvider(IProjectAccessor projectAccessor, ConfiguredProject configuredProject, IProjectThreadingService projectThreadingService)
        {
            _projectAccessor = projectAccessor;
            _configuredProject = configuredProject;
            _outputGroups = new AsyncLazy<IImmutableSet<IOutputGroup>>(GetOutputGroupMetadataAsync, projectThreadingService.JoinableTaskFactory);
        }

        public Task<IImmutableSet<IOutputGroup>> OutputGroups
        {
            get { return _outputGroups.GetValueAsync(); }
        }

        /// <summary>
        /// Gets a collection of names of targets in this project.
        /// </summary>
        /// <returns>Collection of the names of targets in this project.</returns>
        private Task<ImmutableHashSet<string>> GetProjectTargetsAsync()
        {
            return _projectAccessor.OpenProjectForReadAsync(_configuredProject, project =>
                project.Targets.Keys.ToImmutableHashSet(StringComparers.TargetNames));
        }

        /// <summary>
        /// Produces a set of output groups that is a subset of all well known output groups.
        /// </summary>
        /// <returns>Set of all known output groups.</returns>
        private async Task<IImmutableSet<IOutputGroup>> GetOutputGroupMetadataAsync()
        {
            // Start with the comment set of output groups.
            ImmutableHashSet<IOutputGroup> result = s_outputGroups;

            // Remove any well known output group for which no target is defined.
            ImmutableHashSet<string> targets = await GetProjectTargetsAsync();
            foreach (IOutputGroup outputGroup in result)
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
