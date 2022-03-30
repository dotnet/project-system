// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    /// <summary>
    /// Build properties provider for cross targeting projects to ensure that they are build for all target frameworks when doing an explicit build, not just the active target framework.
    /// </summary>
    /// <remarks>Passes <see cref="Order.Default"/> to <see cref="OrderAttribute"/> as that will guarantee it has a higher priority than providers from CPS.</remarks>
    [ExportBuildGlobalPropertiesProvider(designTimeBuildProperties: false)]
    [AppliesTo(ProjectCapability.DotNet)]
    [Order(Order.Default)]
    internal class TargetFrameworkGlobalBuildPropertyProvider : StaticGlobalPropertiesProviderBase
    {
        private readonly ConfiguredProject _configuredProject;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetFrameworkGlobalBuildPropertyProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        internal TargetFrameworkGlobalBuildPropertyProvider(IProjectService projectService, ConfiguredProject configuredProject)
            : base(projectService.Services)
        {
            _configuredProject = configuredProject;
        }

        /// <summary>
        /// Gets the set of global properties that should apply to the project(s) in this scope.
        /// </summary>
        /// <value>A map whose keys are case insensitive.  Never null, but may be empty.</value>
        public override Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
        {
            IImmutableDictionary<string, string> properties = Empty.PropertiesMap;

            // Check if this is a cross targeting project, i.e. project configuration has a "TargetFramework" dimension.
            if (_configuredProject.ProjectConfiguration.IsCrossTargeting())
            {
                // For a cross targeting project, we want to build for all the targeted frameworks.
                // Clear out the TargetFramework property from the configuration.
                properties = properties.Add(ConfigurationGeneral.TargetFrameworkProperty, string.Empty);
            }

            return Task.FromResult(properties);
        }
    }
}
