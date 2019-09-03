// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

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
        private bool? _overrideGeneratePackageOnBuild;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetFrameworkGlobalBuildPropertyProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        internal GeneratePackageOnBuildPropertyProvider(IProjectService projectService)
            : base(projectService.Services)
        {
        }

        /// <summary>
        /// Overrides the value of GeneratePackageOnBuild to the value specified, or resets to the project property value if <c>null</c> is passed in.
        /// </summary>
        public void OverrideGeneratePackageOnBuild(bool? value)
        {
            _overrideGeneratePackageOnBuild = value;
        }

        /// <summary>
        /// Gets the set of global properties that should apply to the project(s) in this scope.
        /// </summary>
        /// <value>A map whose keys are case insensitive.  Never null, but may be empty.</value>
        public override Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
        {
            IImmutableDictionary<string, string> properties = Empty.PropertiesMap;

            if (_overrideGeneratePackageOnBuild.HasValue)
            {
                properties = properties.Add(ConfigurationGeneralBrowseObject.GeneratePackageOnBuildProperty, _overrideGeneratePackageOnBuild.Value ? "true" : "false");
            }

            return Task.FromResult(properties);
        }
    }
}
