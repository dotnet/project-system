// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            if (string.IsNullOrEmpty(evaluatedPropertyValue))
            {
                return Task.FromResult(ConfigurationGeneralBrowseObject.RunPostBuildEventValues.OnBuildSuccess);
            }

            return Task.FromResult(evaluatedPropertyValue);
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            if (string.IsNullOrEmpty(unevaluatedPropertyValue))
            {
                return Task.FromResult(ConfigurationGeneralBrowseObject.RunPostBuildEventValues.OnBuildSuccess);
            }

            return Task.FromResult(unevaluatedPropertyValue);
        }
    }
}
