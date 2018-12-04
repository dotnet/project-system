// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
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

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            ActiveConfiguredObjects<ConfiguredProject> configuredProjects = await _projectProvider.GetActiveConfiguredProjectsAsync();
            var builder = PooledArray<string>.GetInstance();
            foreach (ConfiguredProject configuredProject in configuredProjects.Objects)
            {
                ProjectProperties projectProperties = configuredProject.Services.ExportProvider.GetExportedValue<ProjectProperties>();
                ConfigurationGeneral configuration = await projectProperties.GetConfigurationGeneralPropertiesAsync();
                string currentTargetFrameworkMoniker = (string)await configuration.TargetFrameworkMoniker.GetValueAsync();
                builder.Add(currentTargetFrameworkMoniker);
            }

            return string.Join(";", builder.ToArrayAndFree());
        }
    }
}
