// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider(ConfiguredBrowseObject.PlatformTargetProperty, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class DefaultPlatformPropertyValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string AnyCpuPlatformName = "AnyCPU";
        private const string AnyCpuDisplayName = "Any CPU";

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

        private string GetPlatformValue(string value)
        {
            if (string.IsNullOrEmpty(value) && _configuredProject.ProjectConfiguration.Dimensions.TryGetValue("Platform", out string platform))
            {
                value = platform;
            }

            return value.Equals(AnyCpuPlatformName) ? AnyCpuDisplayName : value;
        }

        public override Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            return Task.FromResult<string?>(unevaluatedPropertyValue.Equals(AnyCpuDisplayName) ? AnyCpuPlatformName : unevaluatedPropertyValue);
        }
    }
}
