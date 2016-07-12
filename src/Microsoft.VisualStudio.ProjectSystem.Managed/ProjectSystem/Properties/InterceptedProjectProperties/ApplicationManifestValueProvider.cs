// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System;
using System.IO;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("ApplicationManifest")]
    internal sealed class ApplicationManifestValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly UnconfiguredProject _unconfiguredProject;

        [ImportingConstructor]
        public ApplicationManifestValueProvider(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            _unconfiguredProject = unconfiguredProject;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            if (!string.IsNullOrEmpty(evaluatedPropertyValue))
            {
                return evaluatedPropertyValue;
            }

            string noManifest = await defaultProperties.GetEvaluatedPropertyValueAsync("NoWin32Manifest").ConfigureAwait(false);
            if (noManifest.Equals("true"))
            {
                return "NoManifest";
            }

            return "DefaultManifest";
        }

        public override async Task<string> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            if (!PathHelper.IsFileNameValid(unevaluatedPropertyValue))
            {
                throw new ArgumentException("Invalid file path");
            }

            // We treat NULL/empty value as reset to default and remove the two properties from the project.
            if (string.IsNullOrEmpty(unevaluatedPropertyValue))
            {
                await defaultProperties.DeletePropertyAsync("ApplicationManifest").ConfigureAwait(false);
                await defaultProperties.DeletePropertyAsync("NoManifest").ConfigureAwait(false);
            }
            else if (string.Equals(unevaluatedPropertyValue, "NoManifest"))
            {
                await defaultProperties.DeletePropertyAsync("ApplicationManifest").ConfigureAwait(false);
                await defaultProperties.SetPropertyValueAsync("NoManifest", "true").ConfigureAwait(false);
            }
            else
            {
                await defaultProperties.DeletePropertyAsync("NoManifest").ConfigureAwait(false);
                string relativePath;
                if (Path.IsPathRooted(unevaluatedPropertyValue) &&
                    PathHelper.TryMakeRelativeToProjectDirectory(_unconfiguredProject, unevaluatedPropertyValue, out relativePath))
                {
                    return relativePath;
                }
            }

            return null;
        }
    }
}
