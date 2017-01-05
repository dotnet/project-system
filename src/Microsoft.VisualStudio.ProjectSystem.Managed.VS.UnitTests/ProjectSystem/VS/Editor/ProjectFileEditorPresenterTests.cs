// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [ProjectSystemTrait]
    public class ProjectFileEditorPresenterTests
    {
        private static readonly Guid XmlFactoryGuid = Guid.Parse("{fa3cd31e-987b-443a-9b81-186104e8dac1}");

        [Fact]
        public void ProjectFileEditorPresenter_NullThreadingService_Throws()
        {
            Assert.Throws<ArgumentNullException>("threadingService", () => new ProjectFileEditorPresenter(
                null,
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                IVsShellUtilitiesHelperFactory.Create(),
                ExportFactoryFactory.CreateInstance<IProjectFileModelWatcher>(),
                ExportFactoryFactory.CreateInstance<ITextBufferStateListener>(),
                ExportFactoryFactory.CreateInstance<IFrameOpenCloseListener>(),
                ExportFactoryFactory.CreateInstance<ITextBufferManager>()));
        }

        [Fact]
        public void ProjectFileEditorPresenter_NullUnconfiguredProject_Throws()
        {
            Assert.Throws<ArgumentNullException>("unconfiguredProject", () => new ProjectFileEditorPresenter(
                IProjectThreadingServiceFactory.Create(),
                null,
                IServiceProviderFactory.Create(),
                IVsShellUtilitiesHelperFactory.Create(),
                ExportFactoryFactory.CreateInstance<IProjectFileModelWatcher>(),
                ExportFactoryFactory.CreateInstance<ITextBufferStateListener>(),
                ExportFactoryFactory.CreateInstance<IFrameOpenCloseListener>(),
                ExportFactoryFactory.CreateInstance<ITextBufferManager>()));
        }

        [Fact]
        public void ProjectFileEditorPresenter_NullServiceProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>("serviceProvider", () => new ProjectFileEditorPresenter(
                IProjectThreadingServiceFactory.Create(),
                UnconfiguredProjectFactory.Create(),
                null,
                IVsShellUtilitiesHelperFactory.Create(),
                ExportFactoryFactory.CreateInstance<IProjectFileModelWatcher>(),
                ExportFactoryFactory.CreateInstance<ITextBufferStateListener>(),
                ExportFactoryFactory.CreateInstance<IFrameOpenCloseListener>(),
                ExportFactoryFactory.CreateInstance<ITextBufferManager>()));
        }

        [Fact]
        public void ProjectFileEditorPresenter_NullShellHelper_Throws()
        {
            Assert.Throws<ArgumentNullException>("shellHelper", () => new ProjectFileEditorPresenter(
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
        public void ProjectFileEditorPresenter_NullProjectFileModelWatcherFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>("projectFileModelWatcherFactory", () => new ProjectFileEditorPresenter(
                IProjectThreadingServiceFactory.Create(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                IVsShellUtilitiesHelperFactory.Create(),
                null,
                ExportFactoryFactory.CreateInstance<ITextBufferStateListener>(),
                ExportFactoryFactory.CreateInstance<IFrameOpenCloseListener>(),
                ExportFactoryFactory.CreateInstance<ITextBufferManager>()));
        }

        [Fact]
        public void ProjectFileEditorPresenter_NullTextBufferListenerFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>("textBufferListenerFactory", () => new ProjectFileEditorPresenter(
                IProjectThreadingServiceFactory.Create(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                IVsShellUtilitiesHelperFactory.Create(),
                ExportFactoryFactory.CreateInstance<IProjectFileModelWatcher>(),
                null,
                ExportFactoryFactory.CreateInstance<IFrameOpenCloseListener>(),
                ExportFactoryFactory.CreateInstance<ITextBufferManager>()));
        }

        [Fact]
        public void ProjectFileEditorPresenter_NullFrameEventsListenerFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>("frameEventsListenerFactory", () => new ProjectFileEditorPresenter(
                IProjectThreadingServiceFactory.Create(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                IVsShellUtilitiesHelperFactory.Create(),
                ExportFactoryFactory.CreateInstance<IProjectFileModelWatcher>(),
                ExportFactoryFactory.CreateInstance<ITextBufferStateListener>(),
                null,
                ExportFactoryFactory.CreateInstance<ITextBufferManager>()));
        }

        [Fact]
        public void ProjectFileEditorPresenter_NullTextBufferManagerFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>("textBufferManagerFactory", () => new ProjectFileEditorPresenter(
                IProjectThreadingServiceFactory.Create(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                IVsShellUtilitiesHelperFactory.Create(),
                ExportFactoryFactory.CreateInstance<IProjectFileModelWatcher>(),
                ExportFactoryFactory.CreateInstance<ITextBufferStateListener>(),
                ExportFactoryFactory.CreateInstance<IFrameOpenCloseListener>(),
                null));
        }

        [Fact]
        public async Task ProjectFileEditorPresenter_OpenEditor_SetsUpListeners()
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
            var shellUtilities = IVsShellUtilitiesHelperFactory.ImplementOpenDocument(filePath, XmlFactoryGuid, Guid.Empty, windowFrame);

            var editorState = new ProjectFileEditorPresenterTester(
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
                Assert.Equal(ProjectFileEditorPresenter.EditorState.Initializing, editorState.CurrentState)).Returns(Task.CompletedTask);

            await editorState.OpenEditorAsync();
            Mock.Get(textBufferManager).Verify(t => t.InitializeBufferAsync(), Times.Once);
            Mock.Get(textBufferListener).Verify(t => t.InitializeListenerAsync(filePath), Times.Once);
            Mock.Get(frameListener).Verify(f => f.InitializeEventsAsync(windowFrame), Times.Once);
            Mock.Get(projectFileWatcher).Verify(p => p.InitializeModelWatcher(), Times.Once);
            Assert.Equal(editorState.CurrentState, ProjectFileEditorPresenter.EditorState.EditorOpen);
        }

        [Fact]
        public async Task ProjectFileEditorPresenter_MultipleOpen_CallsShowSecondTime()
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
            var shellUtilities = IVsShellUtilitiesHelperFactory.ImplementOpenDocument(filePath, XmlFactoryGuid, Guid.Empty, windowFrame);

            var editorState = new ProjectFileEditorPresenterTester(
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
            Assert.Equal(editorState.CurrentState, ProjectFileEditorPresenter.EditorState.EditorOpen);
        }

        [Fact]
        public async Task ProjectFileEditorPresenter_CloseWindowSuccess_ReturnsContinueClose()
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
            var shellUtilities = IVsShellUtilitiesHelperFactory.ImplementOpenDocument(filePath, XmlFactoryGuid, Guid.Empty, windowFrame);

            var editorState = new ProjectFileEditorPresenterTester(
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
        public async Task ProjectFileEditorPresenter_CloseWindowFail_ReturnsStopClose()
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
            var shellUtilities = IVsShellUtilitiesHelperFactory.ImplementOpenDocument(filePath, XmlFactoryGuid, Guid.Empty, windowFrame);

            var editorState = new ProjectFileEditorPresenterTester(
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
        public async Task ProjectFileEditorPresenter_DisposeWithoutOpen_DoesNothing()
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
            var shellUtilities = IVsShellUtilitiesHelperFactory.ImplementOpenDocument(filePath, XmlFactoryGuid, Guid.Empty, windowFrame);

            var editorState = new ProjectFileEditorPresenterTester(
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
        public async Task ProjectFileEditorPresenter_DisposeWithOpen_DisposesListeners()
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
            var shellUtilities = IVsShellUtilitiesHelperFactory.ImplementOpenDocument(filePath, XmlFactoryGuid, Guid.Empty, windowFrame);

            var editorState = new ProjectFileEditorPresenterTester(
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
                Assert.Equal(ProjectFileEditorPresenter.EditorState.EditorClosing, editorState.CurrentState));

            await editorState.OpenEditorAsync();
            await editorState.DisposeEditorAsync();
            Mock.Get(textBufferManager).Verify(t => t.Dispose(), Times.Once);
            Mock.Get(textBufferListener).Verify(t => t.Dispose(), Times.Once);
            Mock.Get(frameListener).Verify(f => f.DisposeAsync(), Times.Once);
            Mock.Get(projectFileWatcher).Verify(p => p.Dispose(), Times.Once);
            Assert.Equal(ProjectFileEditorPresenter.EditorState.NoEditor, editorState.CurrentState);
        }

        [Fact]
        public async Task ProjectFileEditorPresenter_UpdateProjectFile_SchedulesUpdate()
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
            var shellUtilities = IVsShellUtilitiesHelperFactory.ImplementOpenDocument(filePath, XmlFactoryGuid, Guid.Empty, windowFrame);

            var threadingService = IProjectThreadingServiceFactory.Create();

            var editorState = new ProjectFileEditorPresenterTester(
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
                Assert.Equal(ProjectFileEditorPresenter.EditorState.BufferUpdateScheduled, editorState.CurrentState);
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
        [InlineData((int)ProjectFileEditorPresenter.EditorState.NoEditor)]
        [InlineData((int)ProjectFileEditorPresenter.EditorState.EditorClosing)]
        [InlineData((int)ProjectFileEditorPresenter.EditorState.WritingProjectFile)]
        public void ProjectFileEditorPresenter_UpdateProjectFileIncorrectEditorState_DoesNothing(int state)
        {
            var editorState = new ProjectFileEditorPresenterTester(
                IProjectThreadingServiceFactory.Create(),
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                IVsShellUtilitiesHelperFactory.Create(),
                ExportFactoryFactory.CreateInstance<IProjectFileModelWatcher>(),
                ExportFactoryFactory.CreateInstance<ITextBufferStateListener>(),
                ExportFactoryFactory.CreateInstance<IFrameOpenCloseListener>(),
                ExportFactoryFactory.CreateInstance<ITextBufferManager>())
            {
                CurrentState = (ProjectFileEditorPresenter.EditorState)state
            };

            Assert.Null(editorState.ScheduleProjectFileUpdate());
        }

        [Fact]
        public async Task ProjectFileEditorPresenter_SaveProjectFile_SavesFile()
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
            var shellUtilities = IVsShellUtilitiesHelperFactory.ImplementOpenDocument(filePath, XmlFactoryGuid, Guid.Empty, windowFrame);

            var threadingService = IProjectThreadingServiceFactory.Create();

            var editorState = new ProjectFileEditorPresenterTester(
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
                Assert.Equal(ProjectFileEditorPresenter.EditorState.WritingProjectFile, editorState.CurrentState);
                textBufferMock.Verify(t => t.SetReadOnlyAsync(true), Times.Once);
            }).Returns(Task.CompletedTask);

            await editorState.OpenEditorAsync();
            await editorState.SaveProjectFileAsync();

            textBufferMock.Verify(t => t.SetReadOnlyAsync(true), Times.Once);
            textBufferMock.Verify(t => t.SaveAsync(), Times.Once);
            textBufferMock.Verify(t => t.SetReadOnlyAsync(false), Times.Once);
        }

        [Theory]
        [InlineData((int)ProjectFileEditorPresenter.EditorState.BufferUpdateScheduled)]
        [InlineData((int)ProjectFileEditorPresenter.EditorState.EditorClosing)]
        [InlineData((int)ProjectFileEditorPresenter.EditorState.Initializing)]
        [InlineData((int)ProjectFileEditorPresenter.EditorState.NoEditor)]
        [InlineData((int)ProjectFileEditorPresenter.EditorState.WritingProjectFile)]
        public async Task ProjectFileEditorPresenter_SaveProjectFile_OnlySavesInEditorOpen(int state)
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
            var shellUtilities = IVsShellUtilitiesHelperFactory.ImplementOpenDocument(filePath, XmlFactoryGuid, Guid.Empty, windowFrame);

            var threadingService = IProjectThreadingServiceFactory.Create();

            var editorState = new ProjectFileEditorPresenterTester(
                threadingService,
                UnconfiguredProjectFactory.Create(),
                IServiceProviderFactory.Create(),
                shellUtilities,
                projectFileWatcherFactory,
                textBufferListenerFactory,
                frameListenerFactory,
                textBufferManagerFactory)
            {
                CurrentState = (ProjectFileEditorPresenter.EditorState)state
            };

            // When SaveAsync is called, we should hit an assert in the NoEditor case.
            bool assertHit = false;
            try
            {
                await editorState.SaveProjectFileAsync();
            }
            catch (InvalidOperationException)
            {
                assertHit = true;
            }

            Assert.True(state != (int)ProjectFileEditorPresenter.EditorState.NoEditor || assertHit);

            var textBufferMock = Mock.Get(textBufferManager);

            textBufferMock.Verify(t => t.SetReadOnlyAsync(It.IsAny<bool>()), Times.Never);
            textBufferMock.Verify(t => t.SaveAsync(), Times.Never);
        }

        /// <summary>
        /// Simple subclass of <see cref="ProjectFileEditorPresenter"/> that exposes the current state so we can verify it.
        /// </summary>
        private class ProjectFileEditorPresenterTester : ProjectFileEditorPresenter
        {
            public ProjectFileEditorPresenterTester(IProjectThreadingService threadingService,
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
