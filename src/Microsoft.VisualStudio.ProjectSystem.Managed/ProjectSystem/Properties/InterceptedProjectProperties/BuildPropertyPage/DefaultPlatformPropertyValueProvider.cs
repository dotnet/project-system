// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("PlatformTarget", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class DefaultPlatformPropertyValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly ConfiguredProject _configuredProject;

        [ImportingConstructor]
        public DefaultPlatformPropertyValueProvider(ConfiguredProject configuredProject)
        {
            _configuredProject = configuredProject;
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return Task.FromResult(GetPlatformValue(evaluatedPropertyValue));
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return Task.FromResult(GetPlatformValue(unevaluatedPropertyValue));
        }

        private string GetPlatformValue(string unevaluatedPropertyValue)
        {
            if (string.IsNullOrEmpty(unevaluatedPropertyValue) && _configuredProject.ProjectConfiguration.Dimensions.TryGetValue("Platform", out string platform))
            {
                unevaluatedPropertyValue = platform;
            }

            return unevaluatedPropertyValue.Equals("AnyCPU") ? "Any CPU" : unevaluatedPropertyValue;
        }

        public override Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            return Task.FromResult<string?>(unevaluatedPropertyValue.Equals("Any CPU") ? "AnyCPU" : unevaluatedPropertyValue);
        }
    }
}
