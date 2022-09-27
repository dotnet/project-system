// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    internal static class ISolutionBuildManagerFactory
    {
        public static ISolutionBuildManager ImplementBusy(VSSOLNBUILDUPDATEFLAGS buildFlags = VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_NONE)
        {
            var buildManager = new Mock<ISolutionBuildManager>();

            buildManager.Setup(b => b.QueryBuildManagerBusy())
                .Returns((int)buildFlags);

            buildManager.Setup(b => b.QueryBuildManagerBusyEx())
                .Returns((uint)buildFlags);

            return buildManager.Object;
        }

        public static ISolutionBuildManager Create(
            IVsUpdateSolutionEvents? solutionEventsListener = null,
            IVsHierarchy? hierarchyToBuild = null,
            bool isBuilding = false,
            bool cancelBuild = false)
        {
            var buildManager = new Mock<ISolutionBuildManager>();

            solutionEventsListener ??= IVsUpdateSolutionEventsFactory.Create();
            hierarchyToBuild ??= IVsHierarchyFactory.Create();

            int isBusy = isBuilding ? 1 : 0;
            buildManager.Setup(b => b.QueryBuildManagerBusy())
                .Returns(isBusy);

            if (hierarchyToBuild is not null)
            {
                void onBuildStartedWithReturn(IVsHierarchy[] _, uint[] __, uint ___)
                {
                    solutionEventsListener!.UpdateSolution_Begin(It.IsAny<int>());

                    if (cancelBuild)
                    {
                        solutionEventsListener.UpdateSolution_Cancel();
                    }
                    else
                    {
                        solutionEventsListener.UpdateSolution_Done(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());
                    }
                }

                buildManager.Setup(b => b.StartUpdateSpecificProjectConfigurations(It.IsAny<IVsHierarchy[]>(), It.IsAny<uint[]>(), It.IsAny<uint>()))
                    .Callback((System.Action<IVsHierarchy[], uint[], uint>)onBuildStartedWithReturn);
            }

            return buildManager.Object;
        }
    }
}
