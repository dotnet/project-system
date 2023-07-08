// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    [Export(typeof(IOutputGroupProvider))]
    [AppliesTo(ProjectCapabilities.VisualStudioWellKnownOutputGroups)]
    [Order(Order.Default)]
    internal class PublishItemsOutputGroupProvider : IOutputGroupProvider
    {
        private const string PublishItemsOutputGroupTargetName = "PublishItemsOutputGroup";

        /// <summary>
        /// Collection containing a single "publish items" output group.
        /// This is a singleton instance, shared across all projects/configurations.
        /// </summary>
        private static readonly ImmutableHashSet<IOutputGroup> s_outputGroups = ImmutableHashSet.Create<IOutputGroup>(
            new OutputGroup(
                name: "PublishItems",
                targetName: PublishItemsOutputGroupTargetName,
                displayName: Resources.OutputGroupPublishItemsDisplayName,
                description: Resources.OutputGroupPublishItemsDescription,
                items: ImmutableList<KeyValuePair<string, IImmutableDictionary<string, string>>>.Empty,
                successful: false));

        private readonly AsyncLazy<IImmutableSet<IOutputGroup>> _outputGroups;

        [ImportingConstructor]
        internal PublishItemsOutputGroupProvider(
            IProjectAccessor projectAccessor,
            ConfiguredProject configuredProject,
            IProjectThreadingService projectThreadingService)
        {
            _outputGroups = new AsyncLazy<IImmutableSet<IOutputGroup>>(
                GetOutputGroupMetadataAsync,
                projectThreadingService.JoinableTaskFactory);

            async Task<IImmutableSet<IOutputGroup>> GetOutputGroupMetadataAsync()
            {
                bool hasPublishItemsTarget = await projectAccessor.OpenProjectForReadAsync(
                    configuredProject,
                    project => project.Targets.ContainsKey(PublishItemsOutputGroupTargetName));

                return hasPublishItemsTarget
                    ? s_outputGroups
                    : ImmutableHashSet<IOutputGroup>.Empty;
            }
        }

        public Task<IImmutableSet<IOutputGroup>> OutputGroups => _outputGroups.GetValueAsync();
    }
}
