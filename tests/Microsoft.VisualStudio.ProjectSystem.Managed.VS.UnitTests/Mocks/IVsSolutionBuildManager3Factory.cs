// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsSolutionBuildManager3Factory
    {
        public static IVsSolutionBuildManager3 Create(
            IVsUpdateSolutionEvents3? solutionEventsListener = null,
            VSSOLNBUILDUPDATEFLAGS? buildFlags = null)
        {
            var buildManager = new Mock<IVsSolutionBuildManager3>();

            solutionEventsListener ??= IVsUpdateSolutionEvents3Factory.Create();
            
            var flags = (uint)(buildFlags ?? VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_NONE);
            buildManager.Setup(b => b.QueryBuildManagerBusyEx(out flags))
                .Returns(VSConstants.S_OK);

            return buildManager.Object;
        }
    }
}
