// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    /// <summary>
    ///     Attempts to calculate the default configuration of a project without evaluating or loading any unneeded configurations.
    /// </summary>
    [Export(typeof(IProjectDefaultConfigurationProvider))]
    [AppliesTo(ProjectCapabilities.ProjectConfigurationsDeclaredDimensions)]
    [Order(Order.Default)]
    internal class ProjectDefaultConfigurationProvider : IProjectDefaultConfigurationProvider
    {
        private readonly UnconfiguredProject _project;
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public ProjectDefaultConfigurationProvider(UnconfiguredProject project, IProjectThreadingService threadingService)
        {
            _project = project;
            _threadingService = threadingService;

            DimensionsProviders = new OrderPrecedenceImportCollection<IProjectConfigurationDimensionsProvider>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IProjectConfigurationDimensionsProvider> DimensionsProviders
        {
            get;
        }

        public ProjectConfiguration GetDefaultConfiguration(IMinimalProjectConfiguration projectConfiguration)
        {
            return _threadingService.ExecuteSynchronously(() => GetDefaultConfigurationAsync(projectConfiguration));
        }

        private async Task<ProjectConfiguration> GetDefaultConfigurationAsync(IMinimalProjectConfiguration projectConfiguration)
        {
            IEnumerable<KeyValuePair<string, string>> defaultValues = await GetDefaultValuesForDimensionsAsync().ConfigureAwait(false);

            string name = string.Join("|", defaultValues.Select(defaultValue => defaultValue.Value));
            IImmutableDictionary<string, string> dimensions = ConvertDefaultValuesToDimensionsMap(defaultValues, projectConfiguration);

            return new StandardProjectConfiguration(name, dimensions);
        }

        private async Task<IEnumerable<KeyValuePair<string, string>>> GetDefaultValuesForDimensionsAsync()
        {
            // Walk through each dimension provider and get them provide their best guess of 
            // the defaults of their dimensions without actually evaluating the project
            
            var allDefaultValues = new List<KeyValuePair<string, string>>();

            IEnumerable<IProjectConfigurationDimensionsProvider3> providers = DimensionsProviders.Select(p => p.Value)
                                                                                                 .OfType<IProjectConfigurationDimensionsProvider3>();

            foreach (IProjectConfigurationDimensionsProvider3 provider in providers)
            {
                IEnumerable<KeyValuePair<string, string>> defaultValues = await provider.GetBestGuessDefaultValuesForDimensionsAsync(_project)
                                                                                        .ConfigureAwait(false);

                allDefaultValues.AddRange(defaultValues);
            }

            return allDefaultValues;
        }

        private static IImmutableDictionary<string, string> ConvertDefaultValuesToDimensionsMap(IEnumerable<KeyValuePair<string, string>> defaultValues, IMinimalProjectConfiguration projectConfiguration)
        {
            ImmutableDictionary<string, string> dimensions = Empty.PropertiesMap;

            foreach (KeyValuePair<string, string> defaultValue in defaultValues)
            {
                dimensions = dimensions.SetItem(defaultValue.Key, defaultValue.Value);
            }

            if (projectConfiguration != null)
            {   // Solution provided a default configuration based on what it was when the solution closed, use its
                // version of the configuration/platform which is likely more accurate than our providers' defaults

                dimensions = dimensions.SetItem(ConfigurationGeneral.ConfigurationProperty, projectConfiguration.Configuration)
                                       .SetItem(ConfigurationGeneral.PlatformProperty, projectConfiguration.Platform);
            }

            return dimensions;
        }
    }
}
