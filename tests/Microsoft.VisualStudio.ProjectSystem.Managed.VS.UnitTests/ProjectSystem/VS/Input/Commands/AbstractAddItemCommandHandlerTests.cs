// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    public class AbstractAddItemCommandHandlerTests
    {
        [Fact]
        public void GetCommandStatusAsync_NullAsNodes_ThrowsArgumentNull()
        {
            var command = CreateInstance();

            Assert.ThrowsAsync<ArgumentNullException>("nodes", () =>
            {
                return command.GetCommandStatusAsync(null!, TestAddItemCommand.CommandId, true, "commandText", CommandStatus.Enabled);
            });
        }

        [Fact]
        public async Task GetCommandStatusAsync_UnrecognizedCommandIdAsCommandId_ReturnsUnhandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                """);

            var nodes = ImmutableHashSet.Create(tree);

            var result = await command.GetCommandStatusAsync(nodes, 1, true, "commandText", CommandStatus.Enabled);

            Assert.False(result.Handled);
        }

        [Fact]
        public async Task TryHandleCommandAsync_NonMatchingCapability_ReturnsFalse()
        {
            var command = CreateInstance(capability: "IncorrectCapability");

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                """);

            var nodes = ImmutableHashSet.Create(tree);

            var result = await command.GetCommandStatusAsync(nodes, TestAddItemCommand.CommandId, true, "commandText", CommandStatus.Enabled);

            Assert.False(result.Handled);
        }

        [Fact]
        public async Task TryHandleCommandAsync_MoreThanOneNodeAsNodes_ReturnsFalse()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {Folder AppDesignerFolder})
                """);

            var nodes = ImmutableHashSet.Create(tree, tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, TestAddItemCommand.CommandId, true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.False(result);
        }

        [Fact]
        public async Task GetCommandStatusAsync_FolderAsNodes_ReturnsHandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Code (flags: {Folder})
                """);

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, TestAddItemCommand.CommandId, true, "commandText", 0);

            Assert.True(result.Handled);
            Assert.Equal("commandText", result.CommandText);
            Assert.Equal(CommandStatus.Enabled | CommandStatus.Supported, result.Status);
        }

        [Fact]
        public async Task TryHandleCommandAsync_FolderAsNodes_ReturnsTrue()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Code (flags: {Folder})
                """);

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, TestAddItemCommand.CommandId, true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.True(result);
        }

        [Fact]
        public async Task TryHandleCommandAsync_FolderAsNodes_CallsShowAddProjectItemDlgAsyncWithFilter()
        {
            int callCount = 0;
            string localizedDirectoryNameResult = "";
            string localizedTemplateNameResult = "";
            IProjectTree? nodeResult = null;

            var addItemDialogService = IAddItemDialogServiceFactory.ImplementShowAddNewItemDialogAsync((node, localizedDirectoryName, localizedTemplateName) =>
            {
                callCount++;
                nodeResult = node;
                localizedDirectoryNameResult = localizedDirectoryName;
                localizedTemplateNameResult = localizedTemplateName;
                return true;
            });

            var command = CreateInstance(addItemDialogService: addItemDialogService);

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {Folder AppDesignerFolder})
                """);

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            await command.TryHandleCommandAsync(nodes, TestAddItemCommand.CommandId, true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.Equal(1, callCount);
            Assert.Equal(nameof(TestAddItemCommand.ResourceIds.DirName), localizedDirectoryNameResult);
            Assert.Equal(nameof(TestAddItemCommand.ResourceIds.TemplateName), localizedTemplateNameResult);
            Assert.Same(tree.Children[0], nodeResult);
        }

        [Fact]
        public async Task TryHandleCommand_FolderAsNodes_ReturnsTrueWhenUserClicksCancel()
        {
            var addItemDialogService = IAddItemDialogServiceFactory.ImplementShowAddNewItemDialogAsync((node, localizedDirectoryName, localizedTemplateName) => false);

            var command = CreateInstance(addItemDialogService);

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Code (flags: {Folder})
                """);

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, TestAddItemCommand.CommandId, true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.True(result);
        }

        internal static AbstractAddItemCommandHandler CreateInstance(
            IAddItemDialogService? addItemDialogService = null,
            string? capability = null)
        {
            var configuredProject = ConfiguredProjectFactory.Create(IProjectCapabilitiesScopeFactory.Create(new string[] { capability ?? TestAddItemCommand.Capability }));
            addItemDialogService ??= IAddItemDialogServiceFactory.Create();

            string result = "DirName";
            var vsShellMock = new Mock<IVsShell>();
            vsShellMock.Setup(x => x.LoadPackageString(ref It.Ref<Guid>.IsAny, (uint)TestAddItemCommand.ResourceIds.DirName, out result)).Returns(0);
            result = "TemplateName";
            vsShellMock.Setup(x => x.LoadPackageString(ref It.Ref<Guid>.IsAny, (uint)TestAddItemCommand.ResourceIds.TemplateName, out result)).Returns(0);

            var vsShellService = IVsUIServiceFactory.Create<SVsShell, IVsShell>(vsShellMock.Object);

            return new TestAddItemCommand(configuredProject, addItemDialogService, vsShellService);
        }

        private class TestAddItemCommand : AbstractAddItemCommandHandler
        {
            public const long CommandId = 150;
            public const string Capability = "Capability";

            public enum ResourceIds
            {
                DirName = 523,
                TemplateName = 2014
            }

            public TestAddItemCommand(ConfiguredProject configuredProject, IAddItemDialogService addItemDialogService, IVsUIService<SVsShell, IVsShell> vsShell)
                : base(configuredProject, addItemDialogService, vsShell)
            {
            }

            protected override ImmutableDictionary<long, ImmutableArray<TemplateDetails>> GetTemplateDetails() => ImmutableDictionary<long, ImmutableArray<TemplateDetails>>.Empty
                .CreateTemplateDetails(CommandId, Capability, Guid.Empty, ResourceIds.DirName, ResourceIds.TemplateName);
        }
    }
}
