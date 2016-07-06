// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider]
    internal sealed class AssemblyOriginatorKeyFileValueProvider : InterceptingPropertyValueProviderBase
    {
        public override string GetPropertyName() => "AssemblyOriginatorKeyFile";

        public override Task<string> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            try
            {
                var projectFileFullPath = defaultProperties.FileFullPath;
                if (!string.IsNullOrEmpty(projectFileFullPath) && PathUtilities.IsAbsolute(unevaluatedPropertyValue))
                {
                    var projectDirectory = Path.GetDirectoryName(projectFileFullPath);
                    unevaluatedPropertyValue = FilePathUtilities.GetRelativePath(projectDirectory, unevaluatedPropertyValue);
                }
            }
            catch(Exception)
            {
                // Ignore exceptions.
            }

            return Task.FromResult(unevaluatedPropertyValue);
        }
    }
}