// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [ProjectSystemTrait]
    public class EditorStateModelTests
    {
        private static readonly Guid XmlFactoryGuid = Guid.Parse("{fa3cd31e-987b-443a-9b81-186104e8dac1}");

        [Fact]
        public void EditorStateModel_NullThreadingService_Throws()
        {
            Assert.Throws<ArgumentNullException>("threadingService", () => new EditorStateModel(
                null,
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                new TestShellUtilitiesHelper(),
                ExportFactoryFactory.CreateInstance<IProjectFileModelWatcher>(),
                ExportFactoryFactory.CreateInstance<ITextBufferStateListener>(),
                ExportFactoryFactory.CreateInstance<IFrameOpenCloseListener>(),
                ExportFactoryFactory.CreateInstance<ITextBufferManager>()));
        }

        [Fact]
        public void EditorStateModel_NullUnconfiguredProject_Throws()
        {
            Assert.Throws<ArgumentNullException>("unconfiguredProject", () => new EditorStateModel(
                IProjectThreadingServiceFactory.Create(),
                null,
                IServiceProviderFactory.Create(),
                new TestShellUtilitiesHelper(),
                ExportFactoryFactory.CreateInstance<IProjectFileModelWatcher>(),
                ExportFactoryFactory.CreateInstance<ITextBufferStateListener>(),
                ExportFactoryFactory.CreateInstance<IFrameOpenCloseListener>(),
                ExportFactoryFactory.CreateInstance<ITextBufferManager>()));
        }

        [Fact]
        public void EditorStateModel_NullServiceProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>("serviceProvider", () => new EditorStateModel(
                IProjectThreadingServiceFactory.Create(),
                UnconfiguredProjectFactory.Create(),
                null,
                new TestShellUtilitiesHelper(),
                ExportFactoryFactory.CreateInstance<IProjectFileModelWatcher>(),
                ExportFactoryFactory.CreateInstance<ITextBufferStateListener>(),
                ExportFactoryFactory.CreateInstance<IFrameOpenCloseListener>(),
                ExportFactoryFactory.CreateInstance<ITextBufferManager>()));
        }

        [Fact]
        public void EditorStateModel_NullShellHelper_Throws()
        {
            Assert.Throws<ArgumentNullException>("shellHelper", () => new EditorStateModel(
                IProjectThreadingServiceFactory.Create(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                null,
                ExportFactoryFactory.CreateInstance<IProjectFileModelWatcher>(),
                ExportFactoryFactory.CreateInstance<ITextBufferStateListener>(),
                ExportFactoryFactory.CreateInstance<IFrameOpenCloseListener>(),
                ExportFactoryFactory.CreateInstance<ITextBufferManager>()));
        }

        [Fact]
        public void EditorStateModel_NullProjectFileModelWatcherFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>("projectFileModelWatcherFactory", () => new EditorStateModel(
                IProjectThreadingServiceFactory.Create(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                new TestShellUtilitiesHelper(),
                null,
                ExportFactoryFactory.CreateInstance<ITextBufferStateListener>(),
                ExportFactoryFactory.CreateInstance<IFrameOpenCloseListener>(),
                ExportFactoryFactory.CreateInstance<ITextBufferManager>()));
        }

        [Fact]
        public void EditorStateModel_NullTextBufferListenerFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>("textBufferListenerFactory", () => new EditorStateModel(
                IProjectThreadingServiceFactory.Create(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                new TestShellUtilitiesHelper(),
                ExportFactoryFactory.CreateInstance<IProjectFileModelWatcher>(),
                null,
                ExportFactoryFactory.CreateInstance<IFrameOpenCloseListener>(),
                ExportFactoryFactory.CreateInstance<ITextBufferManager>()));
        }

        [Fact]
        public void EditorStateModel_NullFrameEventsListenerFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>("frameEventsListenerFactory", () => new EditorStateModel(
                IProjectThreadingServiceFactory.Create(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                new TestShellUtilitiesHelper(),
                ExportFactoryFactory.CreateInstance<IProjectFileModelWatcher>(),
                ExportFactoryFactory.CreateInstance<ITextBufferStateListener>(),
                null,
                ExportFactoryFactory.CreateInstance<ITextBufferManager>()));
        }

        [Fact]
        public void EditorStateModel_NullTextBufferManagerFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>("textBufferManagerFactory", () => new EditorStateModel(
                IProjectThreadingServiceFactory.Create(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                new TestShellUtilitiesHelper(),
                ExportFactoryFactory.CreateInstance<IProjectFileModelWatcher>(),
                ExportFactoryFactory.CreateInstance<ITextBufferStateListener>(),
                ExportFactoryFactory.CreateInstance<IFrameOpenCloseListener>(),
                null));
        }

        [Fact]
        public async Task EditorStateModel_OpenEditor_SetsUpListeners()
        {
            var filePath = @"C:\Temp\ConsoleApp1.csproj";
            var textBufferManager = ITextBufferManagerFactory.ImplementFilePath(filePath);
            var textBufferManagerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferManager);

            var textBufferListener = ITextBufferStateListenerFactory.Create();
            var textBufferListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferListener);

            var frameListener = IFrameOpenCloseListenerFactory.Create();
            var frameListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => frameListener);

            var projectFileWatcher = IProjectFileModelWatcherFactory.Create();
            var projectFileWatcherFactory = ExportFactoryFactory.ImplementCreateValue(() => projectFileWatcher);

            var windowFrame = IVsWindowFrameFactory.Create();
            var shellUtilities = new TestShellUtilitiesHelper((provider, fullPath) => (null, 0, null, 0), (provider, path, editorType, logicalView) =>
            {
                Assert.Equal(path, filePath);
                Assert.Equal(XmlFactoryGuid, editorType);
                Assert.Equal(Guid.Empty, logicalView);
                return windowFrame;
            });

            var editorState = new EditorStateModelTester(
                new IProjectThreadingServiceMock(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                shellUtilities,
                projectFileWatcherFactory,
                textBufferListenerFactory,
                frameListenerFactory,
                textBufferManagerFactory);

            // Mock textBufferManager.InitializeBufferAsync so it can verify the editor is actually in Initializing
            Mock.Get(textBufferManager).Setup(t => t.InitializeBufferAsync()).Callback(() =>
                Assert.Equal(EditorStateModel.EditorState.Initializing, editorState.CurrentState)).Returns(Task.CompletedTask);

            await editorState.OpenEditorAsync();
            Mock.Get(textBufferManager).Verify(t => t.InitializeBufferAsync(), Times.Once);
            Mock.Get(textBufferListener).Verify(t => t.InitializeListenerAsync(filePath), Times.Once);
            Mock.Get(frameListener).Verify(f => f.InitializeEventsAsync(windowFrame), Times.Once);
            Mock.Get(projectFileWatcher).Verify(p => p.InitializeModelWatcher(), Times.Once);
            Assert.Equal(editorState.CurrentState, EditorStateModel.EditorState.EditorOpen);
        }

        [Fact]
        public async Task EditorStateModel_MultipleOpen_CallsShowSecondTime()
        {
            var filePath = @"C:\Temp\ConsoleApp1.csproj";
            var textBufferManager = ITextBufferManagerFactory.ImplementFilePath(filePath);
            var textBufferManagerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferManager);

            var textBufferListener = ITextBufferStateListenerFactory.Create();
            var textBufferListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferListener);

            var frameListener = IFrameOpenCloseListenerFactory.Create();
            var frameListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => frameListener);

            var projectFileWatcher = IProjectFileModelWatcherFactory.Create();
            var projectFileWatcherFactory = ExportFactoryFactory.ImplementCreateValue(() => projectFileWatcher);

            var windowFrame = IVsWindowFrameFactory.Create();
            var shellUtilities = new TestShellUtilitiesHelper((provider, fullPath) => (null, 0, null, 0), (provider, path, editorType, logicalView) =>
            {
                Assert.Equal(path, filePath);
                Assert.Equal(XmlFactoryGuid, editorType);
                Assert.Equal(Guid.Empty, logicalView);
                return windowFrame;
            });

            var editorState = new EditorStateModelTester(
                new IProjectThreadingServiceMock(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                shellUtilities,
                projectFileWatcherFactory,
                textBufferListenerFactory,
                frameListenerFactory,
                textBufferManagerFactory);

            await editorState.OpenEditorAsync();
            Mock.Get(windowFrame).Verify(w => w.Show(), Times.Never);

            // On the second call, we should call show on the frame, and none of the listeners should have been set up again
            await editorState.OpenEditorAsync();
            Mock.Get(windowFrame).Verify(w => w.Show(), Times.Once);
            Mock.Get(textBufferManager).Verify(t => t.InitializeBufferAsync(), Times.Once);
            Mock.Get(textBufferListener).Verify(t => t.InitializeListenerAsync(filePath), Times.Once);
            Mock.Get(frameListener).Verify(f => f.InitializeEventsAsync(windowFrame), Times.Once);
            Mock.Get(projectFileWatcher).Verify(p => p.InitializeModelWatcher(), Times.Once);
            Assert.Equal(editorState.CurrentState, EditorStateModel.EditorState.EditorOpen);
        }

        [Fact]
        public async Task EditorStateModel_CloseWindowSuccess_ReturnsContinueClose()
        {
            var filePath = @"C:\Temp\ConsoleApp1.csproj";
            var textBufferManager = ITextBufferManagerFactory.ImplementFilePath(filePath);
            var textBufferManagerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferManager);

            var textBufferListener = ITextBufferStateListenerFactory.Create();
            var textBufferListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferListener);

            var frameListener = IFrameOpenCloseListenerFactory.Create();
            var frameListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => frameListener);

            var projectFileWatcher = IProjectFileModelWatcherFactory.Create();
            var projectFileWatcherFactory = ExportFactoryFactory.ImplementCreateValue(() => projectFileWatcher);

            var windowFrame = IVsWindowFrameFactory.ImplementCloseFrame(options =>
            {
                Assert.Equal((uint)__FRAMECLOSE.FRAMECLOSE_PromptSave, options);
                return VSConstants.S_OK;
            });
            var shellUtilities = new TestShellUtilitiesHelper((provider, fullPath) => (null, 0, null, 0), (provider, path, editorType, logicalView) =>
            {
                Assert.Equal(path, filePath);
                Assert.Equal(XmlFactoryGuid, editorType);
                Assert.Equal(Guid.Empty, logicalView);
                return windowFrame;
            });

            var editorState = new EditorStateModelTester(
                new IProjectThreadingServiceMock(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                shellUtilities,
                projectFileWatcherFactory,
                textBufferListenerFactory,
                frameListenerFactory,
                textBufferManagerFactory);

            await editorState.OpenEditorAsync();

            Assert.True(await editorState.CloseWindowAsync());
        }

        [Fact]
        public async Task EditorStateModel_CloseWindowFail_ReturnsStopClose()
        {
            var filePath = @"C:\Temp\ConsoleApp1.csproj";
            var textBufferManager = ITextBufferManagerFactory.ImplementFilePath(filePath);
            var textBufferManagerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferManager);

            var textBufferListener = ITextBufferStateListenerFactory.Create();
            var textBufferListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferListener);

            var frameListener = IFrameOpenCloseListenerFactory.Create();
            var frameListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => frameListener);

            var projectFileWatcher = IProjectFileModelWatcherFactory.Create();
            var projectFileWatcherFactory = ExportFactoryFactory.ImplementCreateValue(() => projectFileWatcher);

            var windowFrame = IVsWindowFrameFactory.ImplementCloseFrame(options =>
            {
                Assert.Equal((uint)__FRAMECLOSE.FRAMECLOSE_PromptSave, options);
                return VSConstants.E_FAIL;
            });
            var shellUtilities = new TestShellUtilitiesHelper((provider, fullPath) => (null, 0, null, 0), (provider, path, editorType, logicalView) =>
            {
                Assert.Equal(path, filePath);
                Assert.Equal(XmlFactoryGuid, editorType);
                Assert.Equal(Guid.Empty, logicalView);
                return windowFrame;
            });

            var editorState = new EditorStateModelTester(
                new IProjectThreadingServiceMock(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                shellUtilities,
                projectFileWatcherFactory,
                textBufferListenerFactory,
                frameListenerFactory,
                textBufferManagerFactory);

            await editorState.OpenEditorAsync();

            Assert.False(await editorState.CloseWindowAsync());
        }

        [Fact]
        public async Task EditorStateModel_DisposeWithoutOpen_DoesNothing()
        {
            var filePath = @"C:\Temp\ConsoleApp1.csproj";
            var textBufferManager = ITextBufferManagerFactory.ImplementFilePath(filePath);
            var textBufferManagerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferManager);

            var textBufferListener = ITextBufferStateListenerFactory.Create();
            var textBufferListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferListener);

            var frameListener = IFrameOpenCloseListenerFactory.Create();
            var frameListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => frameListener);

            var projectFileWatcher = IProjectFileModelWatcherFactory.Create();
            var projectFileWatcherFactory = ExportFactoryFactory.ImplementCreateValue(() => projectFileWatcher);

            var windowFrame = IVsWindowFrameFactory.ImplementCloseFrame(options =>
            {
                Assert.Equal((uint)__FRAMECLOSE.FRAMECLOSE_PromptSave, options);
                return VSConstants.S_OK;
            });
            var shellUtilities = new TestShellUtilitiesHelper((provider, fullPath) => (null, 0, null, 0), (provider, path, editorType, logicalView) =>
            {
                Assert.Equal(path, filePath);
                Assert.Equal(XmlFactoryGuid, editorType);
                Assert.Equal(Guid.Empty, logicalView);
                return windowFrame;
            });

            var editorState = new EditorStateModelTester(
                new IProjectThreadingServiceMock(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                shellUtilities,
                projectFileWatcherFactory,
                textBufferListenerFactory,
                frameListenerFactory,
                textBufferManagerFactory);

            await editorState.DisposeEditorAsync();
            Mock.Get(textBufferManager).Verify(t => t.Dispose(), Times.Never);
            Mock.Get(textBufferListener).Verify(t => t.Dispose(), Times.Never);
            Mock.Get(frameListener).Verify(f => f.DisposeAsync(), Times.Never);
            Mock.Get(projectFileWatcher).Verify(p => p.Dispose(), Times.Never);
        }

        [Fact]
        public async Task EditorStateModel_DisposeWithOpen_DisposesListeners()
        {
            var filePath = @"C:\Temp\ConsoleApp1.csproj";
            var textBufferManager = ITextBufferManagerFactory.ImplementFilePath(filePath);
            var textBufferManagerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferManager);

            var textBufferListener = ITextBufferStateListenerFactory.Create();
            var textBufferListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferListener);

            var frameListener = IFrameOpenCloseListenerFactory.Create();
            var frameListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => frameListener);

            var projectFileWatcher = IProjectFileModelWatcherFactory.Create();
            var projectFileWatcherFactory = ExportFactoryFactory.ImplementCreateValue(() => projectFileWatcher);

            var windowFrame = IVsWindowFrameFactory.ImplementCloseFrame(options =>
            {
                Assert.Equal((uint)__FRAMECLOSE.FRAMECLOSE_PromptSave, options);
                return VSConstants.S_OK;
            });
            var shellUtilities = new TestShellUtilitiesHelper((provider, fullPath) => (null, 0, null, 0), (provider, path, editorType, logicalView) =>
            {
                Assert.Equal(path, filePath);
                Assert.Equal(XmlFactoryGuid, editorType);
                Assert.Equal(Guid.Empty, logicalView);
                return windowFrame;
            });

            var editorState = new EditorStateModelTester(
                new IProjectThreadingServiceMock(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                shellUtilities,
                projectFileWatcherFactory,
                textBufferListenerFactory,
                frameListenerFactory,
                textBufferManagerFactory);

            // Mock dispose for one of the listeners so it can verify the editor is actually in EditorClosing
            Mock.Get(textBufferListener).Setup(t => t.Dispose()).Callback(() =>
                Assert.Equal(EditorStateModel.EditorState.EditorClosing, editorState.CurrentState));

            await editorState.OpenEditorAsync();
            await editorState.DisposeEditorAsync();
            Mock.Get(textBufferManager).Verify(t => t.Dispose(), Times.Once);
            Mock.Get(textBufferListener).Verify(t => t.Dispose(), Times.Once);
            Mock.Get(frameListener).Verify(f => f.DisposeAsync(), Times.Once);
            Mock.Get(projectFileWatcher).Verify(p => p.Dispose(), Times.Once);
            Assert.Equal(EditorStateModel.EditorState.NoEditor, editorState.CurrentState);
        }

        [Fact]
        public async Task EditorStateModel_UpdateProjectFile_SchedulesUpdate()
        {
            var filePath = @"C:\Temp\ConsoleApp1.csproj";
            var textBufferManager = ITextBufferManagerFactory.ImplementFilePath(filePath);
            var textBufferManagerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferManager);

            var textBufferListener = ITextBufferStateListenerFactory.Create();
            var textBufferListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferListener);

            var frameListener = IFrameOpenCloseListenerFactory.Create();
            var frameListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => frameListener);

            var projectFileWatcher = IProjectFileModelWatcherFactory.Create();
            var projectFileWatcherFactory = ExportFactoryFactory.ImplementCreateValue(() => projectFileWatcher);

            var windowFrame = IVsWindowFrameFactory.Create();
            var shellUtilities = new TestShellUtilitiesHelper((provider, fullPath) => (null, 0, null, 0), (provider, path, editorType, logicalView) =>
            {
                Assert.Equal(path, filePath);
                Assert.Equal(XmlFactoryGuid, editorType);
                Assert.Equal(Guid.Empty, logicalView);
                return windowFrame;
            });

            var threadingService = IProjectThreadingServiceFactory.Create();

            var editorState = new EditorStateModelTester(
                threadingService,
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                shellUtilities,
                projectFileWatcherFactory,
                textBufferListenerFactory,
                frameListenerFactory,
                textBufferManagerFactory);

            // Implement ResetBuffer so we can verify that the editor is in BufferUpdateScheduled when calling ResetBuffer. While here,
            // verify that the buffer was set to readonly before reset was called
            var textBufferMock = Mock.Get(textBufferManager);
            textBufferMock.Setup(t => t.ResetBufferAsync()).Callback(() =>
            {
                Assert.Equal(EditorStateModel.EditorState.BufferUpdateScheduled, editorState.CurrentState);
                textBufferMock.Verify(t => t.SetReadOnlyAsync(true), Times.Once);
            }).Returns(Task.CompletedTask);

            await editorState.OpenEditorAsync();
            var jt = editorState.ScheduleProjectFileUpdate();

            // Ensure the update actually runs
            await jt.JoinAsync();

            textBufferMock.Verify(t => t.SetReadOnlyAsync(true), Times.Once);
            textBufferMock.Verify(t => t.ResetBufferAsync(), Times.Once);
            textBufferMock.Verify(t => t.SetReadOnlyAsync(false), Times.Once);
        }

        [Theory]
        [InlineData((int)EditorStateModel.EditorState.NoEditor)]
        [InlineData((int)EditorStateModel.EditorState.EditorClosing)]
        [InlineData((int)EditorStateModel.EditorState.WritingProjectFile)]
        public void EditorStateModel_UpdateProjectFileIncorrectEditorState_DoesNothing(int state)
        {
            var editorState = new EditorStateModelTester(
                IProjectThreadingServiceFactory.Create(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                new TestShellUtilitiesHelper(),
                ExportFactoryFactory.CreateInstance<IProjectFileModelWatcher>(),
                ExportFactoryFactory.CreateInstance<ITextBufferStateListener>(),
                ExportFactoryFactory.CreateInstance<IFrameOpenCloseListener>(),
                ExportFactoryFactory.CreateInstance<ITextBufferManager>())
            {
                CurrentState = (EditorStateModel.EditorState)state
            };

            Assert.Null(editorState.ScheduleProjectFileUpdate());
        }

        [Fact]
        public async Task EditorStateModel_SaveProjectFile_SavesFile()
        {
            var filePath = @"C:\Temp\ConsoleApp1.csproj";
            var textBufferManager = ITextBufferManagerFactory.ImplementFilePath(filePath);
            var textBufferManagerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferManager);

            var textBufferListener = ITextBufferStateListenerFactory.Create();
            var textBufferListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferListener);

            var frameListener = IFrameOpenCloseListenerFactory.Create();
            var frameListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => frameListener);

            var projectFileWatcher = IProjectFileModelWatcherFactory.Create();
            var projectFileWatcherFactory = ExportFactoryFactory.ImplementCreateValue(() => projectFileWatcher);

            var windowFrame = IVsWindowFrameFactory.Create();
            var shellUtilities = new TestShellUtilitiesHelper((provider, fullPath) => (null, 0, null, 0), (provider, path, editorType, logicalView) =>
            {
                Assert.Equal(path, filePath);
                Assert.Equal(XmlFactoryGuid, editorType);
                Assert.Equal(Guid.Empty, logicalView);
                return windowFrame;
            });

            var threadingService = IProjectThreadingServiceFactory.Create();

            var editorState = new EditorStateModelTester(
                threadingService,
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                shellUtilities,
                projectFileWatcherFactory,
                textBufferListenerFactory,
                frameListenerFactory,
                textBufferManagerFactory);

            var textBufferMock = Mock.Get(textBufferManager);
            // Implement textBufferManager.SaveAsync to verify the editor is in WritingProjectFile while saving
            textBufferMock.Setup(t => t.SaveAsync()).Callback(() =>
            {
                Assert.Equal(EditorStateModel.EditorState.WritingProjectFile, editorState.CurrentState);
                textBufferMock.Verify(t => t.SetReadOnlyAsync(true), Times.Once);
            }).Returns(Task.CompletedTask);

            await editorState.OpenEditorAsync();
            await editorState.SaveProjectFileAsync();

            textBufferMock.Verify(t => t.SetReadOnlyAsync(true), Times.Once);
            textBufferMock.Verify(t => t.SaveAsync(), Times.Once);
            textBufferMock.Verify(t => t.SetReadOnlyAsync(false), Times.Once);
        }

        [Theory]
        [InlineData((int)EditorStateModel.EditorState.BufferUpdateScheduled)]
        [InlineData((int)EditorStateModel.EditorState.EditorClosing)]
        [InlineData((int)EditorStateModel.EditorState.Initializing)]
        [InlineData((int)EditorStateModel.EditorState.NoEditor)]
        [InlineData((int)EditorStateModel.EditorState.WritingProjectFile)]
        public async Task EditorStateModel_SaveProjectFile_OnlySavesInEditorOpen(int state)
        {
            var filePath = @"C:\Temp\ConsoleApp1.csproj";
            var textBufferManager = ITextBufferManagerFactory.ImplementFilePath(filePath);
            var textBufferManagerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferManager);

            var textBufferListener = ITextBufferStateListenerFactory.Create();
            var textBufferListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => textBufferListener);

            var frameListener = IFrameOpenCloseListenerFactory.Create();
            var frameListenerFactory = ExportFactoryFactory.ImplementCreateValue(() => frameListener);

            var projectFileWatcher = IProjectFileModelWatcherFactory.Create();
            var projectFileWatcherFactory = ExportFactoryFactory.ImplementCreateValue(() => projectFileWatcher);

            var windowFrame = IVsWindowFrameFactory.Create();
            var shellUtilities = new TestShellUtilitiesHelper((provider, fullPath) => (null, 0, null, 0), (provider, path, editorType, logicalView) =>
            {
                Assert.Equal(path, filePath);
                Assert.Equal(XmlFactoryGuid, editorType);
                Assert.Equal(Guid.Empty, logicalView);
                return windowFrame;
            });

            var threadingService = IProjectThreadingServiceFactory.Create();

            var editorState = new EditorStateModelTester(
                threadingService,
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                shellUtilities,
                projectFileWatcherFactory,
                textBufferListenerFactory,
                frameListenerFactory,
                textBufferManagerFactory)
            {
                CurrentState = (EditorStateModel.EditorState)state
            };

            await editorState.SaveProjectFileAsync();

            var textBufferMock = Mock.Get(textBufferManager);

            textBufferMock.Verify(t => t.SetReadOnlyAsync(It.IsAny<bool>()), Times.Never);
            textBufferMock.Verify(t => t.SaveAsync(), Times.Never);
        }

        /// <summary>
        /// Simple subclass of EditorStateModel that exposes the current state so we can verify it.
        /// </summary>
        private class EditorStateModelTester : EditorStateModel
        {
            public EditorStateModelTester(IProjectThreadingService threadingService,
                UnconfiguredProject unconfiguredProject,
                [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
                IVsShellUtilitiesHelper shellHelper,
                ExportFactory<IProjectFileModelWatcher> projectFileModelWatcherFactory,
                ExportFactory<ITextBufferStateListener> textBufferListenerFactory,
                ExportFactory<IFrameOpenCloseListener> frameEventsListenerFactory,
                ExportFactory<ITextBufferManager> textBufferManagerFactory) : base(threadingService, unconfiguredProject, serviceProvider, shellHelper, projectFileModelWatcherFactory, textBufferListenerFactory, frameEventsListenerFactory, textBufferManagerFactory)
            {
            }

            public EditorState CurrentState
            {
                get => _currentState;
                set => _currentState = value;
            }
        }
    }
}
