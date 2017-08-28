// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectSystemTrait]
    public class GenerateNuGetPackageProjectContextMenuCommandTests : AbstractGenerateNuGetPackageCommandTests
    {
        internal override long GetCommandId() => ManagedProjectSystemPackage.GenerateNuGetPackageProjectContextMenuCmdId;

        internal override AbstractGenerateNuGetPackageCommand CreateInstanceCore(
            UnconfiguredProject unconfiguredProject,
            IProjectThreadingService threadingService,
            Shell.SVsServiceProvider serviceProvider,
            GeneratePackageOnBuildPropertyProvider generatePackageOnBuildPropertyProvider)
        {
            return new GenerateNuGetPackageProjectContextMenuCommand(unconfiguredProject, threadingService, serviceProvider, generatePackageOnBuildPropertyProvider);
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
        public async Task GetCommandStatusAsync_NonRootFolderAsNodes_ReturnsUnhandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.False(result.Handled);
        }
    }
}
