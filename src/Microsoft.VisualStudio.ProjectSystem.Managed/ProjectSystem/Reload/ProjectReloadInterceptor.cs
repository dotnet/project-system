// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Construction;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Reloads a project if its configuration dimensions change.
    /// </summary>
    [Export(typeof(IProjectReloadInterceptor))]
    [AppliesTo(ProjectCapabilities.ProjectConfigurationsDeclaredDimensions)]
    internal sealed class ProjectReloadInterceptor : IProjectReloadInterceptor
    {
        [ImportingConstructor]
        public ProjectReloadInterceptor(UnconfiguredProject project)
        {
            DimensionProviders = new OrderPrecedenceImportCollection<IProjectConfigurationDimensionsProvider>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IProjectConfigurationDimensionsProvider> DimensionProviders { get; }

        public ProjectReloadResult InterceptProjectReload(ImmutableArray<ProjectPropertyElement> oldProperties, ImmutableArray<ProjectPropertyElement> newProperties)
        {
            IEnumerable<string> oldDimensionsNames = GetDimensionsNames(oldProperties);
            IEnumerable<string> newDimensionsNames = GetDimensionsNames(newProperties);

            // If we have same dimensions, no need to reload
            if (oldDimensionsNames.SequenceEqual(newDimensionsNames, StringComparers.ConfigurationDimensionNames))
                return ProjectReloadResult.NoAction;

            // We no longer have same dimensions so we need to reload all configurations by reloading the project.
            // This catches when we switch from [Configuration, Platform] ->  [Configuration, Platform, TargetFramework] or vice versa, 
            return ProjectReloadResult.NeedsForceReload;
        }

        private IEnumerable<string> GetDimensionsNames(ImmutableArray<ProjectPropertyElement> properties)
        {
            // Look through the properties and find all declared dimensions (ie <Configurations>, <Platforms>, <TargetFrameworks>) 
            // and return their dimension name equivalents (Configuration, Platform, TargetFramework)
            return DimensionProviders.Select(v => v.Value)
                                     .OfType<IProjectConfigurationDimensionsProviderInternal>()
                                     .SelectMany(p => p.GetBestGuessDimensionNames(properties));
        }
    }
}
