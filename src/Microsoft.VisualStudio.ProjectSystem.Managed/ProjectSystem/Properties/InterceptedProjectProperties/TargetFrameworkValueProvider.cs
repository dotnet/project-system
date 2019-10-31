// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Runtime.Versioning;
using System.Threading.Tasks;

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

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            ConfigurationGeneral configuration = await _properties.GetConfigurationGeneralPropertiesAsync();
            string? targetFrameworkMoniker = (string?)await configuration.TargetFrameworkMoniker.GetValueAsync();

            if (targetFrameworkMoniker != null)
            {
                var targetFramework = new FrameworkName(targetFrameworkMoniker);

                // define MAKETARGETFRAMEWORKVERSION(maj, min, rev) (TARGETFRAMEWORKVERSION)((maj) << 16 | (rev) << 8 | (min))
                int maj = targetFramework.Version.Major;
                int min = targetFramework.Version.Minor;
                int rev = targetFramework.Version.Revision >= 0 ? targetFramework.Version.Revision : 0;
                uint propertyValue = unchecked((uint)((maj << 16) | (rev << 8) | min));
                return propertyValue.ToString();
            }

            return await base.OnGetEvaluatedPropertyValueAsync(evaluatedPropertyValue, defaultProperties);
        }
    }
}
