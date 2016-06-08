// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsProjectFactory
    {
        public static void ImplementOpenItemWithSpecific(this IVsProject4 project, Guid editorType, Guid logicalView, int hr)
        {
            IVsWindowFrame frame;
            var mock = Mock.Get(project);
            mock.Setup(h => h.OpenItemWithSpecific(It.IsAny<uint>(), It.IsAny<uint>(), ref editorType, It.IsAny<string>(), ref logicalView, It.IsAny<IntPtr>(), out frame))
                .Returns(hr);
        }

        public static void ImplementOpenItemWithSpecific(this IVsProject4 project, Guid editorType, Guid logicalView, IVsWindowFrame frame)
        {
            var mock = Mock.Get(project);
            mock.Setup(h => h.OpenItemWithSpecific(It.IsAny<uint>(), It.IsAny<uint>(), ref editorType, It.IsAny<string>(), ref logicalView, It.IsAny<IntPtr>(), out frame))
                .Returns(0);
        }

        public static void ImplementAddItemWithSpecific(this IVsProject4 project, Func<uint, VSADDITEMOPERATION, string, string[], VSADDRESULT[], int> addItemWithSpecificFunc)
        {
            var mock = Mock.Get(project);
            Guid guidEditorType = Guid.Empty;
            Guid rguidLogicalView = Guid.Empty;
            mock.Setup(h => h.AddItemWithSpecific(It.IsAny<uint>(), It.IsAny<VSADDITEMOPERATION>(), It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<string[]>(), It.IsAny<IntPtr>(), It.IsAny<uint>(), ref guidEditorType, It.IsAny<string>(), ref rguidLogicalView, It.IsAny<VSADDRESULT[]>()))
                .Returns<uint, VSADDITEMOPERATION, string, uint, string[], IntPtr, uint, Guid, string, Guid, VSADDRESULT[]>((itemId, op, itemName, cOpen, arrFiles, handle, flags, editorType, physView, logicalView, result) =>
                {
                    return addItemWithSpecificFunc(itemId, op, itemName, arrFiles, result);
                });
        }
    }
}
