// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("AssemblyOriginatorKeyFile", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class AssemblyOriginatorKeyFileValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly UnconfiguredProject _unconfiguredProject;

        [ImportingConstructor]
        public AssemblyOriginatorKeyFileValueProvider(UnconfiguredProject project)
        {
            _unconfiguredProject = project;
        }

        public override Task<string?> OnSetPropertyValueAsync(
            string propertyName,
            string unevaluatedPropertyValue,
            IProjectProperties defaultProperties,
            IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (Path.IsPathRooted(unevaluatedPropertyValue) &&
                _unconfiguredProject.TryMakeRelativeToProjectDirectory(unevaluatedPropertyValue, out string? relativePath))
            {
                unevaluatedPropertyValue = relativePath;
            }

            return Task.FromResult<string?>(unevaluatedPropertyValue);
        }
    }
}
