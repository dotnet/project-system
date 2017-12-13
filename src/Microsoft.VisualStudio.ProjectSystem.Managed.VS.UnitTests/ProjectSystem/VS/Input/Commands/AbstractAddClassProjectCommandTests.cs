// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    public abstract class AbstractAddClassProjectCommandTests
    {
        [Fact]
        public void Constructor_NullAsProjectTree_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateInstance(null, IUnconfiguredProjectVsServicesFactory.Create(),
                SVsServiceProviderFactory.Create()));
        }

        [Fact]
        public void Constructor_NullAsProjectVsService_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateInstance(IPhysicalProjectTreeFactory.Create(), null,
                SVsServiceProviderFactory.Create()));
        }

        [Fact]
        public void Constructor_NullAsSVsServiceProvider_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => CreateInstance(IPhysicalProjectTreeFactory.Create(),
                IUnconfiguredProjectVsServicesFactory.Create(), null));
        }

        [Fact]
        public void GetCommandStatusAsync_NullAsNodes_ThrowsArgumentNull()
        {
            var command = CreateInstance();

            Assert.Throws<ArgumentNullException>("nodes", () =>
            {
                command.GetCommandStatusAsync((IImmutableSet<IProjectTree>)null, GetCommandId(), true, "commandText", CommandStatus.Enabled);
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
        public async Task TryHandleCommandAsync_MoreThanOneNodeAsNodes_ReturnsFalse()
        {
            var command = CreateInstance();

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree, tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.False(result);
        }

        [Fact]
        public async Task GetCommandStatusAsync_NonAppDesignerFolderAsNodes_ReturnsUnhandled()
        {
            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(), dlg: IVsAddProjectItemDlgFactory.Create());

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.False(result.Handled);
        }

        [Fact]
        public async Task TryHandleCommandAsync_NonRegularFolderAsNodes_ReturnsFalse()
        {
            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(), dlg: IVsAddProjectItemDlgFactory.Create());

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.False(result);
        }

        [Fact]
        public async Task GetCommandStatusAsync_FolderAsNodes_ReturnsHandled()
        {
            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(""), dlg:
                IVsAddProjectItemDlgFactory.Create(0));

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Code (flags: {Folder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);

            Assert.True(result.Handled);
            Assert.Equal("commandText", result.CommandText);
            Assert.Equal(CommandStatus.Enabled | CommandStatus.Supported, result.Status);
        }

        [Fact]
        public async Task TryHandleCommandAsync_FolderAsNodes_ReturnsTrue()
        {
            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(""), dlg:
                IVsAddProjectItemDlgFactory.Create(0));

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Code (flags: {Folder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);

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

            var projectProperties = ProjectPropertiesFactory.Create(UnconfiguredProjectFactory.Create(), new[] {
                    new PropertyPageData { Category = ConfigurationGeneral.SchemaName, PropertyName = ConfigurationGeneral.ProjectGuidProperty, Value = g.ToString() }
                });

            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(folder), dlg: dlg);

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.Equal(1, callCount);
            Assert.Equal(DirName, dirFilter);
            Assert.Equal(VSResources.ClassTemplateName, templateFilter);
            Assert.Equal("folderName", browseLocations);
        }

        [Fact]
        public async Task TryHandleCommand_FolderAsNodes_ReturnsTrueWhenUserClicksCancel()
        {
            var projectProperties = ProjectPropertiesFactory.Create(UnconfiguredProjectFactory.Create(), new[] {
                    new PropertyPageData { Category = ConfigurationGeneral.SchemaName, PropertyName = ConfigurationGeneral.ProjectGuidProperty, Value = Guid.NewGuid().ToString() }
                });

            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(""), dlg:
                IVsAddProjectItemDlgFactory.Create(VSConstants.OLE_E_PROMPTSAVECANCELLED));

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Code (flags: {Folder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);

            Assert.True(result);
        }


        internal long GetCommandId() => VisualStudioStandard97CommandId.AddClass;

        internal abstract string DirName { get; }

        internal AbstractAddClassProjectCommand CreateInstance(IPhysicalProjectTree projectTree = null, IUnconfiguredProjectVsServices projectVsServices = null, Shell.SVsServiceProvider serviceProvider = null, IProjectTreeProvider provider = null, IVsAddProjectItemDlg dlg = null)
        {
            projectTree = projectTree ?? IPhysicalProjectTreeFactory.Create(provider);
            projectVsServices = projectVsServices ?? IUnconfiguredProjectVsServicesFactory.Implement(threadingServiceCreator: () => IProjectThreadingServiceFactory.Create());
            serviceProvider = serviceProvider ?? SVsServiceProviderFactory.Create(dlg);

            return CreateInstance(projectTree, projectVsServices, serviceProvider);
        }

        internal abstract AbstractAddClassProjectCommand CreateInstance(IPhysicalProjectTree tree, IUnconfiguredProjectVsServices services, Shell.SVsServiceProvider serviceProvider);
    }
}
