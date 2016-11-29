// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    class TestShellUtilitiesHelper : IVsShellUtilitiesHelper
    {
        public delegate Tuple<IVsHierarchy, uint, IVsPersistDocData, uint> GetRDTInfoDelegate(IServiceProvider serviceProvider, string fullPath);
        public delegate IVsWindowFrame OpenDocumentWithSpecificEditorDelegate(IServiceProvider provider, string fullPath);

        private readonly GetRDTInfoDelegate _rdtDelegate;
        private readonly OpenDocumentWithSpecificEditorDelegate _openDocDelegate;

        public TestShellUtilitiesHelper(GetRDTInfoDelegate getRDTInfoImpl, OpenDocumentWithSpecificEditorDelegate openDocumentImpl)
        {
            _rdtDelegate = getRDTInfoImpl;
            _openDocDelegate = openDocumentImpl;
        }

        public void GetRDTDocumentInfo(IServiceProvider serviceProvider, string fullPath, out IVsHierarchy hierarchy, out uint itemid, out IVsPersistDocData persistDocData, out uint docCookie)
        {
            var res = _rdtDelegate(serviceProvider, fullPath);
            hierarchy = res.Item1;
            itemid = res.Item2;
            persistDocData = res.Item3;
            docCookie = res.Item4;
        }

        public IVsWindowFrame OpenDocument(IServiceProvider serviceProvider, string fullPath)
        {
            return _openDocDelegate(serviceProvider, fullPath);
        }
    }
}
