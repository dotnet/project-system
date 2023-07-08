// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.VS.Build;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    public abstract class AbstractGenerateNuGetPackageCommandTests
    {
        [Fact]
        public void Constructor_NullAsUnconfiguredProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateInstanceCore(null!, IProjectThreadingServiceFactory.Create(), ISolutionBuildManagerFactory.Create(), CreateGeneratePackageOnBuildPropertyProvider()));
        }

        [Fact]
        public void Constructor_NullAsProjectThreadingServiceFactory_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateInstanceCore(UnconfiguredProjectFactory.Create(), null!, ISolutionBuildManagerFactory.Create(), CreateGeneratePackageOnBuildPropertyProvider()));
        }

        [Fact]
        public void Constructor_NullAsSVsServiceProvider_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateInstanceCore(UnconfiguredProjectFactory.Create(), IProjectThreadingServiceFactory.Create(), null!, CreateGeneratePackageOnBuildPropertyProvider()));
        }

        [Fact]
        public void Constructor_NullAsGeneratePackageOnBuildPropertyProvider_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateInstanceCore(UnconfiguredProjectFactory.Create(), IProjectThreadingServiceFactory.Create(), ISolutionBuildManagerFactory.Create(), null!));
        }

        [Fact]
        public async Task TryHandleCommandAsync_InvokesBuild()
        {
            bool buildStarted = false, buildCancelled = false, buildCompleted = false;

            void onUpdateSolutionBegin() => buildStarted = true;
            void onUpdateSolutionCancel() => buildCancelled = true;
            void onUpdateSolutionDone() => buildCompleted = true;

            var solutionEventsListener = IVsUpdateSolutionEventsFactory.Create(onUpdateSolutionBegin, onUpdateSolutionCancel, onUpdateSolutionDone);
            var command = CreateInstance(solutionEventsListener: solutionEventsListener);

            var tree = ProjectTreeParser.Parse("Root (flags: {ProjectRoot})");

            var nodes = ImmutableHashSet.Create(tree.Root);

            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.True(result);
            Assert.True(buildStarted);
            Assert.True(buildCompleted);
            Assert.False(buildCancelled);
        }

        [Fact]
        public async Task TryHandleCommandAsync_OnBuildCancelled()
        {
            bool buildStarted = false, buildCancelled = false, buildCompleted = false;

            void onUpdateSolutionBegin() => buildStarted = true;
            void onUpdateSolutionCancel() => buildCancelled = true;
            void onUpdateSolutionDone() => buildCompleted = true;

            var solutionEventsListener = IVsUpdateSolutionEventsFactory.Create(onUpdateSolutionBegin, onUpdateSolutionCancel, onUpdateSolutionDone);
            var command = CreateInstance(solutionEventsListener: solutionEventsListener, cancelBuild: true);

            var tree = ProjectTreeParser.Parse("Root (flags: {ProjectRoot})");

            var nodes = ImmutableHashSet.Create(tree.Root);

            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.True(result);
            Assert.True(buildStarted);
            Assert.False(buildCompleted);
            Assert.True(buildCancelled);
        }

        [Fact]
        public async Task GetCommandStatusAsync_BuildInProgress()
        {
            var tree = ProjectTreeParser.Parse("Root (flags: {ProjectRoot})");

            var nodes = ImmutableHashSet.Create(tree.Root);

            // Command is enabled if there is no build in progress.
            var command = CreateInstance(isBuilding: false);
            var results = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", 0);
            Assert.True(results.Handled);
            Assert.Equal(CommandStatus.Enabled | CommandStatus.Supported, results.Status);

            // Command is disabled if there is build in progress.
            command = CreateInstance(isBuilding: true);
            results = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", 0);
            Assert.True(results.Handled);
            Assert.Equal(CommandStatus.Supported, results.Status);
        }

        [Fact]
        public async Task TryHandleCommandAsync_BuildInProgress()
        {
            var tree = ProjectTreeParser.Parse("Root (flags: {ProjectRoot})");

            var nodes = ImmutableHashSet.Create(tree.Root);

            bool buildStarted = false, buildCancelled = false, buildCompleted = false;

            void onUpdateSolutionBegin() => buildStarted = true;
            void onUpdateSolutionCancel() => buildCancelled = true;
            void onUpdateSolutionDone() => buildCompleted = true;

            var solutionEventsListener = IVsUpdateSolutionEventsFactory.Create(onUpdateSolutionBegin, onUpdateSolutionCancel, onUpdateSolutionDone);

            var command = CreateInstance(solutionEventsListener: solutionEventsListener, isBuilding: true);

            // Ensure we handle the command, but don't invoke build as there is a build already in progress.
            var handled = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);
            Assert.True(handled);
            Assert.False(buildStarted);
            Assert.False(buildCompleted);
            Assert.False(buildCancelled);
        }

        internal abstract long GetCommandId();

        internal AbstractGenerateNuGetPackageCommand CreateInstance(
            GeneratePackageOnBuildPropertyProvider? generatePackageOnBuildPropertyProvider = null,
            ISolutionBuildManager? solutionBuildManager = null,
            IVsUpdateSolutionEvents? solutionEventsListener = null,
            bool isBuilding = false,
            bool cancelBuild = false)
        {
            var hierarchy = IVsHierarchyFactory.Create();
            var project = UnconfiguredProjectFactory.Create(hierarchy);
            var threadingService = IProjectThreadingServiceFactory.Create();
            solutionBuildManager ??= ISolutionBuildManagerFactory.Create(solutionEventsListener, hierarchy, isBuilding, cancelBuild);
            generatePackageOnBuildPropertyProvider ??= CreateGeneratePackageOnBuildPropertyProvider();

            return CreateInstanceCore(project, threadingService, solutionBuildManager, generatePackageOnBuildPropertyProvider);
        }

        private static GeneratePackageOnBuildPropertyProvider CreateGeneratePackageOnBuildPropertyProvider(IProjectService? projectService = null)
        {
            projectService ??= IProjectServiceFactory.Create();
            return new GeneratePackageOnBuildPropertyProvider(projectService);
        }

        internal abstract AbstractGenerateNuGetPackageCommand CreateInstanceCore(
            UnconfiguredProject project,
            IProjectThreadingService threadingService,
            ISolutionBuildManager solutionBuildManager,
            GeneratePackageOnBuildPropertyProvider generatePackageOnBuildPropertyProvider);
    }
}
