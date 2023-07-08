// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Text;
using static Microsoft.VisualStudio.ProjectSystem.ConfigurationGeneral;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("TargetMultipleFrameworks", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class TargetMultipleFrameworksValueProvider : InterceptingPropertyValueProviderBase
    {
        private static readonly string[] s_msBuildPropertyNames = { TargetFrameworksProperty, TargetFrameworkProperty};
        
        public override Task<bool> IsValueDefinedInContextAsync(string propertyName, IProjectProperties defaultProperties)
        {
            return IsValueDefinedInContextMSBuildPropertiesAsync(defaultProperties, s_msBuildPropertyNames);
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (bool.TryParse(unevaluatedPropertyValue, out bool value))
            {
                if (value)
                {
                    // Move TargetFramework -> TargetFrameworks
                    string? targetFramework = await defaultProperties.GetUnevaluatedPropertyValueAsync(TargetFrameworkProperty);

                    if (!Strings.IsNullOrEmpty(targetFramework))
                    {
                        await defaultProperties.SetPropertyValueAsync(TargetFrameworksProperty, targetFramework);
                        await defaultProperties.DeletePropertyAsync(TargetFrameworkProperty);
                    }
                }
                else
                {
                    // Move TargetFrameworks -> TargetFramework
                    //
                    // Requires TargetFrameworks to contain a valid string
                    string? targetFrameworks = await defaultProperties.GetUnevaluatedPropertyValueAsync(TargetFrameworksProperty);

                    if (!Strings.IsNullOrEmpty(targetFrameworks))
                    {
                        string? firstTargetFramework = new LazyStringSplit(targetFrameworks, ';').FirstOrDefault();

                        if (!Strings.IsNullOrEmpty(firstTargetFramework))
                        {
                            await defaultProperties.SetPropertyValueAsync(TargetFrameworkProperty, firstTargetFramework);
                            await defaultProperties.DeletePropertyAsync(TargetFrameworksProperty);
                        }
                    }
                }
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
            string? targetFrameworks = await getValue(TargetFrameworksProperty);

            return string.IsNullOrEmpty(targetFrameworks) ? bool.FalseString : bool.TrueString;
        }
    }
}
