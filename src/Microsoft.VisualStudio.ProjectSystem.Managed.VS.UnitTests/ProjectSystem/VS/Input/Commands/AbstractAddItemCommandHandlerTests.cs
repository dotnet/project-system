// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    public class AbstractAddItemCommandHandlerTests
    {
        [Fact]
        public void GetCommandStatusAsync_NullAsNodes_ThrowsArgumentNull()
        {
            var command = CreateInstance();

            Assert.Throws<ArgumentNullException>("nodes", () =>
            {
                command.GetCommandStatusAsync((IImmutableSet<IProjectTree>)null, TestAddItemCommand.CommandId, true, "commandText", CommandStatus.Enabled);
            });
        }

        [Fact]
        public async Task GetCommandStatusAsync_UnrecognizedCommandIdAsCommandId_ReturnsUnhandled()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
");

            var nodes = ImmutableHashSet.Create(tree);

            var result = await command.GetCommandStatusAsync(nodes, 1, true, "commandText", CommandStatus.Enabled);

            Assert.False(result.Handled);
        }

        [Fact]
        public async Task TryHandleCommandAsync_NonMatchingCapability_ReturnsFalse()
        {
            var command = CreateInstance(capability: "IncorrectCapability");

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
");

            var nodes = ImmutableHashSet.Create(tree);

            var result = await command.GetCommandStatusAsync(nodes, TestAddItemCommand.CommandId, true, "commandText", CommandStatus.Enabled);

            Assert.False(result.Handled);
        }


        [Fact]
        public async Task TryHandleCommandAsync_MoreThanOneNodeAsNodes_ReturnsFalse()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree, tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, TestAddItemCommand.CommandId, true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.False(result);
        }

        [Fact]
        public async Task GetCommandStatusAsync_NonAppDesignerFolderAsNodes_ReturnsUnhandled()
        {
            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(), addItemDialog: IVsAddProjectItemDlgFactory.Create());

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, TestAddItemCommand.CommandId, true, "commandText", (CommandStatus)0);

            Assert.False(result.Handled);
        }

        [Fact]
        public async Task TryHandleCommandAsync_NonRegularFolderAsNodes_ReturnsFalse()
        {
            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(), addItemDialog: IVsAddProjectItemDlgFactory.Create());

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, TestAddItemCommand.CommandId, true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.False(result);
        }

        [Fact]
        public async Task GetCommandStatusAsync_FolderAsNodes_ReturnsHandled()
        {
            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(""), addItemDialog:
                IVsAddProjectItemDlgFactory.Create(0));

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Code (flags: {Folder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, TestAddItemCommand.CommandId, true, "commandText", (CommandStatus)0);

            Assert.True(result.Handled);
            Assert.Equal("commandText", result.CommandText);
            Assert.Equal(CommandStatus.Enabled | CommandStatus.Supported, result.Status);
        }

        [Fact]
        public async Task TryHandleCommandAsync_FolderAsNodes_ReturnsTrue()
        {
            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(""), addItemDialog:
                IVsAddProjectItemDlgFactory.Create(0));

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Code (flags: {Folder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, TestAddItemCommand.CommandId, true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.True(result);
        }

        [Fact]
        public async Task TryHandleCommandAsync_FolderAsNodes_CallsShowAddProjectItemDlgAsyncWithFilter()
        {
            int callCount = 0;
            string dirFilter = "";
            string templateFilter = "";
            string browseLocations = "";
            var g = new Guid();
            string folder = "folderName";

            var dlg = IVsAddProjectItemDlgFactory.ImplementWithParams((id, guid, project, flags, dFilter, tFilter, browseLocs, filter, showAgain) =>
            {
                callCount++;
                dirFilter = dFilter;
                templateFilter = tFilter;
                browseLocations = browseLocs;
                return 0;
            }, g, folder, string.Empty, 0);

            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(folder), addItemDialog: dlg);

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            await command.TryHandleCommandAsync(nodes, TestAddItemCommand.CommandId, true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.Equal(1, callCount);
            Assert.Equal(TestAddItemCommand.ResourceIds.DirName.ToString(), dirFilter);
            Assert.Equal(TestAddItemCommand.ResourceIds.TemplateName.ToString(), templateFilter);
            Assert.Equal("folderName", browseLocations);
        }

        [Fact]
        public async Task TryHandleCommand_FolderAsNodes_ReturnsTrueWhenUserClicksCancel()
        {
            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(""), addItemDialog:
                IVsAddProjectItemDlgFactory.Create(VSConstants.OLE_E_PROMPTSAVECANCELLED));

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Code (flags: {Folder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, TestAddItemCommand.CommandId, true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.True(result);
        }

        internal AbstractAddItemCommandHandler CreateInstance(IPhysicalProjectTree projectTree = null, IUnconfiguredProjectVsServices projectVsServices = null, IProjectTreeProvider provider = null, IVsAddProjectItemDlg addItemDialog = null, string capability = null)
        {
            var configuredProject = ConfiguredProjectFactory.Create(IProjectCapabilitiesScopeFactory.Create(new string[] { capability ?? TestAddItemCommand.Capability }));
            projectTree ??= IPhysicalProjectTreeFactory.Create(provider);
            projectVsServices ??=                 IUnconfiguredProjectVsServicesFactory.Implement(
                    threadingServiceCreator: () => IProjectThreadingServiceFactory.Create()
                );
            var addItemDialogService = IVsUIServiceFactory.Create<SVsAddProjectItemDlg, IVsAddProjectItemDlg>(addItemDialog);

            string result = "DirName";
            var vsShellMock = new Mock<IVsShell>();
            vsShellMock.Setup(x => x.LoadPackageString(ref It.Ref<Guid>.IsAny, (uint)TestAddItemCommand.ResourceIds.DirName, out result)).Returns(0);
            result = "TemplateName";
            vsShellMock.Setup(x => x.LoadPackageString(ref It.Ref<Guid>.IsAny, (uint)TestAddItemCommand.ResourceIds.TemplateName, out result)).Returns(0);

            var vsShellService = IVsUIServiceFactory.Create<SVsShell, IVsShell>(vsShellMock.Object);

            return new TestAddItemCommand(configuredProject, projectTree, projectVsServices, addItemDialogService, vsShellService);
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

            public TestAddItemCommand(ConfiguredProject configuredProject, IPhysicalProjectTree projectTree, IUnconfiguredProjectVsServices projectVsServices, IVsUIService<IVsAddProjectItemDlg> addItemDialog, IVsUIService<SVsShell, IVsShell> vsShell)
                : base(configuredProject, projectTree, projectVsServices, addItemDialog, vsShell)
            {
            }

            protected override ImmutableDictionary<long, ImmutableArray<TemplateDetails>> GetTemplateDetails() => ImmutableDictionary<long, ImmutableArray<TemplateDetails>>.Empty
                .CreateTemplateDetails(CommandId, Capability, Guid.Empty, ResourceIds.DirName, ResourceIds.TemplateName);
        }
    }
}
