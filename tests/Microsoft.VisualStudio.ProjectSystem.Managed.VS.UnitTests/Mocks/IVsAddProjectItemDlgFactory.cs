// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsAddProjectItemDlgFactory
    {
        public static IVsAddProjectItemDlg Create(int retVal = -1)
        {
            // All parameters are ignored, we just return the specified value.
            return Implement((a, b, c, d, e, f, g, h, i) => retVal);
        }

        public static IVsAddProjectItemDlg Implement(Func<uint, Guid, IVsProject, uint, string, string, string, string, int, int> body)
        {
            var anyGuid = It.IsAny<Guid>();
            var anyLoc = It.IsAny<string>();
            var anyFilt = It.IsAny<string>();
            var anyInt = It.IsAny<int>();
            return ImplementWithParams(body, anyGuid, anyLoc, anyFilt, anyInt);
        }

        public static IVsAddProjectItemDlg ImplementWithParams(Func<uint, Guid, IVsProject, uint, string, string, string, string, int, int> body, Guid g, string locations, string filter, int showAgain)
        {
            var dlg = new Mock<IVsAddProjectItemDlg>();
            dlg.Setup(d => d.AddProjectItemDlg(It.IsAny<uint>(), ref g, It.IsAny<IVsProject>(), It.IsAny<uint>(), It.IsAny<string>(), It.IsAny<string>(),
                ref locations, ref filter, out showAgain)).Returns(body);
            return dlg.Object;
        }
    }
}
