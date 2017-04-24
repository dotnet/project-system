// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using static Microsoft.VisualStudio.ProjectSystem.Build.TargetFrameworkProjectConfigurationDimensionProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    [ExportInterceptingPropertyValueProvider("TargetFrameworkMonikers", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class TargetFrameworkMonikersValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly ProjectProperties _properties;

        [ImportingConstructor]
        public TargetFrameworkMonikersValueProvider(ProjectProperties properties)
        {
            _properties = properties;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            var configuration = await _properties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(true);
            var currentTargetFramework = (string)await configuration.TargetFramework.GetValueAsync().ConfigureAwait(true);
            var currentTargetFrameworks = (string)await configuration.TargetFrameworks.GetValueAsync().ConfigureAwait(true);
            if (!string.IsNullOrEmpty(currentTargetFrameworks))
            {
                // sorting the target frameworks so projects that specify the same set of frameworks return the same string
                return string.Join(";", ParseTargetFrameworks(currentTargetFrameworks).Sort());
            }
            else if (!string.IsNullOrEmpty(currentTargetFramework))
            {
                return currentTargetFramework;
            }

            return string.Empty;
        }
    }
}
