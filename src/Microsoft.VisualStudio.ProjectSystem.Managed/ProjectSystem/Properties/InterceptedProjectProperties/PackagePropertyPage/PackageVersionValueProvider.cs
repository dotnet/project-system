// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    [ExportInterceptingPropertyValueProvider(PackageVersionPropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class PackageVersionValueProvider : BaseVersionValueProvider
    {
        private const string PackageVersionPropertyName = "PackageVersion";

        protected override string PropertyName => PackageVersionPropertyName;

        protected override Task<Version> GetDefaultVersionAsync(IProjectProperties defaultProperties)
        {
            return Task.FromResult(s_DefaultVersion);
        }
    }
}
