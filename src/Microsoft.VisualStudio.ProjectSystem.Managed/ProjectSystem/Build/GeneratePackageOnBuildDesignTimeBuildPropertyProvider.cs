// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    /// <summary>
    /// Design time build property provider for <see cref="ConfigurationGeneralBrowseObject.GeneratePackageOnBuildProperty"/>.
    /// </summary>
    [ExportBuildGlobalPropertiesProvider(designTimeBuildProperties: true)]
    [AppliesTo(ProjectCapability.Pack)]
    internal class GeneratePackageOnBuildDesignTimeBuildPropertyProvider : StaticGlobalPropertiesProviderBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TargetFrameworkGlobalBuildPropertyProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        internal GeneratePackageOnBuildDesignTimeBuildPropertyProvider(IProjectService projectService)
            : base(projectService.Services)
        {
        }

        /// <summary>
        /// Gets the set of global properties that should apply to the project(s) in this scope.
        /// </summary>
        /// <value>A map whose keys are case insensitive.  Never null, but may be empty.</value>
        public override Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
        {
            // Never generate nuget package during design time build.
            IImmutableDictionary<string, string> properties = Empty.PropertiesMap.Add(ConfigurationGeneralBrowseObject.GeneratePackageOnBuildProperty, "false");
            return Task.FromResult(properties);
        }
    }
}