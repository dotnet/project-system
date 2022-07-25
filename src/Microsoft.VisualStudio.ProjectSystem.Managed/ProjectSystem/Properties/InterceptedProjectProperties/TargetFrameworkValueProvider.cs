// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.Versioning;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("TargetFramework", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class TargetFrameworkValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly ProjectProperties _properties;

        [ImportingConstructor]
        public TargetFrameworkValueProvider(ProjectProperties properties)
        {
            _properties = properties;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            ConfigurationGeneral configuration = await _properties.GetConfigurationGeneralPropertiesAsync();
            string? targetFrameworkMoniker = (string?)await configuration.TargetFrameworkMoniker.GetValueAsync();

            if (targetFrameworkMoniker is not null)
            {
                var targetFramework = new FrameworkName(targetFrameworkMoniker);

                // define MAKETARGETFRAMEWORKVERSION(maj, min, rev) (TARGETFRAMEWORKVERSION)((maj) << 16 | (rev) << 8 | (min))
                int maj = targetFramework.Version.Major;
                int min = targetFramework.Version.Minor;
                int rev = targetFramework.Version.Revision >= 0 ? targetFramework.Version.Revision : 0;
                uint propertyValue = unchecked((uint)((maj << 16) | (rev << 8) | min));
                return propertyValue.ToString();
            }

            return evaluatedPropertyValue;
        }
    }
}
