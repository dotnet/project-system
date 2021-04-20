// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// The complex Target Framework property consists of three UI-exposed properties: target framework, target OS and target OS version.
    /// Those combine and produce a single value like this: [target framework]-[target OS][target OS version].
    /// This class intercepts those values from the UI to saved them into the TargetFramework property, as well as retrive those values from that same property.
    /// </summary>
    [ExportInterceptingPropertyValueProvider(
        new[]
        {
            TargetFrameworkProperty,
            TargetOSProperty,
            TargetOSVersionProperty
        },
        ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class ComplexTargetFrameworkValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string TargetFrameworkProperty = "InterceptedTargetFramework";
        private const string TargetOSProperty = "TargetOS";
        private const string TargetOSVersionProperty = "TargetOSVersion";
        private const string PropertyInProjectFile = "TargetFramework";

        private struct ComplexTargetFramework
        {
            public string TargetFramework;
            public string? Platform;
            public string? PlatformVersion;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            string? storedComplexValue = await defaultProperties.GetUnevaluatedPropertyValueAsync(PropertyInProjectFile);

            ComplexTargetFramework deconstructedValues = BreakDownValue(storedComplexValue);

            if (StringComparers.PropertyLiteralValues.Equals(propertyName, TargetOSProperty))
            {
                // Setting the target OS.
                await defaultProperties.SetPropertyValueAsync(PropertyInProjectFile, ComputeValue(deconstructedValues.TargetFramework, unevaluatedPropertyValue, deconstructedValues.PlatformVersion));
            }
            else if (StringComparers.PropertyLiteralValues.Equals(propertyName, TargetOSVersionProperty))
            {
                // Setting the OS version.
                await defaultProperties.SetPropertyValueAsync(PropertyInProjectFile, ComputeValue(deconstructedValues.TargetFramework, deconstructedValues.Platform, unevaluatedPropertyValue));
            }
            else if (StringComparers.PropertyLiteralValues.Equals(propertyName, TargetFrameworkProperty))
            {
                // Setting the target framework.
                await defaultProperties.SetPropertyValueAsync(PropertyInProjectFile, ComputeValue(unevaluatedPropertyValue, deconstructedValues.Platform, deconstructedValues.PlatformVersion));
            }

            return null;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string? storedComplexValue = await defaultProperties.GetUnevaluatedPropertyValueAsync(PropertyInProjectFile);
            ComplexTargetFramework values = BreakDownValue(storedComplexValue);
            return await base.OnGetEvaluatedPropertyValueAsync(propertyName, ExtractValue(values, propertyName) ?? "", defaultProperties);
        }

        public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string? storedComplexValue = await defaultProperties.GetUnevaluatedPropertyValueAsync(PropertyInProjectFile);
            ComplexTargetFramework values = BreakDownValue(storedComplexValue);
            return await base.OnGetUnevaluatedPropertyValueAsync(propertyName, ExtractValue(values, propertyName) ?? "", defaultProperties);
        }

        //private static string? CommonCode(string propertyName, IProjectProperties defaultProperties)
        //{
        //    string? storedComplexValue = defaultProperties.GetUnevaluatedPropertyValueAsync(PropertyToBeSavedOnProjectFile); // TODO: change to TargetFrameworkProperty
        //    ComplexTargetFramework values = BreakDownValue(storedComplexValue);
        //    return ExtractValue(values, propertyName);
        //}

        /// <summary>
        /// Extracts the specified value from the ComplexTargetFramework structure.
        /// </summary>
        /// <param name="values">The strucutre were all the values are.</param>
        /// <param name="propertyName">The property's value we want to return.</param>
        /// <returns></returns>
        private static string? ExtractValue(ComplexTargetFramework values, string propertyName)
        {
            return propertyName switch
            {
                TargetFrameworkProperty => values.TargetFramework,
                TargetOSProperty => values.Platform,
                TargetOSVersionProperty => values.PlatformVersion,
                _ => null,
            };
        }

        /// <summary>
        /// Combines the three values that make up the TargetFramework property.
        /// I.e. net5.0, windows, 10.0 => net5.0-windows10.0
        /// net5.0, null, null => net5.0
        /// </summary>
        /// <param name="targetFramework">The target framework value.</param>
        /// <param name="targetPlatform">The target platform value.</param>
        /// <param name="platformVersion">The platform version value.</param>
        /// <returns></returns>
        private static string ComputeValue(string targetFramework, string? targetPlatform, string? platformVersion)
        {
            if (targetPlatform != null) // TODO: Ensure TargetFramework is not null.
            {
                return targetFramework + "-" + targetPlatform + platformVersion;
            }
            return targetFramework;
        }

        /// <summary>
        /// Takes the combined string and separates the values into the ComplextTargetFramework structure.
        /// TargetFramework must always be present, but Platform and PlatformVersion can be null.
        /// <para>i.e. net5.0-windows10.0 => { net5.0, windows, 10.0 }; netcoreapp3.1 => { netcoreapp3.1, null, null }</para>
        /// </summary>
        /// <param name="storedComplexTargetFramework"></param>
        /// <returns></returns>
        private static ComplexTargetFramework BreakDownValue(string? storedComplexTargetFramework)
        {
            ComplexTargetFramework result = new ComplexTargetFramework();

            if (storedComplexTargetFramework != null)
            {
                result.Platform = null;
                result.PlatformVersion = null;
            
                if (storedComplexTargetFramework.IndexOf('-') != -1)
                {
                    result.TargetFramework = storedComplexTargetFramework.Split('-')[0];
                    string targetPlatformAndVersion = storedComplexTargetFramework.Split('-')[1];

                    if (targetPlatformAndVersion != null)
                    {
                        result.PlatformVersion = System.Text.RegularExpressions.Regex.Match(targetPlatformAndVersion, @"[\d|\.]+").Value;
                        result.Platform = System.Text.RegularExpressions.Regex.Match(targetPlatformAndVersion, @"^([a-zA-Z])+").Value;
                    }
                }
                else
                {
                    // The stored value has only the TargetFramework property without platform values.
                    result.TargetFramework = storedComplexTargetFramework;
                }
            }
            return result;
        }
    }
}
