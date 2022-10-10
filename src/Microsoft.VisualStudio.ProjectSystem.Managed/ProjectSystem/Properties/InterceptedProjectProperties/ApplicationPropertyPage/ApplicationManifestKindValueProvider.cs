// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Supports choosing between no application manifest, a default manifest, and a custom manifest.
    /// Adjusts the MSBuild properties <c>NoWin32Manifest</c> and <c>ApplicationManifest</c> as
    /// appropriate. In the case of a custom manifest, the path to the manifest file is handled by the
    /// <see cref="ApplicationManifestPathValueProvider"/>.
    /// </summary>
    /// <remarks>
    /// This type, along with <see cref="ApplicationManifestPathValueProvider"/>, provide the same
    /// functionality as <see cref="ApplicationManifestValueProvider"/> but in a different context. That
    /// provider is currently used by the legacy property pages and the VS property APIs; these are
    /// designed to be used by the new property pages.
    /// </remarks>
    [ExportInterceptingPropertyValueProvider(ApplicationManifestKindProperty, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class ApplicationManifestKindValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string ApplicationManifestKindProperty = "ApplicationManifestKind";
        private const string NoManifestMSBuildProperty = "NoWin32Manifest";
        private const string ApplicationManifestMSBuildProperty = "ApplicationManifest";
        private const string NoManifestValue = "NoManifest";
        private const string DefaultManifestValue = "DefaultManifest";
        private const string CustomManifestValue = "CustomManifest";
        private readonly ITemporaryPropertyStorage _temporaryPropertyStorage;

        private static readonly string[] s_msBuildPropertyNames = { NoManifestMSBuildProperty, ApplicationManifestMSBuildProperty };
        
        [ImportingConstructor]
        public ApplicationManifestKindValueProvider(ITemporaryPropertyStorage temporaryPropertyStorage)
        {
            _temporaryPropertyStorage = temporaryPropertyStorage;
        }

        public override Task<bool> IsValueDefinedInContextAsync(string propertyName, IProjectProperties defaultProperties)
        {
            return IsValueDefinedInContextMSBuildPropertiesAsync(defaultProperties, s_msBuildPropertyNames);
        }

        /// <summary>
        /// Gets the application manifest kind property
        /// </summary>
        /// <remarks>
        /// The Application Manifest kind's value is one of three possibilities:
        ///     - It's the value "CustomManifest" which means the user will supply the path to a custom manifest file.
        ///     - It's the value "NoManifest" which means the application doesn't have a manifest.
        ///     - It's the value "DefaultManifest" which means that the application will have a default manifest.
        ///
        /// These three values map to two MSBuild properties - ApplicationManifest (for the first case) or NoWin32Manifest
        /// which is true for the second case and false or non-existent for the third. If those two properties aren't set
        /// then we'll use the stored value (if any) or default to DefaultManifest.
        /// </remarks>
        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            if (!string.IsNullOrEmpty(await defaultProperties.GetEvaluatedPropertyValueAsync(ApplicationManifestMSBuildProperty)))
            {
                return CustomManifestValue;
            }

            string noManifestPropertyValue = await defaultProperties.GetEvaluatedPropertyValueAsync(NoManifestMSBuildProperty);
            if (StringComparers.PropertyLiteralValues.Equals(noManifestPropertyValue, "true"))
            {
                return NoManifestValue;
            }

            string? storedValue = _temporaryPropertyStorage.GetPropertyValue(ApplicationManifestKindProperty);
            if (!Strings.IsNullOrEmpty(storedValue))
            {
                return storedValue;
            }

            // It doesn't matter if it is set to false or the value is not present. We default to "DefaultManifest" scenario.
            return DefaultManifestValue;
        }

        /// <summary>
        /// Sets the application manifest kind property
        /// </summary>
        public override async Task<string?> OnSetPropertyValueAsync(
            string propertyName,
            string? unevaluatedPropertyValue,
            IProjectProperties defaultProperties,
            IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (StringComparers.PropertyLiteralValues.Equals(unevaluatedPropertyValue, DefaultManifestValue))
            {
                _temporaryPropertyStorage.AddOrUpdatePropertyValue(ApplicationManifestKindProperty, DefaultManifestValue);

                await defaultProperties.SaveValueIfCurrentlySetAsync(ApplicationManifestMSBuildProperty, _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync(ApplicationManifestMSBuildProperty);
                await defaultProperties.DeletePropertyAsync(NoManifestMSBuildProperty);
            }
            else if (StringComparers.PropertyLiteralValues.Equals(unevaluatedPropertyValue, NoManifestValue))
            {
                _temporaryPropertyStorage.AddOrUpdatePropertyValue(ApplicationManifestKindProperty, NoManifestValue);

                await defaultProperties.SaveValueIfCurrentlySetAsync(ApplicationManifestMSBuildProperty, _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync(ApplicationManifestMSBuildProperty);
                await defaultProperties.SetPropertyValueAsync(NoManifestMSBuildProperty, "true");
            }
            else if (StringComparers.PropertyLiteralValues.Equals(unevaluatedPropertyValue, CustomManifestValue))
            {
                _temporaryPropertyStorage.AddOrUpdatePropertyValue(ApplicationManifestKindProperty, CustomManifestValue);

                await defaultProperties.RestoreValueIfNotCurrentlySetAsync(ApplicationManifestMSBuildProperty, _temporaryPropertyStorage, dimensionalConditions);
                await defaultProperties.DeletePropertyAsync(NoManifestMSBuildProperty);
            }

            // We don't want to store a value for this so return null.
            return null;
        }
    }
}
