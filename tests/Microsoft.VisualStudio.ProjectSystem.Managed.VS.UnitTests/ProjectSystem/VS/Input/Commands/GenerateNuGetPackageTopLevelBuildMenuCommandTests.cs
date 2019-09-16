// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    public class GenerateNuGetPackageTopLevelBuildMenuCommandTests : AbstractGenerateNuGetPackageCommandTests
    {
        internal override long GetCommandId() => ManagedProjectSystemCommandId.GenerateNuGetPackageTopLevelBuild;

        internal override AbstractGenerateNuGetPackageCommand CreateInstanceCore(
            UnconfiguredProject project,
            IProjectThreadingService threadingService,
            IVsService<SVsSolutionBuildManager, IVsSolutionBuildManager2> vsSolutionBuildManagerService,
            GeneratePackageOnBuildPropertyProvider generatePackageOnBuildPropertyProvider)
        {
            return new GenerateNuGetPackageTopLevelBuildMenuCommand(project, threadingService, vsSolutionBuildManagerService, generatePackageOnBuildPropertyProvider);
        }

        [Fact]
        public async Task GetCommandStatusAsync_RootFolderAsNodes_ReturnsHandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree.Root);

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Handled);
        }

        [Fact]
        public async Task GetCommandStatusAsync_NonRootFolderAsNodes_ReturnsHandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Handled);
        }
    }
}
