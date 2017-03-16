// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Configuration
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
    [Order(int.MaxValue -1)]
    internal class PlatformProjectConfigurationDimensionProvider : BaseProjectConfigurationDimensionProvider
    {
        [ImportingConstructor]
        public PlatformProjectConfigurationDimensionProvider(IProjectXmlAccessor projectXmlAccessor)
            : base(projectXmlAccessor, ConfigurationGeneral.PlatformProperty, "Platforms")
        {
        }

        /// <summary>
        /// Modifies the project when there's a configuration change.
        /// </summary>
        /// <param name="args">Information about the configuration dimension value change.</param>
        /// <returns>A task for the async operation.</returns>
        public override async Task OnDimensionValueChangedAsync(ProjectConfigurationDimensionValueChangedEventArgs args)
        {
            if (args.DimensionName == _dimensionName)
            {
                if (args.Stage == ChangeEventStage.Before)
                {
                    switch (args.Change)
                    {
                        case ConfigurationDimensionChange.Add:
                            await OnPlatformAdded(args.Project, args.DimensionValue).ConfigureAwait(false);
                            break;
                        case ConfigurationDimensionChange.Delete:
                            await OnPlatformDeleted(args.Project, args.DimensionValue).ConfigureAwait(false);
                            break;
                        case ConfigurationDimensionChange.Rename:
                            // Platform doesn't currently supports rename, this should never be called.
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a platform to the project.
        /// </summary>
        /// <param name="unconfiguredProject">Unconfigured project for which the configuration change.</param>
        /// <param name="platformName">Name of the new platform.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        private async Task OnPlatformAdded(UnconfiguredProject unconfiguredProject, string platformName)
        {
            string evaluatedPropertyValue = await GetPropertyValue(unconfiguredProject).ConfigureAwait(false);
            await _projectXmlAccessor.ExecuteInWriteLock(msbuildProject =>
            {
                MsBuildUtilities.AppendPropertyValue(msbuildProject, evaluatedPropertyValue, _propertyName, platformName);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes a platform from the project.
        /// </summary>
        /// <param name="unconfiguredProject">Unconfigured project for which the configuration change.</param>
        /// <param name="platformName">Name of the deleted platform.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        private async Task OnPlatformDeleted(UnconfiguredProject unconfiguredProject, string platformName)
        {
            string evaluatedPropertyValue = await GetPropertyValue(unconfiguredProject).ConfigureAwait(false);
            await _projectXmlAccessor.ExecuteInWriteLock(msbuildProject =>
            {
                MsBuildUtilities.RemovePropertyValue(msbuildProject, evaluatedPropertyValue, _propertyName, platformName);
            }).ConfigureAwait(false);
        }
    }
}
