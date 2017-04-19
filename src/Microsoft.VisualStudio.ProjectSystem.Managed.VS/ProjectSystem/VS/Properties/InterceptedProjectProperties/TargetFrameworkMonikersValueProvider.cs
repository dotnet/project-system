// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using static Microsoft.VisualStudio.ProjectSystem.Build.TargetFrameworkProjectConfigurationDimensionProvider;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    [ExportInterceptingPropertyValueProvider("TargetFrameworkMonikers", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class TargetFrameworkMonikersValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly ActiveConfiguredProjectsProvider _projectProvider;
        private readonly ProjectProperties _properties;

        [ImportingConstructor]
        public TargetFrameworkMonikersValueProvider(ActiveConfiguredProjectsProvider projectProvider, ProjectProperties properties)
        {
            _projectProvider = projectProvider;
            _properties = properties;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            var activeProjectConfigurations = await _projectProvider.GetActiveProjectConfigurationsAsync().ConfigureAwait(false);
            var isCrossTarging = activeProjectConfigurations.All(c => c.IsCrossTargeting());
            if (isCrossTarging)
            {
                var builder = ImmutableArray.CreateBuilder<string>();
                foreach (var activeProjectConfiguration in activeProjectConfigurations)
                {
                    if (activeProjectConfiguration.Dimensions.TryGetValue(TargetFrameworkPropertyName, out var tfm))
                    {
                        builder.Add(tfm);
                    }
                }

                builder.Sort();
                return string.Join(";", builder.ToArray());
            }
            else
            {
                var configuration = await _properties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(true);
                var currentTargetFrameworkMoniker = (string)await configuration.TargetFrameworkMoniker.GetValueAsync().ConfigureAwait(false);
                return currentTargetFrameworkMoniker;
            }
        }
    }
}
