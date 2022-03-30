// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    internal abstract class OutputTypeValueProviderBase : InterceptingPropertyValueProviderBase
    {
        private readonly ProjectProperties _properties;

        protected abstract ImmutableDictionary<string, string> GetMap { get; }
        protected abstract ImmutableDictionary<string, string> SetMap { get; }
        protected abstract string DefaultGetValue { get; }

        protected OutputTypeValueProviderBase(ProjectProperties properties)
        {
            _properties = properties;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            ConfigurationGeneral configuration = await _properties.GetConfigurationGeneralPropertiesAsync();
            string value = await configuration.OutputType.GetEvaluatedValueAtEndAsync();
            if (GetMap.TryGetValue(value, out string? returnValue))
            {
                return returnValue;
            }

            return DefaultGetValue;
        }

        public override async Task<string?> OnSetPropertyValueAsync(
            string propertyName,
            string unevaluatedPropertyValue,
            IProjectProperties defaultProperties,
            IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            string value = SetMap[unevaluatedPropertyValue];
            ConfigurationGeneral configuration = await _properties.GetConfigurationGeneralPropertiesAsync();
            await configuration.OutputType.SetValueAsync(value);

            // Since we have persisted the value of OutputType, we don't have to persist the incoming value
            return null;
        }
    }
}
