// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.VS.Build;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    public class GenerateNuGetPackageProjectContextMenuCommandTests : AbstractGenerateNuGetPackageCommandTests
    {
        internal override long GetCommandId() => ManagedProjectSystemCommandId.GenerateNuGetPackageProjectContextMenu;

        internal override AbstractGenerateNuGetPackageCommand CreateInstanceCore(
            UnconfiguredProject project,
            IProjectThreadingService threadingService,
            ISolutionBuildManager solutionBuildManager,
            GeneratePackageOnBuildPropertyProvider generatePackageOnBuildPropertyProvider)
        {
            return new GenerateNuGetPackageProjectContextMenuCommand(project, threadingService, solutionBuildManager, generatePackageOnBuildPropertyProvider);
        }

        [Fact]
        public async Task GetCommandStatusAsync_RootFolderAsNodes_ReturnsHandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {AppDesignerFolder})
                """);

            var nodes = ImmutableHashSet.Create(tree.Root);

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", 0);

            Assert.True(result.Handled);
        }

        [Fact]
        public async Task GetCommandStatusAsync_NonRootFolderAsNodes_ReturnsUnhandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {AppDesignerFolder})
                """);

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", 0);

            Assert.False(result.Handled);
        }
    }
}
