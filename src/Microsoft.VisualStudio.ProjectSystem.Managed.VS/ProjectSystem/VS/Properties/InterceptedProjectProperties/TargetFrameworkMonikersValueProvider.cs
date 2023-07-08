// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Buffers.PooledObjects;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    [ExportInterceptingPropertyValueProvider("TargetFrameworkMonikers", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class TargetFrameworkMonikersValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly IActiveConfiguredProjectsProvider _projectProvider;

        [ImportingConstructor]
        public TargetFrameworkMonikersValueProvider(IActiveConfiguredProjectsProvider projectProvider)
        {
            _projectProvider = projectProvider;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            ActiveConfiguredObjects<ConfiguredProject>? configuredProjects = await _projectProvider.GetActiveConfiguredProjectsAsync();

            if (configuredProjects is null)
            {
                return "";
            }

            var builder = PooledArray<string>.GetInstance(capacity: configuredProjects.Objects.Length);

            foreach (ConfiguredProject configuredProject in configuredProjects.Objects)
            {
                ProjectProperties projectProperties = configuredProject.Services.ExportProvider.GetExportedValue<ProjectProperties>();
                ConfigurationGeneral configuration = await projectProperties.GetConfigurationGeneralPropertiesAsync();
                string? currentTargetFrameworkMoniker = (string?)await configuration.TargetFrameworkMoniker.GetValueAsync();
                Assumes.NotNull(currentTargetFrameworkMoniker);
                builder.Add(currentTargetFrameworkMoniker);
            }

            return string.Join(";", builder.ToArrayAndFree());
        }
    }
}
