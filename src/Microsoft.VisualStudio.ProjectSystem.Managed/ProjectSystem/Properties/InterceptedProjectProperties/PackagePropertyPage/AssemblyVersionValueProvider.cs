// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    [ExportInterceptingPropertyValueProvider(AssemblyVersionPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class AssemblyVersionValueProvider : BaseVersionValueProvider
    {
        private static readonly Version s_DefaultAssemblyVersion = new Version(1, 0, 0, 0);
        private const string AssemblyVersionPropertyName = "AssemblyVersion";

        protected override string PropertyName => AssemblyVersionPropertyName;

        protected async override Task<Version> GetDefaultVersionAsync(IProjectProperties defaultProperties)
        {
            // Default semantic/package version just has 3 fields, we need to append an additional Revision field with value "0".
            var defaultVersion = await base.GetDefaultVersionAsync(defaultProperties).ConfigureAwait(true);
            if (ReferenceEquals(defaultVersion, s_DefaultVersion))
            {
                return s_DefaultAssemblyVersion;
            }

            return new Version(defaultVersion.Major, defaultVersion.Minor, Math.Max(defaultVersion.Build, 0), revision: Math.Max(defaultVersion.Revision, 0));
        }
    }
}
