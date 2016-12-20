// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    [ExportInterceptingPropertyValueProvider("Authors", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class AuthorsValueProvider : DefaultToAnotherPropertyProvider
    {
        protected sealed override string DelegatedPropertyName => "AssemblyName";
    }
}
