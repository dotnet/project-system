// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    public class ImplicitlyTriggeredDebugBuildManagerTests
    {
        [Theory, CombinatorialData]
        public async Task TestImplicitlySkipAnalyzersForDebugBuilds(
            bool startDebuggingBuild,
            bool startWithoutDebuggingBuild,
            bool cancelBuild)
        {
            var implicitBuildStartInvoked = false;
            var implicitBuildEndOrCancelInvoked = false;
            Action onImplicitBuildStart = () => implicitBuildStartInvoked = true;
            Action onImplicitBuildEndOrCancel = () => implicitBuildEndOrCancelInvoked = true;

            var buildManager = await CreateInitializedInstanceAsync(
                onImplicitBuildStart, onImplicitBuildEndOrCancel, startDebuggingBuild, startWithoutDebuggingBuild);

            RunBuild(buildManager, cancelBuild);
            
            if ((startDebuggingBuild || startWithoutDebuggingBuild))
            {
                Assert.True(implicitBuildStartInvoked);
                Assert.True(implicitBuildEndOrCancelInvoked);
            }
            else
            {
                Assert.False(implicitBuildStartInvoked);
                Assert.False(implicitBuildEndOrCancelInvoked);
            }
        }

        private static async Task<ImplicitlyTriggeredDebugBuildManager> CreateInitializedInstanceAsync(
            Action? onImplicitBuildStart = null,
            Action? onImplicitBuildEndOrCancel = null,
            bool startDebuggingBuild = false,
            bool startWithoutDebuggingBuild = false)
        {
            var buildFlags = VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_NONE;
            if (startDebuggingBuild)
            {
                buildFlags |= VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_LAUNCHDEBUG;
            }

            if (startWithoutDebuggingBuild)
            {
                buildFlags |= VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_LAUNCH;
            }

            var solutionBuildManager = IVsSolutionBuildManager3Factory.Create(buildFlags: buildFlags);
            var serviceProvider = IVsServiceFactory.Create<SVsSolutionBuildManager, IVsSolutionBuildManager3>(solutionBuildManager);

            var instance = new ImplicitlyTriggeredDebugBuildManager(
                IProjectThreadingServiceFactory.Create(),
                serviceProvider,
                IImplicitlyTriggeredBuildManagerFactory.Create(onImplicitBuildStart, onImplicitBuildEndOrCancel));
            
            await instance.LoadAsync();

            return instance;
        }

        private static void RunBuild(ImplicitlyTriggeredDebugBuildManager buildManager, bool cancelBuild)
        {
            int discard = 0;
            buildManager.UpdateSolution_Begin(ref discard);

            if (cancelBuild)
            {
                buildManager.UpdateSolution_Cancel();
            }
            else
            {
                buildManager.UpdateSolution_Done(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>());
            }
        }
    }
}
