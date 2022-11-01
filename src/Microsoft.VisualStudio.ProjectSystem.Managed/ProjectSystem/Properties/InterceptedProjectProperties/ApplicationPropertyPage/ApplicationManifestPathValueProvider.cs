// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Supports setting the path to a custom manifest file. Ultimately this reads and writes the
    /// <c>ApplicationManifest</c> MSBuild property. It also handles saving the manifest path as
    /// a relative path, if possible.
    /// </summary>
    /// <remarks>
    /// This type, along with <see cref="ApplicationManifestPathValueProvider"/>, provide the same
    /// functionality as <see cref="ApplicationManifestValueProvider"/> but in a different context. That
    /// provider is currently used by the legacy property pages and the VS property APIs; these are
    /// designed to be used by the new property pages.
    /// </remarks>
    [ExportInterceptingPropertyValueProvider("ApplicationManifestPath", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class ApplicationManifestPathValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly UnconfiguredProject _unconfiguredProject;

        private const string ApplicationManifestMSBuildProperty = "ApplicationManifest";
        private static readonly string[] s_msBuildPropertyNames = { ApplicationManifestMSBuildProperty };
        
        [ImportingConstructor]
        public ApplicationManifestPathValueProvider(UnconfiguredProject project)
        {
            _unconfiguredProject = project;
        }

        public override Task<bool> IsValueDefinedInContextAsync(string propertyName, IProjectProperties defaultProperties)
        {
            return IsValueDefinedInContextMSBuildPropertiesAsync(defaultProperties, s_msBuildPropertyNames);
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return defaultProperties.GetEvaluatedPropertyValueAsync(ApplicationManifestMSBuildProperty);
        }

        public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return await defaultProperties.GetUnevaluatedPropertyValueAsync(ApplicationManifestMSBuildProperty)
                ?? string.Empty;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            // If we can make the path relative to the project folder do so. Otherwise just use the given path.
            if (Path.IsPathRooted(unevaluatedPropertyValue) &&
                PathHelper.TryMakeRelativeToProjectDirectory(_unconfiguredProject, unevaluatedPropertyValue, out string? relativePath))
            {
                unevaluatedPropertyValue = relativePath;
            }

            await defaultProperties.SetPropertyValueAsync(ApplicationManifestMSBuildProperty, unevaluatedPropertyValue);

            return null;
        }
    }
}
