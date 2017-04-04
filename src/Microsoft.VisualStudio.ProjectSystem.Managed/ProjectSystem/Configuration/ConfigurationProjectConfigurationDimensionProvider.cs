// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Build;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Configuration
{
    /// <summary>
    /// Provides 'Configuration' project configuration dimension and values.
    /// </summary>
    /// <remarks>
    /// The Order attribute will determine the order of the dimensions inside the configuration
    /// service. We want Configuration|Platform|TargetFramework as the defaults so the values
    /// start at MaxValue and get decremented for each in order for future extenders to fall
    /// below these 3 providers.
    /// </remarks>
    [Export(typeof(IProjectConfigurationDimensionsProvider))]
    [AppliesTo(ProjectCapabilities.ProjectConfigurationsDeclaredDimensions)]
    [Order(int.MaxValue)]
    internal class ConfigurationProjectConfigurationDimensionProvider : BaseProjectConfigurationDimensionProvider
    {
        [ImportingConstructor]
        public ConfigurationProjectConfigurationDimensionProvider(IProjectXmlAccessor projectXmlAccessor)
            : base(projectXmlAccessor, ConfigurationGeneral.ConfigurationProperty, "Configurations")
        {
        }

        /// <summary>
        /// Modifies the project when there's a configuration change.
        /// </summary>
        /// <param name="args">Information about the configuration dimension value change.</param>
        /// <returns>A task for the async operation.</returns>
        public override async Task OnDimensionValueChangedAsync(ProjectConfigurationDimensionValueChangedEventArgs args)
        {
            if (StringComparers.ConfigurationDimensionNames.Equals(args.DimensionName, DimensionName))
            {
                if (args.Stage == ChangeEventStage.Before)
                {
                    switch (args.Change)
                    {
                        case ConfigurationDimensionChange.Add:
                            await OnConfigurationAddedAsync(args.Project, args.DimensionValue).ConfigureAwait(true);
                            break;
                        case ConfigurationDimensionChange.Delete:
                            await OnConfigurationRemovedAsync(args.Project, args.DimensionValue).ConfigureAwait(true);
                            break;
                        case ConfigurationDimensionChange.Rename:
                            // Need to wait until the core rename changes happen before renaming the property.
                            break;
                    }
                }
                else if (args.Stage == ChangeEventStage.After)
                {
                    // Only change that needs to be handled here is renaming configurations which needs to happen after all
                    // of the core changes to rename existing conditions have executed.
                    if (args.Change == ConfigurationDimensionChange.Rename)
                    {
                        await OnConfigurationRenamedAsync(args.Project, args.OldDimensionValue, args.DimensionValue).ConfigureAwait(true);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a configuration to the project.
        /// </summary>
        /// <param name="unconfiguredProject">Unconfigured project for which the configuration change.</param>
        /// <param name="configurationName">Name of the new configuration.</param>
        /// <returns>A task for the async operation.</returns>
        private async Task OnConfigurationAddedAsync(UnconfiguredProject unconfiguredProject, string configurationName)
        {
            string evaluatedPropertyValue = await GetPropertyValue(unconfiguredProject).ConfigureAwait(false);
            await ProjectXmlAccessor.ExecuteInWriteLock(msbuildProject =>
            {
                BuildUtilities.AppendPropertyValue(msbuildProject, evaluatedPropertyValue, PropertyName, configurationName);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes a configuration from the project.
        /// </summary>
        /// <param name="unconfiguredProject">Unconfigured project for which the configuration change.</param>
        /// <param name="configurationName">Name of the deleted configuration.</param>
        /// <returns>A task for the async operation.</returns>
        private async Task OnConfigurationRemovedAsync(UnconfiguredProject unconfiguredProject, string configurationName)
        {
            string evaluatedPropertyValue = await GetPropertyValue(unconfiguredProject).ConfigureAwait(false);
            await ProjectXmlAccessor.ExecuteInWriteLock(msbuildProject =>
            {
                BuildUtilities.RemovePropertyValue(msbuildProject, evaluatedPropertyValue, PropertyName, configurationName);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Renames an existing configuration in the project.
        /// </summary>
        /// <param name="unconfiguredProject">Unconfigured project for which the configuration change.</param>
        /// <param name="oldName">Original name of the configuration.</param>
        /// <param name="newName">New name of the configuration.</param>
        /// <returns>A task for the async operation.</returns>
        private async Task OnConfigurationRenamedAsync(UnconfiguredProject unconfiguredProject, string oldName, string newName)
        {
            string evaluatedPropertyValue = await GetPropertyValue(unconfiguredProject).ConfigureAwait(false);
            await ProjectXmlAccessor.ExecuteInWriteLock(msbuildProject =>
            {
                BuildUtilities.RenamePropertyValue(msbuildProject, evaluatedPropertyValue, PropertyName, oldName, newName);
            }).ConfigureAwait(false);
        }
    }
}
