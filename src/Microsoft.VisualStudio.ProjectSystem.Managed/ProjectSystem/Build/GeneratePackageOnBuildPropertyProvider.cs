// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    /// <summary>
    /// Build property provider for <see cref="ConfigurationGeneralBrowseObject.GeneratePackageOnBuildProperty"/> for solution build.
    /// </summary>
    [ExportBuildGlobalPropertiesProvider(designTimeBuildProperties: false)]
    [Export(typeof(GeneratePackageOnBuildPropertyProvider))]
    [AppliesTo(ProjectCapability.Pack)]
    internal class GeneratePackageOnBuildPropertyProvider : StaticGlobalPropertiesProviderBase
    {
        private Task<IImmutableDictionary<string, string>> _properties = Task.FromResult<IImmutableDictionary<string, string>>(Empty.PropertiesMap);

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetFrameworkGlobalBuildPropertyProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        internal GeneratePackageOnBuildPropertyProvider(IProjectService projectService)
            : base(projectService.Services)
        {
        }

        /// <summary>
        /// Overrides the value of GeneratePackageOnBuild to the value specified, or resets to the project property value if <see langword="null"/> is passed in.
        /// </summary>
        public void OverrideGeneratePackageOnBuild(bool? value)
        {
            _properties = Task.FromResult<IImmutableDictionary<string, string>>(
                value switch
                {
                    null => Empty.PropertiesMap,
                    true => Empty.PropertiesMap.Add(ConfigurationGeneralBrowseObject.GeneratePackageOnBuildProperty, "true"),
                    false => Empty.PropertiesMap.Add(ConfigurationGeneralBrowseObject.GeneratePackageOnBuildProperty, "false")
                });
        }

        /// <summary>
        /// Gets the set of global properties that should apply to the project(s) in this scope.
        /// </summary>
        /// <value>A map whose keys are case insensitive.  Never null, but may be empty.</value>
        public override Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
        {
            return _properties;
        }
    }
}
