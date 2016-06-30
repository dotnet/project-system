using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.Mocks;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectSystemTrait]
    public class AddClassProjectCommandTests
    {
        [Fact]
        public void Constructor_NullAsProjectTree_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new AddClassProjectCommand(null, IUnconfiguredProjectVsServicesFactory.Create(),
                SVsServiceProviderFactory.Create()));
        }

        [Fact]
        public void Constructor_NullAsProjectVsService_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new AddClassProjectCommand(IPhysicalProjectTreeFactory.Create(), null,
                SVsServiceProviderFactory.Create()));
        }

        [Fact]
        public void Constructor_NullAsSVsServiceProvider_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new AddClassProjectCommand(IPhysicalProjectTreeFactory.Create(),
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

#pragma warning disable RS0003 // Do not directly await a Task (see https://github.com/dotnet/roslyn/issues/6770)
            var result = await command.GetCommandStatusAsync(nodes, 1, true, "commandText", CommandStatus.Enabled);
#pragma warning restore RS0003 // Do not directly await a Task

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

#pragma warning disable RS0003 // Do not directly await a Task (see https://github.com/dotnet/roslyn/issues/6770)
            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);
#pragma warning restore RS0003 // Do not directly await a Task

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

#pragma warning disable RS0003 // Do not directly await a Task (see https://github.com/dotnet/roslyn/issues/6770)
            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);
#pragma warning restore RS0003 // Do not directly await a Task

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

#pragma warning disable RS0003 // Do not directly await a Task (see https://github.com/dotnet/roslyn/issues/6770)
            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);
#pragma warning restore RS0003 // Do not directly await a Task

            Assert.False(result);
        }

        [Fact]
        public async Task GetCommandStatusAsync_FolderAsNodes_ReturnsHandled()
        {

            var projectProperties = ProjectPropertiesFactory.Create(IUnconfiguredProjectFactory.Create(), new[] {
                    new PropertyPageData { Category = "Project", PropertyName = "ProjectGuid", Value = Guid.NewGuid().ToString() }
                });

            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(""), dlg:
                IVsAddProjectItemDlgFactory.Create(0), properties: () => projectProperties);

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Code (flags: {Folder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

#pragma warning disable RS0003 // Do not directly await a Task (see https://github.com/dotnet/roslyn/issues/6770)
            var result = await command.GetCommandStatusAsync(nodes, GetCommandId(), true, "commandText", (CommandStatus)0);
#pragma warning restore RS0003 // Do not directly await a Task

            Assert.True(result.Handled);
            Assert.Equal("commandText", result.CommandText);
            Assert.Equal(CommandStatus.Enabled | CommandStatus.Supported, result.Status);
        }

        [Fact]
        public async Task TryHandleCommandAsync_FolderAsNodes_ReturnsTrue()
        {
            var projectProperties = ProjectPropertiesFactory.Create(IUnconfiguredProjectFactory.Create(), new[] {
                    new PropertyPageData { Category = "Project", PropertyName = "ProjectGuid", Value = Guid.NewGuid().ToString() }
                });

            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(""), dlg:
                IVsAddProjectItemDlgFactory.Create(0), properties: () => projectProperties);

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Code (flags: {Folder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

#pragma warning disable RS0003 // Do not directly await a Task (see https://github.com/dotnet/roslyn/issues/6770)
            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);
#pragma warning restore RS0003 // Do not directly await a Task

            Assert.True(result);
        }

        [Fact]
        public async Task TryHandleCommandAsync_FolderAsNodes_CallsShowAddProjectItemDlgAsyncWithFilter()
        {
            int callCount = 0;
            string dirFilter = "";
            string templateFilter = "";
            string browseLocations = "";
            Guid g = new Guid();
            string folder = "folderName";

            var dlg = IVsAddProjectItemDlgFactory.ImplementWithParams((id, guid, project, flags, dFilter, tFilter, browseLocs, filter, showAgain) =>
            {
                callCount++;
                dirFilter = dFilter;
                templateFilter = tFilter;
                browseLocations = browseLocs;
                return 0;
            }, g, folder, string.Empty, 0);

            var projectProperties = ProjectPropertiesFactory.Create(IUnconfiguredProjectFactory.Create(), new[] {
                    new PropertyPageData { Category = "Project", PropertyName = "ProjectGuid", Value = g.ToString() }
                });

            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(folder), dlg: dlg, properties: () => projectProperties);

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties (flags: {Folder AppDesignerFolder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

#pragma warning disable RS0003 // Do not directly await a Task (see https://github.com/dotnet/roslyn/issues/6770)
            await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);
#pragma warning restore RS0003 // Do not directly await a Task

            Assert.Equal(1, callCount);
            Assert.Equal("Visual C# Items", dirFilter);
            Assert.Equal("Class", templateFilter);
            Assert.Equal("folderName", browseLocations);
        }

        public async Task TryHandleCommand_FolderAsNodes_ReturnsTrueWhenUserClicksCancel()
        {
            var projectProperties = ProjectPropertiesFactory.Create(IUnconfiguredProjectFactory.Create(), new[] {
                    new PropertyPageData { Category = "Project", PropertyName = "ProjectGuid", Value = Guid.NewGuid().ToString() }
                });

            var command = CreateInstance(provider: IProjectTreeProviderFactory.Create(""), dlg:
                IVsAddProjectItemDlgFactory.Create(VSConstants.OLE_E_PROMPTSAVECANCELLED), properties: () => projectProperties);

            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Code (flags: {Folder})
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

#pragma warning disable RS0003 // Do not directly await a Task (see https://github.com/dotnet/roslyn/issues/6770)
            var result = await command.TryHandleCommandAsync(nodes, GetCommandId(), true, 0, IntPtr.Zero, IntPtr.Zero);
#pragma warning restore RS0003 // Do not directly await a Task

            Assert.True(result);
        }


        internal long GetCommandId() => VisualStudioStandard97CommandId.AddClass;

        internal AddClassProjectCommand CreateInstance(IPhysicalProjectTree projectTree = null, IUnconfiguredProjectVsServices projectVsServices = null, Shell.SVsServiceProvider serviceProvider = null, IProjectTreeProvider provider = null, IVsAddProjectItemDlg dlg = null, Func<ProjectProperties> properties = null)
        {
            projectTree = projectTree ?? IPhysicalProjectTreeFactory.Create(provider);
            projectVsServices = projectVsServices ?? IUnconfiguredProjectVsServicesFactory.Implement(projectProperties: properties);
            serviceProvider = serviceProvider ?? SVsServiceProviderFactory.Create(dlg);

            return new AddClassProjectCommand(projectTree, projectVsServices, serviceProvider);
        }
    }
}
