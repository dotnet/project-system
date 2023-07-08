// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Shell.Interop;

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
            Action<ImmutableArray<string>> onImplicitBuildStartWithStartupPaths = startupPaths => implicitBuildStartInvoked = true;
            Action onImplicitBuildEndOrCancel = () => implicitBuildEndOrCancelInvoked = true;

            var buildManager = await CreateInitializedInstanceAsync(
                onImplicitBuildStart,
                onImplicitBuildEndOrCancel,
                onImplicitBuildStartWithStartupPaths,
                startDebuggingBuild,
                startWithoutDebuggingBuild);

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

        [Fact]
        public async Task StartupProjectsArePassedThrough()
        {
            ImmutableArray<string> startupProjectPaths = ImmutableArray<string>.Empty;
            Action<ImmutableArray<string>> onImplicitBuildStartWithStartPaths = paths => startupProjectPaths = paths;

            var buildManager = await CreateInitializedInstanceAsync(
                onImplicitBuildStartWithStartupPaths: onImplicitBuildStartWithStartPaths,
                startWithoutDebuggingBuild: true,
                startupProjectFullPaths: ImmutableArray.Create(@"C:\alpha\beta.csproj", @"C:\alpha\gamma.csproj"));

            RunBuild(buildManager, cancelBuild: false);

            Assert.Contains(@"C:\alpha\beta.csproj", startupProjectPaths);
            Assert.Contains(@"C:\alpha\gamma.csproj", startupProjectPaths);
        }

        private static async Task<ImplicitlyTriggeredDebugBuildManager> CreateInitializedInstanceAsync(
            Action? onImplicitBuildStart = null,
            Action? onImplicitBuildEndOrCancel = null,
            Action<ImmutableArray<string>>? onImplicitBuildStartWithStartupPaths = null,
            bool startDebuggingBuild = false,
            bool startWithoutDebuggingBuild = false,
            ImmutableArray<string>? startupProjectFullPaths = null)
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

            var solutionBuildManager = ISolutionBuildManagerFactory.ImplementBusy(buildFlags);

            var instance = new ImplicitlyTriggeredDebugBuildManager(
                IProjectThreadingServiceFactory.Create(),
                solutionBuildManager,
                IImplicitlyTriggeredBuildManagerFactory.Create(onImplicitBuildStart, onImplicitBuildEndOrCancel, onImplicitBuildStartWithStartupPaths),
                IStartupProjectHelperFactory.Create(startupProjectFullPaths ?? ImmutableArray<string>.Empty));
            
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
