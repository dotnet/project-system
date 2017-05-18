// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    internal abstract class OutputTypeValueProviderBase : InterceptingPropertyValueProviderBase
    {
        private readonly ProjectProperties _properties;

        protected abstract ImmutableDictionary<string, string> GetMap { get; }
        protected abstract ImmutableDictionary<string, string> SetMap { get; }

        public OutputTypeValueProviderBase(ProjectProperties properties)
        {
            _properties = properties;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            var configuration = await _properties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            var value = await configuration.OutputType.GetEvaluatedValueAtEndAsync().ConfigureAwait(false);
            return GetMap[value];
        }


        public override async Task<string> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            var value = SetMap[unevaluatedPropertyValue];
            var configuration = await _properties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            await configuration.OutputType.SetValueAsync(value).ConfigureAwait(false);

            // Since we have persisted the value of OutputType, we dont have to persist the incoming value
            return null;
        }
    }
}
