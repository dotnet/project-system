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
        public AssemblyOriginatorKeyFileValueProvider(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            _unconfiguredProject = unconfiguredProject;
        }

        public override Task<string> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            string relativePath;
            if (Path.IsPathRooted(unevaluatedPropertyValue) &&
                PathHelper.TryMakeRelativeToProjectDirectory(_unconfiguredProject, unevaluatedPropertyValue, out relativePath))
            {
                unevaluatedPropertyValue = relativePath;
            }

            return Task.FromResult(unevaluatedPropertyValue);
        }
    }
}