// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IVsSolutionBuildManager2Factory
    {
        public static IVsSolutionBuildManager2 Create(
            IVsUpdateSolutionEvents? solutionEventsListener = null,
            IVsHierarchy? hierarchyToBuild = null,
            bool isBuilding = false,
            bool cancelBuild = false)
        {
            var buildManager = new Mock<IVsSolutionBuildManager2>();

            solutionEventsListener ??= IVsUpdateSolutionEventsFactory.Create();
            hierarchyToBuild ??= IVsHierarchyFactory.Create();

            int isBusy = isBuilding ? 1 : 0;
            buildManager.Setup(b => b.QueryBuildManagerBusy(out isBusy))
                .Returns(VSConstants.S_OK);

            if (hierarchyToBuild != null)
            {
                int onBuildStartedWithReturn()
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

                    return VSConstants.S_OK;
                }

                buildManager.Setup(b => b.StartUpdateSpecificProjectConfigurations(It.IsAny<uint>(), It.IsAny<IVsHierarchy[]>(), It.IsAny<IVsCfg[]>(), It.IsAny<uint[]>(), It.IsAny<uint[]>(), It.IsAny<uint[]>(), It.IsAny<uint>(), It.IsAny<int>()))
                    .Returns(onBuildStartedWithReturn);
            }

            return buildManager.Object;
        }
    }
}
