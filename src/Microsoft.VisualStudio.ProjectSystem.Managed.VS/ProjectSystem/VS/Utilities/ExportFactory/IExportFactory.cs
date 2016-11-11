// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    /// <summary>
    /// This is a wrapper type around ExportFactory to make export factory testable. Because MEF 2 does not support imports of generic types
    /// (ie, it cannot resolve an import of type <![CDATA[ExportFactory<T>]]>), we instead have to create separate classes for each thing we need to use
    /// an ExportFactory for. Test code and suffice by simply using one generic implementation.
    /// </summary>
    internal interface IExportFactory<T>
    {
        T CreateExport();
    }
}
