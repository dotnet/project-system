// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider(ResourceSpecificationKindProperty, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class ResourceSpecificationKindValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly ITemporaryPropertyStorage _temporaryPropertyStorage;

        internal const string ResourceSpecificationKindProperty = "ResourceSpecificationKind";
        internal const string Win32ResourceMSBuildProperty = "Win32Resource";
        internal const string ApplicationIconMSBuildProperty = "ApplicationIcon";
        internal const string ApplicationManifestMSBuildProperty = "ApplicationManifest";
        internal const string IconAndManifestValue = "IconAndManifest";
        internal const string ResourceFileValue = "ResourceFile";

        private static readonly string[] s_msBuildPropertyNames = { Win32ResourceMSBuildProperty, ApplicationIconMSBuildProperty, ApplicationManifestMSBuildProperty };

        [ImportingConstructor]
        public ResourceSpecificationKindValueProvider(ITemporaryPropertyStorage temporaryPropertyStorage)
        {
            _temporaryPropertyStorage = temporaryPropertyStorage;
        }

        public override Task<bool> IsValueDefinedInContextAsync(string propertyName, IProjectProperties defaultProperties)
        {
            return IsValueDefinedInContextMSBuildPropertiesAsync(defaultProperties, s_msBuildPropertyNames);
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (StringComparers.PropertyLiteralValues.Equals(unevaluatedPropertyValue, IconAndManifestValue))
            {
                _temporaryPropertyStorage.AddOrUpdatePropertyValue(ResourceSpecificationKindProperty, IconAndManifestValue);

                await defaultProperties.SaveValueIfCurrentlySetAsync(Win32ResourceMSBuildProperty, _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync(Win32ResourceMSBuildProperty);
                await defaultProperties.RestoreValueIfNotCurrentlySetAsync(ApplicationIconMSBuildProperty, _temporaryPropertyStorage, dimensionalConditions);
                await defaultProperties.RestoreValueIfNotCurrentlySetAsync(ApplicationManifestMSBuildProperty, _temporaryPropertyStorage, dimensionalConditions);
            }
            else if (StringComparers.PropertyLiteralValues.Equals(unevaluatedPropertyValue, ResourceFileValue))
            {
                _temporaryPropertyStorage.AddOrUpdatePropertyValue(ResourceSpecificationKindProperty, ResourceFileValue);

                await defaultProperties.SaveValueIfCurrentlySetAsync(ApplicationIconMSBuildProperty, _temporaryPropertyStorage);
                await defaultProperties.SaveValueIfCurrentlySetAsync(ApplicationManifestMSBuildProperty, _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync(ApplicationIconMSBuildProperty);
                await defaultProperties.DeletePropertyAsync(ApplicationManifestMSBuildProperty);
                await defaultProperties.RestoreValueIfNotCurrentlySetAsync(Win32ResourceMSBuildProperty, _temporaryPropertyStorage, dimensionalConditions);
            }

            return null;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string win32Resource = await defaultProperties.GetEvaluatedPropertyValueAsync(Win32ResourceMSBuildProperty);
            string applicationIconResource = await defaultProperties.GetEvaluatedPropertyValueAsync(ApplicationIconMSBuildProperty);
            string applicationManifestResource = await defaultProperties.GetEvaluatedPropertyValueAsync(ApplicationManifestMSBuildProperty);

            return ComputeValue(win32Resource, applicationIconResource, applicationManifestResource);
        }

        public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string? win32Resource = await defaultProperties.GetUnevaluatedPropertyValueAsync(Win32ResourceMSBuildProperty);
            string? applicationIconResource = await defaultProperties.GetUnevaluatedPropertyValueAsync(ApplicationIconMSBuildProperty);
            string? applicationManifestResource = await defaultProperties.GetUnevaluatedPropertyValueAsync(ApplicationManifestMSBuildProperty);

            return ComputeValue(win32Resource, applicationIconResource, applicationManifestResource);
        }

        private string ComputeValue(string? win32Resource, string? applicationIconResource, string? applicationManifestResource)
        {
            if (!string.IsNullOrEmpty(applicationIconResource)
                || !string.IsNullOrEmpty(applicationManifestResource))
            {
                return IconAndManifestValue;
            }

            if (!string.IsNullOrEmpty(win32Resource))
            {
                return ResourceFileValue;
            }

            if (_temporaryPropertyStorage.GetPropertyValue(ResourceSpecificationKindProperty) is string savedValue)
            {
                return savedValue;
            }

            return IconAndManifestValue;
        }
    }
}
