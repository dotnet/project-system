// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Build;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    /// <summary>
    /// Provides 'Platform' project configuration dimension and values.
    /// </summary>
    /// <remarks>
    /// The Order attribute will determine the order of the dimensions inside the configuration
    /// service. We want Configuration|Platform|TargetFramework as the defaults so the values
    /// start at MaxValue and get decremented for each in order for future extenders to fall
    /// below these 3 providers.
    /// </remarks>
    [Export(typeof(IProjectConfigurationDimensionsProvider))]
    [AppliesTo(ProjectCapabilities.ProjectConfigurationsDeclaredDimensions)]
    [Order(DimensionProviderOrder.Platform)]
    internal class PlatformProjectConfigurationDimensionProvider : BaseProjectConfigurationDimensionProvider
    {
        [ImportingConstructor]
        public PlatformProjectConfigurationDimensionProvider(IProjectAccessor projectAccessor)
            : base(projectAccessor, ConfigurationGeneral.PlatformProperty, "Platforms")
        {
        }

        /// <summary>
        /// Modifies the project when there's a platform change.
        /// </summary>
        /// <param name="args">Information about the configuration dimension value change.</param>
        /// <returns>A task for the async operation.</returns>
        public override Task OnDimensionValueChangedAsync(ProjectConfigurationDimensionValueChangedEventArgs args)
        {
            if (StringComparers.ConfigurationDimensionNames.Equals(args.DimensionName, DimensionName))
            {
                if (args.Stage == ChangeEventStage.Before)
                {
                    switch (args.Change)
                    {
                        case ConfigurationDimensionChange.Add:
                            return OnPlatformAddedAsync(args.Project, args.DimensionValue);

                        case ConfigurationDimensionChange.Delete:
                            return OnPlatformDeletedAsync(args.Project, args.DimensionValue);

                        case ConfigurationDimensionChange.Rename:
                            // Platform doesn't currently supports rename, this should never be called.
                            break;
                    }
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds a platform to the project.
        /// </summary>
        /// <param name="project">Unconfigured project for which the configuration change.</param>
        /// <param name="platformName">Name of the new platform.</param>
        /// <returns>A task for the async operation.</returns>
        private async Task OnPlatformAddedAsync(UnconfiguredProject project, string platformName)
        {
            string evaluatedPropertyValue = await GetPropertyValue(project).ConfigureAwait(false);
            await ProjectAccessor.OpenProjectXmlForWriteAsync(project, msbuildProject =>
            {
                BuildUtilities.AppendPropertyValue(msbuildProject, evaluatedPropertyValue, PropertyName, platformName);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes a platform from the project.
        /// </summary>
        /// <param name="project">Unconfigured project for which the configuration change.</param>
        /// <param name="platformName">Name of the deleted platform.</param>
        /// <returns>A task for the async operation.</returns>
        private async Task OnPlatformDeletedAsync(UnconfiguredProject project, string platformName)
        {
            string evaluatedPropertyValue = await GetPropertyValue(project).ConfigureAwait(false);
            await ProjectAccessor.OpenProjectXmlForWriteAsync(project, msbuildProject =>
            {
                BuildUtilities.RemovePropertyValue(msbuildProject, evaluatedPropertyValue, PropertyName, platformName);
            }).ConfigureAwait(false);
        }
    }
}
