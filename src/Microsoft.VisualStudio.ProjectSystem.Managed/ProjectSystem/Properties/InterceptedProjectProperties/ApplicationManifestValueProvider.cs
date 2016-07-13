// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System;
using System.IO;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("ApplicationManifest")]
    internal sealed class ApplicationManifestValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IProjectTreeService _projectTreeService;

        private const string NoManifestMSBuildProperty = "NoWin32Manifest";
        private const string ApplicationManifestMSBuildProperty = "ApplicationManifest";
        private const string NoManifestValue = "NoManifest";
        private const string DefaultManifestValue = "DefaultManifest";

        [ImportingConstructor]
        public ApplicationManifestValueProvider(UnconfiguredProject unconfiguredProject, IProjectTreeService projectTreeService)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
            Requires.NotNull(projectTreeService, nameof(projectTreeService));

            _unconfiguredProject = unconfiguredProject;
            _projectTreeService = projectTreeService;
        }

        /// <summary>
        /// Gets the application manifest property
        /// </summary>
        /// <remarks>
        /// The Application Manifest's value is one of three possibilites:
        ///     - It's either a path to file that is the manifest
        ///     - It's the value "NoManifest" which means the application doesn't have a manifest.
        ///     - It's the value "DefaultManifest" which means that the application will have a default manifest.
        ///     
        /// These three values map to two MSBuild properties - ApplicationManifest (specified if it's a path) or NoWin32Manfiest 
        /// which is true for the second case and false or non-existent for the third.
        /// </remarks>
        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            if (!string.IsNullOrEmpty(evaluatedPropertyValue))
            {
                return evaluatedPropertyValue;
            }

            string noManifestPropertyValue = await defaultProperties.GetEvaluatedPropertyValueAsync(NoManifestMSBuildProperty).ConfigureAwait(false);
            if (noManifestPropertyValue?.Equals("true", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                return NoManifestValue;
            }

            // It doesnt matter if it is set to false or the value is not present. We default to "DefaultManifest" scenario.
            return DefaultManifestValue;
        }

        /// <summary>
        /// Sets the application manifest property
        /// </summary>
        public override async Task<string> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            string returnValue = null;

            // We treat NULL/empty value as reset to default and remove the two properties from the project.
            if (string.IsNullOrEmpty(unevaluatedPropertyValue) || string.Equals(unevaluatedPropertyValue, DefaultManifestValue, StringComparison.InvariantCultureIgnoreCase))
            {
                await defaultProperties.DeletePropertyAsync(ApplicationManifestMSBuildProperty).ConfigureAwait(false);
                await defaultProperties.DeletePropertyAsync(NoManifestMSBuildProperty).ConfigureAwait(false);
            }
            else if (string.Equals(unevaluatedPropertyValue, NoManifestValue, StringComparison.InvariantCultureIgnoreCase))
            {
                await defaultProperties.DeletePropertyAsync(ApplicationManifestMSBuildProperty).ConfigureAwait(false);
                await defaultProperties.SetPropertyValueAsync(NoManifestMSBuildProperty, "true").ConfigureAwait(false);
            }
            else
            {
                await defaultProperties.DeletePropertyAsync(NoManifestMSBuildProperty).ConfigureAwait(false);

                // If we can make the path relative to the project folder do so. Otherwise just use the given path.
                string relativePath;
                if (Path.IsPathRooted(unevaluatedPropertyValue) &&
                    PathHelper.TryMakeRelativeToProjectDirectory(_unconfiguredProject, unevaluatedPropertyValue, out relativePath))
                {
                    returnValue = relativePath;
                }
                else
                {
                    returnValue = unevaluatedPropertyValue;
                }
            }

            // Push the changes so that they take effect immediately, since the property pages try to read the value right after the set.
            await _projectTreeService.PublishLatestTreeAsync().ConfigureAwait(false);

            return returnValue;
        }
    }
}
