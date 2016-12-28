// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    class TestShellUtilitiesHelper : IVsShellUtilitiesHelper
    {
        public delegate (IVsHierarchy hier, uint id, IVsPersistDocData docData, uint cookie) GetRDTInfoDelegate(IServiceProvider serviceProvider, string fullPath);
        public delegate IVsWindowFrame OpenDocumentWithSpecificEditorDelegate(IServiceProvider provider, string fullPath, Guid editorType, Guid logicalView);

        private readonly GetRDTInfoDelegate _rdtDelegate;
        private readonly OpenDocumentWithSpecificEditorDelegate _openDocDelegate;

        public TestShellUtilitiesHelper() { }

        public TestShellUtilitiesHelper(GetRDTInfoDelegate getRDTInfoImpl, OpenDocumentWithSpecificEditorDelegate openDocumentImpl)
        {
            _rdtDelegate = getRDTInfoImpl;
            _openDocDelegate = openDocumentImpl;
        }

        public void GetRDTDocumentInfo(IServiceProvider serviceProvider, string fullPath, out IVsHierarchy hierarchy, out uint itemid, out IVsPersistDocData persistDocData, out uint docCookie) =>
            (hierarchy, itemid, persistDocData, docCookie) = _rdtDelegate(serviceProvider, fullPath);

        public IVsWindowFrame OpenDocumentWithSpecificEditor(IServiceProvider serviceProvider, string fullPath, Guid editorType, Guid logicalView) =>
            _openDocDelegate(serviceProvider, fullPath, editorType, logicalView);
    }
}
