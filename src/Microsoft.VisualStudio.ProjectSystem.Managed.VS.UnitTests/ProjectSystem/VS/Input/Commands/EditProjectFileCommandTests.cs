// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Text;
using Xunit;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities.ExportFactory;
using Microsoft.VisualStudio.Packaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectSystemTrait]
    public class EditProjectFileCommandTests
    {
        private const long CommandId = ManagedProjectSystemPackage.EditProjectFileCmdId;
        private const string Extension = "proj";

        [Fact]
        public async Task EditProjectFileCommand_ValidNode_ShouldHandle()
        {
            var capabilityChecker = IProjectCapabilitiesServiceFactory.ImplementsContains(CapabilityChecker(true));
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
");

            var nodes = ImmutableHashSet.Create(tree);

            var unconfiguredProject = IUnconfiguredProjectFactory.Create(filePath: @"C:\Temp\Root\Root.proj");

            var command = CreateInstance(unconfiguredProject: unconfiguredProject);

            var result = await command.GetCommandStatusAsync(nodes, CommandId, true, "", 0);
            Assert.True(result.Handled);
            Assert.Equal(CommandStatus.Enabled | CommandStatus.Supported, result.Status);
            Assert.Equal($"Edit Root.{Extension}", result.CommandText);
        }

        [Fact]
        public async Task EditProjectFileCommand_NonRootNode_ShouldntHandle()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties ()
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var command = CreateInstance();

            var result = await command.GetCommandStatusAsync(nodes, CommandId, true, "", 0);
            Assert.False(result.Handled);
            Assert.Equal(CommandStatus.NotSupported, result.Status);
        }

        [Fact]
        public async Task EditProjectFileCommand_WrongCmdId_ShouldntHandle()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
