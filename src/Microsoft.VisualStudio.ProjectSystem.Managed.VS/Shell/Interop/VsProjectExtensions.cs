// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Shell.Interop
{
    /// <summary>
    ///     Provides extension methods for <see cref="IVsProject"/> instances.
    /// </summary>
    internal static class VsProjectExtensions
    {
        /// <summary>
        ///     Returns the <see cref="HierarchyId"/> of the given document moniker, or
        ///     <see cref="HierarchyId.Nil"/> if the document moniker is not part of the project.
        /// </summary>
        public static HierarchyId GetHierarchyId(this IVsProject project, string documentMoniker)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNullOrEmpty(documentMoniker, nameof(documentMoniker));

            var priority = new VSDOCUMENTPRIORITY[1];
            Verify.HResult(project.IsDocumentInProject(documentMoniker, out int isFound, priority, out uint itemId));

            // We only return items that are actually part of the project. CPS returns non-member from this API.
            if (isFound == 0 || (priority[0] != VSDOCUMENTPRIORITY.DP_Standard && priority[0] != VSDOCUMENTPRIORITY.DP_Intrinsic))
                return HierarchyId.Nil;

            HierarchyId id = itemId;

            Assumes.False(id.IsNilOrEmpty);

            return id;
        }

        /// <summary>
        ///     Opens the specified item with the specified editor using the primary logical view.
        /// </summary>
        /// <returns>
        ///     The <see cref="IVsWindowFrame"/> that contains the editor; otherwise, <see langword="null"/> if it was opened
        ///     with an editor external of Visual Studio.
        /// </returns>
        public static IVsWindowFrame? OpenItemWithSpecific(this IVsProject4 project, HierarchyId id, Guid editorType)
        {
            Requires.NotNull(project, nameof(project));

            Verify.HResult(project.OpenItemWithSpecific(id, 0, ref editorType, "", VSConstants.LOGVIEWID_Primary, (IntPtr)(-1), out IVsWindowFrame frame));

            // NOTE: frame is 'null' when opened in an external editor
            return frame;
        }
    }
}
