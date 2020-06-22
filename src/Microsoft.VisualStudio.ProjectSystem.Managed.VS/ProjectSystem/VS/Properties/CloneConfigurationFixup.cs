// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    /// <summary>
    /// Interface for components that provide post-configuration cloning propertyName fix ups.
    /// </summary>
    internal class CloneConfigurationFixup : IClonePlatformFixup
    {
        private string _fromConfigurationName;
        private IEnumerable<string> _propertyNames = null;
        private List<string> _configurationProperties;
        private readonly IUnconfiguredProjectServices _unconfiguredProjectServices;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IProjectLockService _projectLockService;
        private readonly ProjectConfiguration _fromProjectConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloneConfigurationFixup"/> class.
        /// </summary>
        public CloneConfigurationFixup(ProjectConfiguration fromProjectConfiguration,
            string newConfigurationName,
            IUnconfiguredProjectServices unconfiguredProjectServices,
            UnconfiguredProject unconfiguredProject,
            IProjectLockService projectLockService)
        {
            _fromProjectConfiguration = fromProjectConfiguration;
            _fromConfigurationName = newConfigurationName;

            _unconfiguredProjectServices = unconfiguredProjectServices;
            _unconfiguredProject = unconfiguredProject;
            _projectLockService = projectLockService;

        }

        /// <summary>
        /// Gets the list of property names that factor on Configuration builds.
        /// </summary>
        private async Task<IEnumerable<string>> GetConfigurationPropertiesNamesToClone()
        {
            if (_propertyNames != null)
            {
                return _propertyNames;
            }

            IProjectProperties? commonProperties =
                _unconfiguredProjectServices.ActiveConfiguredProjectProvider?.ActiveConfiguredProject?.Services.ProjectPropertiesProvider?.GetCommonProperties();
            string value = await commonProperties.GetEvaluatedPropertyValueAsync("CopyingConfigurationProperties");

            _propertyNames = value.Split(';').Distinct().Where(s => !string.IsNullOrEmpty(s));

            return _propertyNames;
        }

        /// <summary>
        /// Gets a value indicating whether this propertyName factors or not on configuration build
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        private async Task<bool> isInConfigurationDefaultsAsync(string propertyName)
        {
            _configurationProperties ??= (await GetConfigurationPropertiesNamesToClone()).ToList();
            return _configurationProperties.Contains(propertyName);
        }

        /// <summary>
        /// Read the value for propertyName via Targets
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns>Default propertyName value</returns>
        private async Task<string> GetDefaultPropertyValueAsync(string propertyName)
        {
            string propertyValue = string.Empty;
            try
            {
                await _projectLockService.ReadLockAsync(
                    async access =>
                    {
                        // Read values from Sdk
                        var configuredProject =
                            await _unconfiguredProject.LoadConfiguredProjectAsync(_fromProjectConfiguration);

                        Project project = await access.GetProjectAsync(configuredProject);
                        propertyValue = project.GetPropertyValue(propertyName);

                    }).ConfigureAwaitRunInline();
            }
            catch (Exception e)
            {
            }
            return propertyValue;
        }


        public bool ShouldElementBeCloned(ProjectPropertyElement propertyElement, ref string? alternativeValue)
        {
            bool shouldClone = false;
            if (isInConfigurationDefaultsAsync(propertyElement.Name).GetAwaiter().GetResult())
            {
                alternativeValue = GetDefaultPropertyValueAsync(propertyElement.Name).GetAwaiter().GetResult();
                shouldClone = true;
            }

            return shouldClone;
        }


        public bool ShouldElementBeCloned(ProjectMetadataElement metadataElement, ref string? alternativeValue)
        {
            bool shouldClone = false;
            if (isInConfigurationDefaultsAsync(metadataElement.Name).GetAwaiter().GetResult())
            {
                alternativeValue = GetDefaultPropertyValueAsync(metadataElement.Name).GetAwaiter().GetResult();
                shouldClone = true;
            }

            return shouldClone;
        }
    }
}
