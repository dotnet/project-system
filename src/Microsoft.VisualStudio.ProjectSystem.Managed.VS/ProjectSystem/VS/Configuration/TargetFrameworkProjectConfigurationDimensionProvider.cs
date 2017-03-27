// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Build;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Configuration
{
    /// <summary>
    /// Provides "TargetFramework" project configuration dimension and values.
    /// </summary>
    /// <remarks>
    /// The Order attribute will determine the order of the dimensions inside the configuration
    /// service. We want Configuration|Platform|TargetFramework as the defaults so the values
    /// start at MaxValue and get decremented for each in order for future extenders to fall
    /// below these 3 providers.
    /// </remarks>
    [Export(typeof(IProjectConfigurationDimensionsProvider))]
    [AppliesTo(ProjectCapabilities.ProjectConfigurationsDeclaredDimensions)]
    [Order(int.MaxValue -2)]
    internal class TargetFrameworkProjectConfigurationDimensionProvider : BaseProjectConfigurationDimensionProvider
    {
        [ImportingConstructor]
        public TargetFrameworkProjectConfigurationDimensionProvider(IProjectXmlAccessor projectXmlAccessor)
            : base(projectXmlAccessor, ConfigurationGeneral.TargetFrameworkProperty, ConfigurationGeneral.TargetFrameworksProperty)
        {
        }

        protected override async Task<ImmutableArray<string>> GetOrderedPropertyValuesAsync(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            string targetFrameworkProperty = await _projectXmlAccessor.GetEvaluatedPropertyValue(project, ConfigurationGeneral.TargetFrameworkProperty).ConfigureAwait(true);
            if (targetFrameworkProperty != null)
            {
                // If the project already defines a specific "TargetFramework" to target, then this is not a cross-targeting project and we don't need a target framework dimension.
                return ImmutableArray<string>.Empty;
            }
            else
            {
                string targetFrameworksProperty = await _projectXmlAccessor.GetEvaluatedPropertyValue(project, ConfigurationGeneral.TargetFrameworksProperty).ConfigureAwait(true);
                return BuildUtilities.GetPropertyValues(targetFrameworksProperty);
            }
        }

        public override Task OnDimensionValueChangedAsync(ProjectConfigurationDimensionValueChangedEventArgs args)
        {
            // CPS cannot change TargetFramework currently so don't do anything for the dimension
            return Task.CompletedTask;
        }
    }
}
