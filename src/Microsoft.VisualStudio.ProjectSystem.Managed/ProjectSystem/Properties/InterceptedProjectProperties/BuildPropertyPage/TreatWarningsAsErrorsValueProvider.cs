// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("TreatWarningsAsErrors", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class TreatWarningsAsErrorsValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string WarningsAsErrorsProperty = "WarningsAsErrors";
        private const string WarningsNotAsErrorsProperty = "WarningsNotAsErrors";
        private readonly ITemporaryPropertyStorage _temporaryPropertyStorage;

        [ImportingConstructor]
        public TreatWarningsAsErrorsValueProvider(ITemporaryPropertyStorage temporaryPropertyStorage)
        {
            _temporaryPropertyStorage = temporaryPropertyStorage;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (StringComparers.PropertyLiteralValues.Equals(unevaluatedPropertyValue, "true"))
            {
                // When setting this to "true", remove WarningsAsErrors
                await defaultProperties.SaveValueIfCurrentlySetAsync(WarningsAsErrorsProperty, _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync(WarningsAsErrorsProperty, dimensionalConditions);
                await defaultProperties.RestoreValueIfNotCurrentlySetAsync(WarningsNotAsErrorsProperty, _temporaryPropertyStorage);
            }
            else
            {
                // When settings this to "false", remove WarningsNotAsErrors
                await defaultProperties.SaveValueIfCurrentlySetAsync(WarningsNotAsErrorsProperty, _temporaryPropertyStorage);
                await defaultProperties.DeletePropertyAsync(WarningsNotAsErrorsProperty, dimensionalConditions);
                await defaultProperties.RestoreValueIfNotCurrentlySetAsync(WarningsAsErrorsProperty, _temporaryPropertyStorage);
            }

            return await base.OnSetPropertyValueAsync(propertyName, unevaluatedPropertyValue, defaultProperties, dimensionalConditions);
        }
    }
}
