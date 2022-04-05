// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <remarks>
    /// See also: <see cref="ApplicationManifestKindValueProvider"/>, <see cref="ApplicationManifestPathValueProvider"/>.
    /// </remarks>
    [ExportInterceptingPropertyValueProvider("ApplicationManifest", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class ApplicationManifestValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly UnconfiguredProject _unconfiguredProject;

        private const string NoManifestMSBuildProperty = "NoWin32Manifest";
        private const string ApplicationManifestMSBuildProperty = "ApplicationManifest";
        private const string NoManifestValue = "NoManifest";
        private const string DefaultManifestValue = "DefaultManifest";

        [ImportingConstructor]
        public ApplicationManifestValueProvider(UnconfiguredProject project)
        {
            _unconfiguredProject = project;
        }

        /// <summary>
        /// Gets the application manifest property
        /// </summary>
        /// <remarks>
        /// The Application Manifest's value is one of three possibilities:
        ///     - It's either a path to file that is the manifest
        ///     - It's the value "NoManifest" which means the application doesn't have a manifest.
        ///     - It's the value "DefaultManifest" which means that the application will have a default manifest.
        ///
        /// These three values map to two MSBuild properties - ApplicationManifest (specified if it's a path) or NoWin32Manifest
        /// which is true for the second case and false or non-existent for the third.
        /// </remarks>
        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            if (!string.IsNullOrEmpty(evaluatedPropertyValue))
            {
                return evaluatedPropertyValue;
            }

            string noManifestPropertyValue = await defaultProperties.GetEvaluatedPropertyValueAsync(NoManifestMSBuildProperty);
            if (noManifestPropertyValue?.Equals("true", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                return NoManifestValue;
            }

            // It doesn't matter if it is set to false or the value is not present. We default to "DefaultManifest" scenario.
            return DefaultManifestValue;
        }

        /// <summary>
        /// Sets the application manifest property
        /// </summary>
        public override async Task<string?> OnSetPropertyValueAsync(
            string propertyName,
            string? unevaluatedPropertyValue,
            IProjectProperties defaultProperties,
            IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            string? returnValue = null;

            // We treat NULL/empty value as reset to default and remove the two properties from the project.
            if (Strings.IsNullOrEmpty(unevaluatedPropertyValue) || string.Equals(unevaluatedPropertyValue, DefaultManifestValue, StringComparison.InvariantCultureIgnoreCase))
            {
                await defaultProperties.DeletePropertyAsync(ApplicationManifestMSBuildProperty);
                await defaultProperties.DeletePropertyAsync(NoManifestMSBuildProperty);
            }
            else if (string.Equals(unevaluatedPropertyValue, NoManifestValue, StringComparison.InvariantCultureIgnoreCase))
            {
                await defaultProperties.DeletePropertyAsync(ApplicationManifestMSBuildProperty);
                await defaultProperties.SetPropertyValueAsync(NoManifestMSBuildProperty, "true");
            }
            else
            {
                await defaultProperties.DeletePropertyAsync(NoManifestMSBuildProperty);
                // If we can make the path relative to the project folder do so. Otherwise just use the given path.
                if (Path.IsPathRooted(unevaluatedPropertyValue) &&
                    PathHelper.TryMakeRelativeToProjectDirectory(_unconfiguredProject, unevaluatedPropertyValue, out string? relativePath))
                {
                    returnValue = relativePath;
                }
                else
                {
                    returnValue = unevaluatedPropertyValue;
                }
            }

            return returnValue;
        }
    }
}
