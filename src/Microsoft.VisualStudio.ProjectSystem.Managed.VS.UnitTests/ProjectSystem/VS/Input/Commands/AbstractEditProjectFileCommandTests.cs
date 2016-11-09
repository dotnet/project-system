// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Text;
using Xunit;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities.ExportFactory;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    [ProjectSystemTrait]
    public class AbstractEditProjectFileCommandTests
    {
        private const long CommandId = VisualStudioStandard97CommandId.SaveProjectItem;
        private static readonly Guid XmlGuid = Guid.Parse("{fa3cd31e-987b-443a-9b81-186104e8dac1}");

        [Fact]
        public async Task AbstractEditProjectFileCommand_ValidNode_ShouldHandle()
        {
            var capabilityChecker = IProjectCapabilitiesServiceFactory.ImplementsContains(CapabilityChecker(true));
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
");

            var nodes = ImmutableHashSet.Create(tree);

            var command = CreateInstance();

            var result = await command.GetCommandStatusAsync(nodes, CommandId, true, "", 0);
            Assert.True(result.Handled);
            Assert.Equal(CommandStatus.Enabled | CommandStatus.Supported, result.Status);
            Assert.Equal($"Edit Root.{EditProjectFileCommand.Extension}", result.CommandText);
        }

        [Fact]
        public async Task AbstractEditProjectFileCommand_NonRootNode_ShouldntHandle()
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
        public async Task AbstractEditProjectFileCommand_NoCapability_ShouldntHandle()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
");

            var nodes = ImmutableHashSet.Create(tree);
            var command = CreateInstance(implementCapabilities: false);

            var result = await command.GetCommandStatusAsync(nodes, CommandId, true, "", 0);
            Assert.False(result.Handled);
            Assert.Equal(CommandStatus.NotSupported, result.Status);
        }

        [Fact]
        public async Task AbstractEditProjectFileCommand_WrongCmdId_ShouldntHandle()
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
        public async Task AbstractEditProjectFileCommand_CorrectNode_CreatesWindowCorrectly()
        {
            var projectPath = $"C:\\Project1\\Project1.{EditProjectFileCommand.Extension}";
            var tempDirectory = "C:\\Temp\\asdf.xyz";
            var tempProjFile = $"{tempDirectory}\\Project1.{EditProjectFileCommand.Extension}";
            var projectXml = @"<Project></Project>";
            var autoOpenSet = false;
            var listenerSet = false;
            IVsWindowFrameNotify2 notifier = null;

            var fileSystem = new IFileSystemMock();
            var textDoc = ITextDocumentFactory.Create();
            var frame = IVsWindowFrameFactory.ImplementShowAndSetProperty(VSConstants.S_OK, (property, obj) =>
            {
                switch (property)
                {
                    case (int)__VSFPROPID5.VSFPROPID_DontAutoOpen:
                        autoOpenSet = true;
                        break;
                    case (int)__VSFPROPID.VSFPROPID_ViewHelper:
                        listenerSet = true;
                        Assert.IsAssignableFrom<IVsWindowFrameNotify2>(obj);
                        notifier = obj as IVsWindowFrameNotify2;
                        break;
                    default:
                        Assert.False(true, $"Unexpected property ID {property}");
                        break;
                }

                return VSConstants.S_OK;
            });

            var command = SetupScenario(projectXml, tempDirectory, tempProjFile, projectPath, fileSystem, textDoc, frame);
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
            Assert.True(listenerSet);
            Assert.NotNull(notifier);

            // Now see if the event correctly saved the text from the buffer into the project file
            var args = new TextDocumentFileActionEventArgs(tempProjFile, DateTime.Now, FileActionTypes.ContentSavedToDisk);
            Mock.Get(textDoc).Raise(t => t.FileActionOccurred += null, args);
            Assert.Equal(projectXml, fileSystem.ReadAllText(projectPath));

            // Finally, ensure the cleanup works as expected. We don't do anything with the passed option. The notifier
            // should remove the file temp file from the filesystem.
            Assert.Equal(VSConstants.S_OK, notifier.OnClose(0));
            Assert.False(fileSystem.DirectoryExists(tempDirectory));
            Assert.False(fileSystem.FileExists(tempProjFile));
        }

        [Theory]
        [InlineData(FileActionTypes.ContentLoadedFromDisk)]
        [InlineData(FileActionTypes.DocumentRenamed)]
        public async Task AbstractEditProjectFileCommand_NonSaveAction_DoesNotOverwriteProjectFile(FileActionTypes actionType)
        {
            var projectPath = $"C:\\Project1\\Project1.{EditProjectFileCommand.Extension}";
            var tempDirectory = "C:\\Temp\\asdf.xyz";
            var tempProjFile = $"{tempDirectory}\\Project1.{EditProjectFileCommand.Extension}";
            var projectXml = @"<Project></Project>";

            var fileSystem = new IFileSystemMock();
            var textDoc = ITextDocumentFactory.Create();
            var frame = IVsWindowFrameFactory.ImplementShowAndSetProperty(VSConstants.S_OK, (prop, obj) => VSConstants.S_OK);

            var command = SetupScenario(projectXml, tempDirectory, tempProjFile, projectPath, fileSystem, textDoc, frame);
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
        public async Task AbstractEditProjectFileCommand_NonRootNode_DoesNotHandle()
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
        public async Task AbstractEditProjectFileCommand_WrongCmdId_DoesNotHandle()
        {
            var capabilityChecker = IProjectCapabilitiesServiceFactory.ImplementsContains(CapabilityChecker(true));
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
");

            var nodes = ImmutableHashSet.Create(tree);

            var command = CreateInstance();

            Assert.False(await command.TryHandleCommandAsync(nodes, 0, true, 0, IntPtr.Zero, IntPtr.Zero));
        }

        [Fact]
        public async Task AbstractEditProjectFileCommand_NoCapability_DoesNotHandle()
        {
            var tree = ProjectTreeParser.Parse(@"
Root (flags: {ProjectRoot})
");

            var nodes = ImmutableHashSet.Create(tree);

            var command = CreateInstance(implementCapabilities: false);

            Assert.False(await command.TryHandleCommandAsync(nodes, CommandId, true, 0, IntPtr.Zero, IntPtr.Zero));
        }

        private EditProjectFileCommand SetupScenario(string projectXml, string tempPath, string tempProjectFile, string projectFile,
            IFileSystemMock fileSystem, ITextDocument textDoc, IVsWindowFrame frame)
        {
            fileSystem.SetTempFile(tempPath);
            var configuredProject = ConfiguredProjectFactory.Create();
            var unconfiguredProject = IUnconfiguredProjectFactory.Create(filePath: projectFile, configuredProject: configuredProject);
            var shellUtilities = new TestShellUtilitiesHelper((sp, path) =>
            {
                Assert.Equal(tempProjectFile, path);
                return Tuple.Create(IVsHierarchyFactory.Create(), (uint)0, IVsPersistDocDataFactory.ImplementAsIVsTextBuffer(), (uint)0);
            }, (sp, path, factoryGuid, logicalView) =>
            {
                Assert.Equal(tempProjectFile, path);
                Assert.Equal(XmlGuid, factoryGuid);
                Assert.Equal(Guid.Empty, logicalView);

                return frame;
            });

            var textBuffer = ITextBufferFactory.ImplementSnapshot(projectXml);
            var editorFactoryService = IVsEditorAdaptersFactoryServiceFactory.ImplementGetDocumentBuffer(textBuffer);
            Mock.Get(textDoc).SetupGet(t => t.TextBuffer).Returns(textBuffer);
            var textDocFactory = ITextDocumentFactoryServiceFactory.ImplementGetTextDocument(textDoc, true);

            var msbuildAccessor = IMsBuildAccessorFactory.Implement(projectXml, async (writeLock, callback) =>
            {
                await callback();
                Assert.True(writeLock);
            });

            var threadingService = IProjectThreadingServiceFactory.Create();

            return CreateInstance(unconfiguredProject, true, msbuildAccessor, fileSystem, textDocFactory, editorFactoryService, threadingService, shellUtilities);
        }

        private EditProjectFileCommand CreateInstance(
            UnconfiguredProject unconfiguredProject = null,
            bool implementCapabilities = true,
            IMsBuildAccessor msbuildAccessor = null,
            IFileSystem fileSystem = null,
            ITextDocumentFactoryService textDocumentService = null,
            IVsEditorAdaptersFactoryService editorAdapterService = null,
            IProjectThreadingService threadingService = null,
            IVsShellUtilitiesHelper shellUtilities = null,
            IExportFactory<MsBuildModelWatcher> watcherFactory = null
            )
        {
            UnitTestHelper.IsRunningUnitTests = true;
            var uProj = unconfiguredProject ?? IUnconfiguredProjectFactory.Create();
            var capabilities = IProjectCapabilitiesServiceFactory.ImplementsContains(CapabilityChecker(implementCapabilities));
            var msbuild = msbuildAccessor ?? IMsBuildAccessorFactory.Create();
            var fs = fileSystem ?? new IFileSystemMock();
            var tds = textDocumentService ?? ITextDocumentFactoryServiceFactory.Create();
            var eas = editorAdapterService ?? IVsEditorAdaptersFactoryServiceFactory.Create();
            var threadServ = threadingService ?? IProjectThreadingServiceFactory.Create();
            var shellUt = shellUtilities ?? new TestShellUtilitiesHelper(
                (sp, path) => Tuple.Create(IVsHierarchyFactory.Create(), (uint)1, IVsPersistDocDataFactory.Create(), (uint)1),
                (sp, path, edType, logView) => IVsWindowFrameFactory.Create());
            var wFact = watcherFactory ?? IExportFactoryMock.CreateInstance<MsBuildModelWatcher>();
            return new EditProjectFileCommand(uProj, capabilities, IServiceProviderFactory.Create(), msbuild, fs, tds, eas, threadServ, shellUt, wFact);
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

    [ProjectCommand(ManagedProjectSystemPackage.ManagedProjectSystemCommandSet, VisualStudioStandard97CommandId.SaveProjectItem)]
    internal class EditProjectFileCommand : AbstractEditProjectFileCommand
    {
        public const string Extension = "proj";

        public EditProjectFileCommand(UnconfiguredProject unconfiguredProject,
            IProjectCapabilitiesService projectCapabilitiesService,
            IServiceProvider serviceProvider,
            IMsBuildAccessor msbuildAccessor,
            IFileSystem fileSystem,
            ITextDocumentFactoryService textDocumentService,
            IVsEditorAdaptersFactoryService editorFactoryService,
            IProjectThreadingService threadingService,
            IVsShellUtilitiesHelper shellUtilities,
            IExportFactory<MsBuildModelWatcher> watcherFactory) :
            base(unconfiguredProject, projectCapabilitiesService, serviceProvider, msbuildAccessor,
                fileSystem, textDocumentService, editorFactoryService, threadingService, shellUtilities, watcherFactory)
        {
        }

        protected override string FileExtension
        {
            get
            {
                return Extension;
            }
        }
    }
}
