// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("ResourceSpecificationKind", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class ResourceSpecificationKindValueProvider : InterceptingPropertyValueProviderBase
    {
        // TODO should the rule file generate property and enum value constants that we can use here instead of these string literals?

        private readonly ITemporaryPropertyStorage _temporaryPropertyStorage;

        [ImportingConstructor]
        public ResourceSpecificationKindValueProvider(ITemporaryPropertyStorage temporaryPropertyStorage)
        {
            _temporaryPropertyStorage = temporaryPropertyStorage;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (unevaluatedPropertyValue == "IconAndManifest")
            {
                await defaultProperties.RememberValueIfCurrentlySet("Win32Resource", _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync("Win32Resource");
                await defaultProperties.RestoreValueIfNotCurrentlySet("ApplicationIcon", _temporaryPropertyStorage);
                await defaultProperties.RestoreValueIfNotCurrentlySet("ApplicationManifest", _temporaryPropertyStorage);
            }
            else if (unevaluatedPropertyValue == "ResourceFile")
            {
                await defaultProperties.RememberValueIfCurrentlySet("ApplicationIcon", _temporaryPropertyStorage);
                await defaultProperties.RememberValueIfCurrentlySet("ApplicationManifest", _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync("ApplicationIcon");
                await defaultProperties.DeletePropertyAsync("ApplicationManifest");
                await defaultProperties.RestoreValueIfNotCurrentlySet("Win32Resource", _temporaryPropertyStorage);
            }

            return null;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string win32Resource = await defaultProperties.GetEvaluatedPropertyValueAsync("Win32Resource");

            return ComputeValue(win32Resource);
        }

        public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string? win32Resource = await defaultProperties.GetUnevaluatedPropertyValueAsync("Win32Resource");

            return ComputeValue(win32Resource);
        }

        private static string ComputeValue(string? win32Resource)
        {
            return string.IsNullOrEmpty(win32Resource) ? "IconAndManifest" : "ResourceFile";
        }
    }
}
