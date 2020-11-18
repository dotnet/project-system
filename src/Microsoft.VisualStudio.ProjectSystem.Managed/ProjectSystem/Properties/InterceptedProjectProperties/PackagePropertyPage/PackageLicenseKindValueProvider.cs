// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("PackageLicenseKind", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class PackageLicenseKindValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly ITemporaryPropertyStorage _temporaryPropertyStorage;

        // TODO should the rule file generate property and enum value constants that we can use here instead of these string literals?

        [ImportingConstructor]
        public PackageLicenseKindValueProvider(ITemporaryPropertyStorage temporaryPropertyStorage)
        {
            _temporaryPropertyStorage = temporaryPropertyStorage;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (unevaluatedPropertyValue == "Expression")
            {
                await defaultProperties.RememberValueIfCurrentlySet("PackageLicenseFile", _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync("PackageLicenseFile");
                await defaultProperties.RestoreValueIfNotCurrentlySet("PackageLicenseExpression", _temporaryPropertyStorage);
                await defaultProperties.RestoreValueIfNotCurrentlySet("PackageRequireLicenseAcceptance", _temporaryPropertyStorage);
            }
            else if (unevaluatedPropertyValue == "File")
            {
                await defaultProperties.RememberValueIfCurrentlySet("PackageLicenseExpression", _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync("PackageLicenseExpression");
                await defaultProperties.RestoreValueIfNotCurrentlySet("PackageLicenseFile", _temporaryPropertyStorage);
                await defaultProperties.RestoreValueIfNotCurrentlySet("PackageRequireLicenseAcceptance", _temporaryPropertyStorage);
            }
            else if (unevaluatedPropertyValue == "None")
            {
                await defaultProperties.RememberValueIfCurrentlySet("PackageLicenseFile", _temporaryPropertyStorage);
                await defaultProperties.RememberValueIfCurrentlySet("PackageLicenseExpression", _temporaryPropertyStorage);
                await defaultProperties.RememberValueIfCurrentlySet("PackageRequireLicenseAcceptance", _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync("PackageLicenseFile");
                await defaultProperties.DeletePropertyAsync("PackageLicenseExpression");
                await defaultProperties.DeletePropertyAsync("PackageRequireLicenseAcceptance");
            }

            return null;
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return ComputeValueAsync(defaultProperties.GetEvaluatedPropertyValueAsync!);
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return ComputeValueAsync(defaultProperties.GetUnevaluatedPropertyValueAsync);
        }

        private static async Task<string> ComputeValueAsync(Func<string, Task<string?>> getValue)
        {
            if (!string.IsNullOrEmpty(await getValue("PackageLicenseExpression")))
            {
                return "Expression";
            }

            if (!string.IsNullOrEmpty(await getValue("PackageLicenseFile")))
            {
                return "File";
            }
                
            return "None";
        }
    }
}
