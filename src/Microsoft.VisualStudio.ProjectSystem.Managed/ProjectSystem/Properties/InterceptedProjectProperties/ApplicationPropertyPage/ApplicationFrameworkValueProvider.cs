// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("UseApplicationFramework", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal sealed class ApplicationFrameworkValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string ApplicationFrameworkMSBuildProperty = "MyType";
        private const string EnabledValue = "WindowsForms";
        private const string DisabledValue = "WindowsFormsWithCustomSubMain";

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (bool.TryParse(unevaluatedPropertyValue, out bool value))
            {
                if (value)
                {
                    // Enabled: <MyType>WindowsForms</MyType>
                    await defaultProperties.SetPropertyValueAsync(ApplicationFrameworkMSBuildProperty, EnabledValue);
                }
                else
                {
                    // Disabled: <MyType>WindowsFormsWithCustomSubMain</MyType>
                    await defaultProperties.SetPropertyValueAsync(ApplicationFrameworkMSBuildProperty, DisabledValue);
                }
            }
            return null;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            var value = await defaultProperties.GetEvaluatedPropertyValueAsync(ApplicationFrameworkMSBuildProperty);

            if (value == EnabledValue)
            {
                return "true";
            }
            else if (value == DisabledValue)
            {
                return "false";
            }
            else
            {
                return string.Empty;
            }
        }

        public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            var value = await defaultProperties.GetUnevaluatedPropertyValueAsync(ApplicationFrameworkMSBuildProperty);

            return value switch
            {
                EnabledValue => "true",
                DisabledValue => "false",
                _ => string.Empty
            };
        }
    }
}
