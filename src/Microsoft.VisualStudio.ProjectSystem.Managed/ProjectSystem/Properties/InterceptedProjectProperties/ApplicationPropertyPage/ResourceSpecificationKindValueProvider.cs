// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("ResourceSpecificationKind", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class ResourceSpecificationKindValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly ITemporaryPropertyStorage _temporaryPropertyStorage;

        private const string Win32ResourceMSBuildProperty = "Win32Resource";
        private const string ApplicationIconMSBuildProperty = "ApplicationIcon";
        private const string ApplicationManifestMSBuildProperty = "ApplicationManifest";
        private const string IconAndManifestValue = "IconAndManifest";
        private const string ResourceFileValue = "ResourceFile";

        [ImportingConstructor]
        public ResourceSpecificationKindValueProvider(ITemporaryPropertyStorage temporaryPropertyStorage)
        {
            _temporaryPropertyStorage = temporaryPropertyStorage;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (StringComparers.PropertyLiteralValues.Equals(unevaluatedPropertyValue, IconAndManifestValue))
            {
                await defaultProperties.SaveValueIfCurrentlySetAsync(Win32ResourceMSBuildProperty, _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync(Win32ResourceMSBuildProperty);
                await defaultProperties.RestoreValueIfNotCurrentlySetAsync(ApplicationIconMSBuildProperty, _temporaryPropertyStorage);
                await defaultProperties.RestoreValueIfNotCurrentlySetAsync(ApplicationManifestMSBuildProperty, _temporaryPropertyStorage);
            }
            else if (StringComparers.PropertyLiteralValues.Equals(unevaluatedPropertyValue, ResourceFileValue))
            {
                await defaultProperties.SaveValueIfCurrentlySetAsync(ApplicationIconMSBuildProperty, _temporaryPropertyStorage);
                await defaultProperties.SaveValueIfCurrentlySetAsync(ApplicationManifestMSBuildProperty, _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync(ApplicationIconMSBuildProperty);
                await defaultProperties.DeletePropertyAsync(ApplicationManifestMSBuildProperty);
                await defaultProperties.RestoreValueIfNotCurrentlySetAsync(Win32ResourceMSBuildProperty, _temporaryPropertyStorage);
            }

            return null;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string win32Resource = await defaultProperties.GetEvaluatedPropertyValueAsync(Win32ResourceMSBuildProperty);

            return ComputeValue(win32Resource);
        }

        public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string? win32Resource = await defaultProperties.GetUnevaluatedPropertyValueAsync(Win32ResourceMSBuildProperty);

            return ComputeValue(win32Resource);
        }

        private static string ComputeValue(string? win32Resource)
        {
            return string.IsNullOrEmpty(win32Resource) ? IconAndManifestValue : ResourceFileValue;
        }
    }
}
