// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.Shell.Interop
{
    // Named with an _ instead of IVsProjectFactory to avoid collisions with the actual IVsProjectFactory
    // class.
    internal static class IVsProject_Factory
    {
        public static IVsProject4 ImplementIsDocumentInProject(int hr)
        {
            return ImplementIsDocumentInProject(hr, found: 0, itemid: 0);
        }

        public static IVsProject4 ImplementIsDocumentInProject(bool found, uint itemid = 0)
        {
            return ImplementIsDocumentInProject(HResult.OK, found: found ? 1 : 0, itemid: itemid);
        }

        public static IVsProject4 ImplementIsDocumentInProject(int hr, int found, uint itemid)
        {
            var mock = new Mock<IVsProject4>();

            mock.Setup(p => p.IsDocumentInProject(It.IsAny<string>(), out found, It.IsAny<VSDOCUMENTPRIORITY[]>(), out itemid))
                .Returns(hr);

            return mock.Object;
        }

        public static void ImplementOpenItemWithSpecific(this IVsProject4 project, Guid editorType, Guid logicalView, int hr)
        {
            IVsWindowFrame frame;
            var mock = Mock.Get(project);
            mock.Setup(h => h.OpenItemWithSpecific(It.IsAny<uint>(), It.IsAny<uint>(), ref editorType, It.IsAny<string>(), ref logicalView, It.IsAny<IntPtr>(), out frame))
                .Returns(hr);
        }

        public static void ImplementOpenItemWithSpecific(this IVsProject4 project, Guid editorType, Guid logicalView, IVsWindowFrame? frame)
        {
            var mock = Mock.Get(project);
            mock.Setup(h => h.OpenItemWithSpecific(It.IsAny<uint>(), It.IsAny<uint>(), ref editorType, It.IsAny<string>(), ref logicalView, It.IsAny<IntPtr>(), out frame))
                .Returns(0);
        }

        public static void ImplementAddItemWithSpecific(this IVsProject4 project, Func<uint, VSADDITEMOPERATION, string, uint, string[], VSADDRESULT[], int> addItemWithSpecificFunc)
        {
            var mock = Mock.Get(project);
            Guid guidEditorType = Guid.Empty;
            Guid rguidLogicalView = Guid.Empty;
            mock.Setup(h => h.AddItemWithSpecific(It.IsAny<uint>(), It.IsAny<VSADDITEMOPERATION>(), It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<string[]>(), It.IsAny<IntPtr>(), It.IsAny<uint>(), ref guidEditorType, It.IsAny<string>(), ref rguidLogicalView, It.IsAny<VSADDRESULT[]>()))
                .Returns<uint, VSADDITEMOPERATION, string, uint, string[], IntPtr, uint, Guid, string, Guid, VSADDRESULT[]>((itemId, op, itemName, cOpen, arrFiles, handle, flags, editorType, physView, logicalView, result) =>
                {
                    return addItemWithSpecificFunc(itemId, op, itemName, cOpen, arrFiles, result);
                });
        }
    }
}
