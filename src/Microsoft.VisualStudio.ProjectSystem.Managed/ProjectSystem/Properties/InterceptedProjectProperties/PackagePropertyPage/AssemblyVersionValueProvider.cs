// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    [ExportInterceptingPropertyValueProvider(AssemblyVersionPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class AssemblyVersionValueProvider : BaseVersionValueProvider
    {
        private static readonly Version s_defaultAssemblyVersion = new Version(1, 0, 0, 0);

        private const string AssemblyVersionPropertyName = "AssemblyVersion";

        protected override string PropertyName => AssemblyVersionPropertyName;

        protected override async Task<Version> GetDefaultVersionAsync(IProjectProperties defaultProperties)
        {
            // Default semantic/package version just has 3 fields, we need to append an additional Revision field with value "0".
            Version defaultVersion = await base.GetDefaultVersionAsync(defaultProperties);

            if (ReferenceEquals(defaultVersion, DefaultVersion))
            {
                return s_defaultAssemblyVersion;
            }

            return new Version(defaultVersion.Major, defaultVersion.Minor, Math.Max(defaultVersion.Build, 0), revision: Math.Max(defaultVersion.Revision, 0));
        }
    }
}
