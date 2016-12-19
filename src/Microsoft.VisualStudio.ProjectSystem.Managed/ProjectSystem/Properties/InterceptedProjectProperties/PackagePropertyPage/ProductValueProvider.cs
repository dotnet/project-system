// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties.Package
{
    [ExportInterceptingPropertyValueProvider("Product", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class ProductValueProvider : DefaultToAnotherPropertyProvider
    {
        protected sealed override string DelegatedPropertyName => "AssemblyName";
    }
}
