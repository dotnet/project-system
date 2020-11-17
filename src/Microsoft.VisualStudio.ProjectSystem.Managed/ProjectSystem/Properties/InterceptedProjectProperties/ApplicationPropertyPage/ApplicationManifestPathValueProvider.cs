// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("ApplicationManifestPath", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class ApplicationManifestPathValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly UnconfiguredProject _unconfiguredProject;

        private const string ApplicationManifestMSBuildProperty = "ApplicationManifest";

        [ImportingConstructor]
        public ApplicationManifestPathValueProvider(UnconfiguredProject project)
        {
            _unconfiguredProject = project;
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return defaultProperties.GetEvaluatedPropertyValueAsync(ApplicationManifestMSBuildProperty);
        }

        public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return await defaultProperties.GetUnevaluatedPropertyValueAsync(ApplicationManifestMSBuildProperty)
                ?? string.Empty;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            // If we can make the path relative to the project folder do so. Otherwise just use the given path.
            if (Path.IsPathRooted(unevaluatedPropertyValue) &&
                PathHelper.TryMakeRelativeToProjectDirectory(_unconfiguredProject, unevaluatedPropertyValue, out string? relativePath))
            {
                unevaluatedPropertyValue = relativePath;
            }

            await defaultProperties.SetPropertyValueAsync(ApplicationManifestMSBuildProperty, unevaluatedPropertyValue);

            return null;
        }
    }
}
