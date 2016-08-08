// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ExportInterceptingPropertyValueProvider("TargetFramework", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class TargetFrameworkValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly ProjectProperties _properties;

        [ImportingConstructor]
        public TargetFrameworkValueProvider(ProjectProperties properties)
        {
            Requires.NotNull(properties, nameof(properties));

            _properties = properties;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            var configuration = await _properties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            var targetFrameworkMoniker = (string)await configuration.TargetFrameworkMoniker.GetValueAsync().ConfigureAwait(false);
            if (targetFrameworkMoniker != null)
            {
                var targetFramework = new FrameworkName(targetFrameworkMoniker);

                // define MAKETARGETFRAMEWORKVERSION(maj, min, rev) (TARGETFRAMEWORKVERSION)((maj) << 16 | (rev) << 8 | (min))
                var maj = targetFramework.Version.Major;
                var min = targetFramework.Version.Minor;
                var rev = targetFramework.Version.Revision >= 0 ? targetFramework.Version.Revision : 0;
                var propertyValue = unchecked((uint)((maj << 16) | (rev << 8) | min));
                return propertyValue.ToString();
            }

            return await base.OnGetEvaluatedPropertyValueAsync(evaluatedPropertyValue, defaultProperties).ConfigureAwait(false);
        }
    }
}