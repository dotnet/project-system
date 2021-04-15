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
            if (!bool.TryParse(unevaluatedPropertyValue, out bool value))
            {
                return null;
            }

            // When true, remove WarningsAsErrors. Otherwise, remove WarningsNotAsErrors.
            string removePropertyName = value ? WarningsAsErrorsProperty : WarningsNotAsErrorsProperty;
            string restorePropertyName = value ? WarningsNotAsErrorsProperty : WarningsAsErrorsProperty;

            await defaultProperties.SaveValueIfCurrentlySetAsync(removePropertyName, _temporaryPropertyStorage);
            await defaultProperties.DeletePropertyAsync(removePropertyName, dimensionalConditions);
            await defaultProperties.RestoreValueIfNotCurrentlySetAsync(restorePropertyName, _temporaryPropertyStorage);

            return await base.OnSetPropertyValueAsync(propertyName, unevaluatedPropertyValue, defaultProperties, dimensionalConditions);
        }
    }
}
