// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [ProjectSystemTrait]
    public class FrameOpenCloseListenerTests
    {
        [Fact]
        public void FrameOpenCloseListener_NullServiceProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>("helper", () => new FrameOpenCloseListener(null, IEditorStateModelFactory.Create(),
                IProjectThreadingServiceFactory.Create(), UnconfiguredProjectFactory.Create()));
        }

        [Fact]
        public void FrameOpenCloseListener_NullEditorModel_Throws()
        {
            Assert.Throws<ArgumentNullException>("editorModel", () => new FrameOpenCloseListener(IServiceProviderFactory.Create(), null,
                IProjectThreadingServiceFactory.Create(), UnconfiguredProjectFactory.Create()));
        }

        [Fact]
        public void FrameOpenCloseListener_NullThreadingService_Throws()
        {
            Assert.Throws<ArgumentNullException>("threadingService", () => new FrameOpenCloseListener(IServiceProviderFactory.Create(),
                IEditorStateModelFactory.Create(), null, UnconfiguredProjectFactory.Create()));
        }

        [Fact]
        public void FrameOpenCloseListener_NullProject_Throws()
        {
            Assert.Throws<ArgumentNullException>("unconfiguredProject", () => new FrameOpenCloseListener(IServiceProviderFactory.Create(),
                IEditorStateModelFactory.Create(), IProjectThreadingServiceFactory.Create(), null));
        }

        [Fact]
        public async Task FrameOpenCloseListener_NullFrame_Throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>("frame", async () =>
            {
                var listener = new FrameOpenCloseListener(IServiceProviderFactory.Create(), IEditorStateModelFactory.Create(),
                    IProjectThreadingServiceFactory.Create(), UnconfiguredProjectFactory.Create());
                await listener.InitializeEventsAsync(null);
            });
        }

        [Fact]
        public async Task FrameOpenCloseListener_InitializeEventsAsync_SetsUpWindowFrameEvents()
        {
            var uiShell = IVsUIShell7Factory.ImplementAdviseWindowEvents(l => 1);

            uint eventsCookie = 2;
            var solution = IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(eventsCookie);

            var serviceProvider = IServiceProviderFactory.ImplementGetService(ServiceTypeChecker(uiShell, solution));

            var listener = new FrameOpenCloseListener(serviceProvider,
                IEditorStateModelFactory.Create(),
                new IProjectThreadingServiceMock(),
                UnconfiguredProjectFactory.Create());

            await listener.InitializeEventsAsync(IVsWindowFrameFactory.Create());
            Mock.Get(uiShell).Verify(u => u.AdviseWindowFrameEvents(listener), Times.Once);
            Mock.Get(solution).Verify(s => s.AdviseSolutionEvents(listener, out eventsCookie), Times.Once);
        }

        [Fact]
        public async Task FrameOpenCloseListener_Dispose_UnsubscribesCorrectly()
        {
            var uiShell = IVsUIShell7Factory.ImplementAdviseUnadviseWindowEvents(l => 1234, c => Assert.Equal<uint>(1234, c));

            var solution = IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(4321);

            var serviceProvider = IServiceProviderFactory.ImplementGetService(ServiceTypeChecker(uiShell, solution));

            var listener = new FrameOpenCloseListener(serviceProvider,
                IEditorStateModelFactory.Create(),
                new IProjectThreadingServiceMock(),
                UnconfiguredProjectFactory.Create());

            await listener.InitializeEventsAsync(IVsWindowFrameFactory.Create());

            await listener.DisposeAsync();
            Mock.Get(uiShell).Verify(u => u.UnadviseWindowFrameEvents(1234), Times.Once);
            Mock.Get(solution).Verify(s => s.UnadviseSolutionEvents(4321), Times.Once);
        }

        [Fact]
        public void FrameOpenCloseListener_QueryUnloadCorrectProjectNoCancel_CallsCloseOnEditor()
        {
            var uiShell = IVsUIShell7Factory.ImplementAdviseUnadviseWindowEvents(l => 1234, c => Assert.Equal<uint>(1234, c));

            var solution = IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(4321);

            var serviceProvider = IServiceProviderFactory.ImplementGetService(ServiceTypeChecker(uiShell, solution));

            var editorState = IEditorStateModelFactory.ImplementCloseWindowAsync(true);

            var projPath = @"C:\ConsoleApp\ConsoleApp1\ConsoleApp1.csproj";
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projPath);
            var hierarchy = IVsHierarchyFactory.ImplementAsUnconfiguredProject(unconfiguredProject);

            int shouldCancel = -1;

            var listener = new FrameOpenCloseListener(serviceProvider, editorState, new IProjectThreadingServiceMock(), unconfiguredProject);
            Assert.Equal(VSConstants.S_OK, listener.OnQueryUnloadProject(hierarchy, ref shouldCancel));
            Assert.Equal(0, shouldCancel);
            Mock.Get(editorState).Verify(e => e.CloseWindowAsync(), Times.Once);
        }

        [Fact]
        public void FrameOpenCloseListener_QueryUnloadCorrectProjectYesCancel_CancelsUnload()
        {
            var uiShell = IVsUIShell7Factory.ImplementAdviseUnadviseWindowEvents(l => 1234, c => Assert.Equal<uint>(1234, c));

            var solution = IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(4321);

            var serviceProvider = IServiceProviderFactory.ImplementGetService(ServiceTypeChecker(uiShell, solution));

            var editorState = IEditorStateModelFactory.ImplementCloseWindowAsync(false);

            var projPath = @"C:\ConsoleApp\ConsoleApp1\ConsoleApp1.csproj";
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projPath);
            var hierarchy = IVsHierarchyFactory.ImplementAsUnconfiguredProject(unconfiguredProject);

            int shouldCancel = -1;

            var listener = new FrameOpenCloseListener(serviceProvider, editorState, new IProjectThreadingServiceMock(), unconfiguredProject);
            Assert.Equal(VSConstants.S_OK, listener.OnQueryUnloadProject(hierarchy, ref shouldCancel));
            Assert.Equal(1, shouldCancel);
            Mock.Get(editorState).Verify(e => e.CloseWindowAsync(), Times.Once);
        }

        [Fact]
        public void FrameOpenCloseListener_QueryUnloadWrongProject_DoesNotCallClose()
        {
            var uiShell = IVsUIShell7Factory.ImplementAdviseUnadviseWindowEvents(l => 1234, c => Assert.Equal<uint>(1234, c));

            var solution = IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(4321);

            var serviceProvider = IServiceProviderFactory.ImplementGetService(ServiceTypeChecker(uiShell, solution));

            var editorState = IEditorStateModelFactory.ImplementCloseWindowAsync(false);

            var projPath = @"C:\ConsoleApp\ConsoleApp1\ConsoleApp1.csproj";
            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: projPath);
            var hierarchy = IVsHierarchyFactory.ImplementAsUnconfiguredProject(unconfiguredProject);

            int shouldCancel = -1;

            var listener = new FrameOpenCloseListener(serviceProvider, editorState, new IProjectThreadingServiceMock(),
                UnconfiguredProjectFactory.Create(filePath: @"C:\ConsoleApp\ADifferentProject\ADifferentProject.csproj"));
            Assert.Equal(VSConstants.S_OK, listener.OnQueryUnloadProject(hierarchy, ref shouldCancel));
            Assert.Equal(0, shouldCancel);
            Mock.Get(editorState).Verify(e => e.CloseWindowAsync(), Times.Never);
        }

        private Func<Type, object> ServiceTypeChecker(IVsUIShell7 uiShell, IVsSolution solution)
        {
            return t =>
            {
                if (typeof(SVsUIShell).Equals(t))
                {
                    return uiShell;
                }

                if (typeof(SVsSolution).Equals(t))
                {
                    return solution;
                }

                Assert.True(false, $"Type {t} is not expected");
                return null;
            };
        }
    }
}
