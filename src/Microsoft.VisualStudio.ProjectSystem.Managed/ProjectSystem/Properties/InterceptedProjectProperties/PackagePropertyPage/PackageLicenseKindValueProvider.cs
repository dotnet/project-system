// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider(PackageLicenseKindProperty, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class PackageLicenseKindValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly ITemporaryPropertyStorage _temporaryPropertyStorage;

        private const string PackageLicenseKindProperty = "PackageLicenseKind";
        private const string PackageLicenseFileMSBuildProperty = "PackageLicenseFile";
        private const string PackageLicenseExpressionMSBuildProperty = "PackageLicenseExpression";
        private const string PackageRequireLicenseAcceptanceMSBuildProperty = "PackageRequireLicenseAcceptance";
        private const string ExpressionValue = "Expression";
        private const string FileValue = "File";
        private const string NoneValue = "None";

        [ImportingConstructor]
        public PackageLicenseKindValueProvider(ITemporaryPropertyStorage temporaryPropertyStorage)
        {
            _temporaryPropertyStorage = temporaryPropertyStorage;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (StringComparers.PropertyLiteralValues.Equals(unevaluatedPropertyValue, ExpressionValue))
            {
                _temporaryPropertyStorage.AddOrUpdatePropertyValue(PackageLicenseKindProperty, ExpressionValue);

                await defaultProperties.SaveValueIfCurrentlySetAsync(PackageLicenseFileMSBuildProperty, _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync(PackageLicenseFileMSBuildProperty);
                await defaultProperties.RestoreValueIfNotCurrentlySetAsync(PackageLicenseExpressionMSBuildProperty, _temporaryPropertyStorage, dimensionalConditions);
                await defaultProperties.RestoreValueIfNotCurrentlySetAsync(PackageRequireLicenseAcceptanceMSBuildProperty, _temporaryPropertyStorage, dimensionalConditions);
            }
            else if (StringComparers.PropertyLiteralValues.Equals(unevaluatedPropertyValue, FileValue))
            {
                _temporaryPropertyStorage.AddOrUpdatePropertyValue(PackageLicenseKindProperty, FileValue);

                await defaultProperties.SaveValueIfCurrentlySetAsync(PackageLicenseExpressionMSBuildProperty, _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync(PackageLicenseExpressionMSBuildProperty);
                await defaultProperties.RestoreValueIfNotCurrentlySetAsync(PackageLicenseFileMSBuildProperty, _temporaryPropertyStorage, dimensionalConditions);
                await defaultProperties.RestoreValueIfNotCurrentlySetAsync(PackageRequireLicenseAcceptanceMSBuildProperty, _temporaryPropertyStorage, dimensionalConditions);
            }
            else if (StringComparers.PropertyLiteralValues.Equals(unevaluatedPropertyValue, NoneValue))
            {
                _temporaryPropertyStorage.AddOrUpdatePropertyValue(PackageLicenseKindProperty, NoneValue);

                await defaultProperties.SaveValueIfCurrentlySetAsync(PackageLicenseFileMSBuildProperty, _temporaryPropertyStorage);
                await defaultProperties.SaveValueIfCurrentlySetAsync(PackageLicenseExpressionMSBuildProperty, _temporaryPropertyStorage);
                await defaultProperties.SaveValueIfCurrentlySetAsync(PackageRequireLicenseAcceptanceMSBuildProperty, _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync(PackageLicenseFileMSBuildProperty);
                await defaultProperties.DeletePropertyAsync(PackageLicenseExpressionMSBuildProperty);
                await defaultProperties.DeletePropertyAsync(PackageRequireLicenseAcceptanceMSBuildProperty);
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

        private async Task<string> ComputeValueAsync(Func<string, Task<string?>> getValue)
        {
            if (!string.IsNullOrEmpty(await getValue(PackageLicenseExpressionMSBuildProperty)))
            {
                return ExpressionValue;
            }

            if (!string.IsNullOrEmpty(await getValue(PackageLicenseFileMSBuildProperty)))
            {
                return FileValue;
            }

            string? storedValue = _temporaryPropertyStorage.GetPropertyValue(PackageLicenseKindProperty);
            if (!Strings.IsNullOrEmpty(storedValue))
            {
                return storedValue;
            }
                
            return NoneValue;
        }
    }
}
