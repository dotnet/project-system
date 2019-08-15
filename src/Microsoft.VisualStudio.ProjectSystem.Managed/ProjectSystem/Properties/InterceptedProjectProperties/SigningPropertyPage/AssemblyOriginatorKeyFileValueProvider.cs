// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

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
            string unevaluatedPropertyValue,
            IProjectProperties defaultProperties,
            IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (Path.IsPathRooted(unevaluatedPropertyValue) &&
                _unconfiguredProject.TryMakeRelativeToProjectDirectory(unevaluatedPropertyValue, out string relativePath))
            {
                unevaluatedPropertyValue = relativePath;
            }

            return Task.FromResult<string?>(unevaluatedPropertyValue);
        }
    }
}
