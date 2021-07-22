// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Set the default value for the <c>RunPostBuildEvent</c> property via interception.
    /// </summary>
    /// <remarks>
    /// If the property system is updated to support <see cref="IEnumValue.IsDefault"/> this class
    /// can be removed, and the property's <c>Persistence</c> changed to remove interception.
    /// </remarks>
    [ExportInterceptingPropertyValueProvider(ConfigurationGeneralBrowseObject.RunPostBuildEventProperty, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class RunPostBuildEventValueProvider : InterceptingPropertyValueProviderBase
    {
        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string value = await base.OnGetEvaluatedPropertyValueAsync(propertyName, evaluatedPropertyValue, defaultProperties);

            if (string.IsNullOrEmpty(value))
            {
                value = ConfigurationGeneralBrowseObject.RunPostBuildEventValues.OnBuildSuccess;
            }

            return value;
        }

        public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            string value = await base.OnGetUnevaluatedPropertyValueAsync(propertyName, unevaluatedPropertyValue, defaultProperties);

            if (string.IsNullOrEmpty(value))
            {
                value = ConfigurationGeneralBrowseObject.RunPostBuildEventValues.OnBuildSuccess;
            }

            return value;
        }
    }
}