");

            var nodes = ImmutableHashSet.Create(tree);

            var command = CreateInstance();

            var result = await command.GetCommandStatusAsync(nodes, 0, true, "", 0);
            Assert.False(result.Handled);
            Assert.Equal(CommandStatus.NotSupported, result.Status);
        }

        [Fact]
        public async Task EditProjectFileCommand_CorrectNode_CreatesWindowCorrectly()
        {
            var projectPath = $"C:\\Project1\\Project1.{Extension}";
            var tempDirectory = "C:\\Temp\\asdf.xyz";
            var tempProjFile = $"{tempDirectory}\\Project1.{Extension}";
            var projectXml = @"<Project></Project>";
            var autoOpenSet = false;

            var fileSystem = new IFileSystemMock();
            var textDoc = ITextDocumentFactory.Create();
            var frame = IVsWindowFrameFactory.ImplementShowAndSetProperty(VSConstants.S_OK, (property, obj) =>
            {
                switch (property)
                {
                    case (int)__VSFPROPID5.VSFPROPID_DontAutoOpen:
                        autoOpenSet = true;
                        break;
                    default:
                        Assert.False(true, $"Unexpected property ID {property}");
                        break;
                }

                return VSConstants.S_OK;
            });

            var modelWatcher = IMsBuildModelWatcherFactory.CreateInstance();
            var modelWatcherFactory = IExportFactoryFactory.ImplementCreateValue(() => modelWatcher);

            IVsWindowFrameEvents events = null;
            var uiShell = IVsUIShell7Factory.ImplementAdviseUnadviseWindowEvents(ev =>
            {
                events = ev;
                return 1;
            }, cookie => Assert.Equal(1u, cookie));

            var command = SetupScenario(projectXml, tempDirectory, tempProjFile, projectPath, fileSystem, textDoc, frame, uiShell, modelWatcherFactory);
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
");
            var nodes = ImmutableHashSet.Create(tree);

            // Verify the frame was setup correctly
            Assert.True(await command.TryHandleCommandAsync(nodes, CommandId, true, 0, IntPtr.Zero, IntPtr.Zero));
            Assert.True(fileSystem.DirectoryExists(tempDirectory));
            Assert.Equal(projectXml, fileSystem.ReadAllText(tempProjFile));
            Mock.Get(frame).Verify(f => f.Show());
            Assert.True(autoOpenSet);
            Assert.NotNull(events);

            // Verify that the model watcher was correctly initialized
            Mock.Get(modelWatcher).Verify(m => m.InitializeAsync(tempProjFile, It.IsAny<string>()), Times.Once);
            Mock.Get(modelWatcher).Verify(m => m.Dispose(), Times.Never);

            // Now see if the event correctly saved the text from the buffer into the project file
            var args = new TextDocumentFileActionEventArgs(tempProjFile, DateTime.Now, FileActionTypes.ContentSavedToDisk);
            Mock.Get(textDoc).Raise(t => t.FileActionOccurred += null, args);
            Assert.Equal(projectXml, fileSystem.ReadAllText(projectPath));
            Mock.Get(modelWatcher).Verify(m => m.Dispose(), Times.Never);

            // Finally, ensure the cleanup works as expected. We don't do anything with the passed option. The notifier
            // should remove the file temp file from the filesystem.
            events.OnFrameDestroyed(frame);
            Assert.False(fileSystem.DirectoryExists(tempDirectory));
            Assert.False(fileSystem.FileExists(tempProjFile));

            // Verify that dispose was called on the model watcher when the window was closed
            Mock.Get(modelWatcher).Verify(m => m.Dispose(), Times.Once);
        }

        [Theory]
        [InlineData(FileActionTypes.ContentLoadedFromDisk)]
        [InlineData(FileActionTypes.DocumentRenamed)]
        public async Task EditProjectFileCommand_NonSaveAction_DoesNotOverwriteProjectFile(FileActionTypes actionType)
        {
            var projectPath = $"C:\\Project1\\Project1.{Extension}";
            var tempDirectory = "C:\\Temp\\asdf.xyz";
            var tempProjFile = $"{tempDirectory}\\Project1.{Extension}";
            var projectXml = @"<Project></Project>";

            var fileSystem = new IFileSystemMock();
            var textDoc = ITextDocumentFactory.Create();
            var frame = IVsWindowFrameFactory.ImplementShowAndSetProperty(VSConstants.S_OK, (prop, obj) => VSConstants.S_OK);
            var exportFactory = IExportFactoryFactory.ImplementCreateValue(() => IMsBuildModelWatcherFactory.CreateInstance());
            var uiShell = IVsUIShell7Factory.ImplementAdviseWindowEvents(ev => 1);

            var command = SetupScenario(projectXml, tempDirectory, tempProjFile, projectPath, fileSystem, textDoc, frame, uiShell, exportFactory);
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
");
            var nodes = ImmutableHashSet.Create(tree);

            // Verify the frame was setup correctly
            Assert.True(await command.TryHandleCommandAsync(nodes, CommandId, true, 0, IntPtr.Zero, IntPtr.Zero));

            var args = new TextDocumentFileActionEventArgs(tempProjFile, DateTime.Now, actionType);
            Mock.Get(textDoc).Raise(t => t.FileActionOccurred += null, args);
            Assert.False(fileSystem.FileExists(projectPath));
        }

        [Fact]
        public async Task EditProjectFileCommand_NonRootNode_DoesNotHandle()
        {
            var capabilityChecker = IProjectCapabilitiesServiceFactory.ImplementsContains(CapabilityChecker(true));
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
    Properties ()
");

            var nodes = ImmutableHashSet.Create(tree.Children[0]);

            var command = CreateInstance();

            Assert.False(await command.TryHandleCommandAsync(nodes, CommandId, true, 0, IntPtr.Zero, IntPtr.Zero));
        }

        [Fact]
        public async Task EditProjectFileCommand_WrongCmdId_DoesNotHandle()
        {
            var capabilityChecker = IProjectCapabilitiesServiceFactory.ImplementsContains(CapabilityChecker(true));
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
");

            var nodes = ImmutableHashSet.Create(tree);

            var command = CreateInstance();

            Assert.False(await command.TryHandleCommandAsync(nodes, 0, true, 0, IntPtr.Zero, IntPtr.Zero));
        }

        private EditProjectFileCommand SetupScenario(string projectXml, string tempPath, string tempProjectFile, string projectFile,
            IFileSystemMock fileSystem, ITextDocument textDoc, IVsWindowFrame frame, IVsUIShell7 shellService,
            IExportFactory<IMsBuildModelWatcher> watcherFactory = null)
        {
            fileSystem.SetTempFile(tempPath);
            var configuredProject = ConfiguredProjectFactory.Create();
            var unconfiguredProject = IUnconfiguredProjectFactory.Create(filePath: projectFile, configuredProject: configuredProject);
            var shellUtilities = new TestShellUtilitiesHelper((sp, path) =>
            {
                Assert.Equal(tempProjectFile, path);
                return Tuple.Create(IVsHierarchyFactory.Create(), (uint)0, IVsPersistDocDataFactory.ImplementAsIVsTextBuffer(), (uint)0);
            }, (sp, path) =>
            {
                Assert.Equal(tempProjectFile, path);
                return frame;
            });

            var textBuffer = ITextBufferFactory.ImplementSnapshot(projectXml);
            var editorFactoryService = IVsEditorAdaptersFactoryServiceFactory.ImplementGetDocumentBuffer(textBuffer);
            Mock.Get(textDoc).SetupGet(t => t.TextBuffer).Returns(textBuffer);
            var textDocFactory = ITextDocumentFactoryServiceFactory.ImplementGetTextDocument(textDoc, true);

            var msbuildAccessor = IMsBuildAccessorFactory.ImplementGetProjectXmlRunLocked(projectXml, async (writeLock, callback) =>
            {
                await callback();
                Assert.True(writeLock);
            });

            var threadingService = IProjectThreadingServiceFactory.Create();

            var provider = IServiceProviderFactory.Create(typeof(SVsUIShell), shellService);

            return CreateInstance(unconfiguredProject, msbuildAccessor, fileSystem, textDocFactory,
                editorFactoryService, threadingService, shellUtilities, provider, watcherFactory);
        }

        private EditProjectFileCommand CreateInstance(
            UnconfiguredProject unconfiguredProject = null,
            IMsBuildAccessor msbuildAccessor = null,
            IFileSystem fileSystem = null,
            ITextDocumentFactoryService textDocumentService = null,
            IVsEditorAdaptersFactoryService editorAdapterService = null,
            IProjectThreadingService threadingService = null,
            IVsShellUtilitiesHelper shellUtilities = null,
            IServiceProvider serviceProvider = null,
            IExportFactory<IMsBuildModelWatcher> watcherFactory = null
            )
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var uProj = unconfiguredProject ?? IUnconfiguredProjectFactory.Create();
            var msbuild = msbuildAccessor ?? IMsBuildAccessorFactory.Create();
            var fs = fileSystem ?? new IFileSystemMock();
            var tds = textDocumentService ?? ITextDocumentFactoryServiceFactory.Create();
            var eas = editorAdapterService ?? IVsEditorAdaptersFactoryServiceFactory.Create();
            var threadServ = threadingService ?? IProjectThreadingServiceFactory.Create();
            var shellUt = shellUtilities ?? new TestShellUtilitiesHelper(
                (provider, path) => Tuple.Create(IVsHierarchyFactory.Create(), (uint)1, IVsPersistDocDataFactory.Create(), (uint)1),
                (provider, path) => IVsWindowFrameFactory.Create());
            var sp = serviceProvider ?? IServiceProviderFactory.Create();
            var wFact = watcherFactory ?? IExportFactoryFactory.CreateInstance<IMsBuildModelWatcher>();
            return new EditProjectFileCommand(uProj, sp, msbuild, fs, tds, eas, threadServ, shellUt, wFact);
        }

        private Func<string, bool> CapabilityChecker(bool result)
        {
            // There's only ever one capability that could be checked.
            return (cap) =>
            {
                Assert.Equal("OpenProjectFile", cap);
                return result;
            };
        }
    }
}
