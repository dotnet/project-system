// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    public abstract class AbstractOpenProjectDesignerCommandTests
    {
        [Fact]
        public void GetCommandStatusAsync_NullAsNodes_ThrowsArgumentNull()
        {
            var command = CreateInstance();

            Assert.ThrowsAsync<ArgumentNullException>("nodes", () =>
            {
                return command.GetCommandStatusAsync(null!, GetCommandId(), true, "commandText", CommandStatus.Enabled);
            });
        }

        [Fact]
        public async Task GetCommandStatusAsync_UnrecognizedCommandIdAsCommandId_ReturnsUnhandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {Folder AppDesignerFolder})
                """);

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, 1, true, "commandText", CommandStatus.Enabled);

            Assert.False(result.Handled);
        }

        [Fact]
        public async Task TryHandleCommandAsync_UnrecognizedCommandIdAsCommandId_ReturnsFalse()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {Folder AppDesignerFolder})
                """);

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, 1, true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.False(result);
        }

        [Fact]
        public async Task GetCommandStatusAsync_MoreThanOneNodeAsNodes_ReturnsUnhandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {Folder AppDesignerFolder})
                """);

            var nodes = ImmutableHashSet.Create(tree, tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", 0);

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

            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.False(result);
        }

        [Fact]
        public async Task GetCommandStatusAsync_NonAppDesignerFolderAsNodes_ReturnsUnhandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {Folder})
                """);

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", 0);

            Assert.False(result.Handled);
        }

        [Fact]
        public async Task TryHandleCommandAsync_NonAppDesignerFolderAsNodes_ReturnsFalse()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {Folder})
                """);

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.False(result);
        }

        [Fact]
        public async Task GetCommandStatusAsync_AppDesignerFolderAsNodes_ReturnsHandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {Folder AppDesignerFolder})
                """);

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", 0);

            Assert.True(result.Handled);
            Assert.Equal("commandText", result.CommandText);
            Assert.Equal(CommandStatus.Enabled | CommandStatus.Supported, result.Status);
        }

        [Fact]
        public async Task TryHandleCommandAsync_AppDesignerFolderAsNodes_ReturnsTrue()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {Folder AppDesignerFolder})
                """);

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.True(result);
        }

        [Fact]
        public async Task TryHandleCommandAsync_AppDesignerFolderAsNodes_CallsShowProjectDesignerAsync()
        {
            int callCount = 0;
            var designerService = IProjectDesignerServiceFactory.ImplementShowProjectDesignerAsync(() => { callCount++; });

            var command = CreateInstance(designerService);

            var tree = ProjectTreeParser.Parse(
                """
                Root (flags: {ProjectRoot})
                    Properties (flags: {Folder AppDesignerFolder})
                """);

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.Equal(1, callCount);
        }

        internal abstract long GetCommandId();

        internal abstract AbstractOpenProjectDesignerCommand CreateInstance(IProjectDesignerService? designerService = null);
    }
}
