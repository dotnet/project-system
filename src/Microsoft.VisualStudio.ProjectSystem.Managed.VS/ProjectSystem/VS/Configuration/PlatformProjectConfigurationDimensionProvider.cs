// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;
using Microsoft.VisualStudio.ProjectSystem.VS.Telemetry;

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
        public PlatformProjectConfigurationDimensionProvider(IProjectXmlAccessor projectXmlAccessor, ITelemetryService telemetryService)
            : base(projectXmlAccessor, telemetryService, ConfigurationGeneral.PlatformProperty, "Platforms", valueContainsPii: false)
        {
        }

        /// <summary>
        /// Modifies the project when there's a platform change.
        /// </summary>
        /// <param name="args">Information about the configuration dimension value change.</param>
        /// <returns>A task for the async operation.</returns>
        public override async Task OnDimensionValueChangedAsync(ProjectConfigurationDimensionValueChangedEventArgs args)
        {
            if (string.Compare(args.DimensionName, _dimensionName, StringComparison.Ordinal) == 0)
            {
                if (args.Stage == ChangeEventStage.Before)
                {
                    switch (args.Change)
                    {
                        case ConfigurationDimensionChange.Add:
                            await OnDimensionValueAddedAsync(args.Project, args.DimensionValue).ConfigureAwait(false);
                            break;
                        case ConfigurationDimensionChange.Delete:
                            await OnDimensionValueRemovedAsync(args.Project, args.DimensionValue).ConfigureAwait(false);
                            break;
                        case ConfigurationDimensionChange.Rename:
                            // Platform doesn't currently supports rename, this should never be called.
                            break;
                    }
                }
            }
        }
    }
}
