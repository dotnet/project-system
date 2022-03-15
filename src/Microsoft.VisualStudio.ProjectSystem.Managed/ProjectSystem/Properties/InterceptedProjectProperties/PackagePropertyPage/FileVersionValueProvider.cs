// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    [ExportInterceptingPropertyValueProvider(FileVersionPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class FileVersionValueProvider : BaseVersionValueProvider
    {
        private const string FileVersionPropertyName = "FileVersion";

        private static readonly Version s_defaultFileVersion = new(1, 0, 0, 0);

        protected override string PropertyName => FileVersionPropertyName;

        protected override async Task<Version> GetDefaultVersionAsync(IProjectProperties defaultProperties)
        {
            // Default semantic/package version just has 3 fields, we need to append an additional Revision field with value "0".
            Version defaultVersion = await base.GetDefaultVersionAsync(defaultProperties);

            if (ReferenceEquals(defaultVersion, DefaultVersion))
            {
                return s_defaultFileVersion;
            }

            return new Version(defaultVersion.Major, defaultVersion.Minor, Math.Max(defaultVersion.Build, 0), revision: Math.Max(defaultVersion.Revision, 0));
        }
    }
}
