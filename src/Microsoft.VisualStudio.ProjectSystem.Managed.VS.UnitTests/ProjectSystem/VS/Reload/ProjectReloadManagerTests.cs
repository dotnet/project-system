// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemTrait]
    public class ProjectReloadManagerTests
    {
        [Fact]
        public async Task Initialize_ValidateSolutionEventsEstablished()
        {
            var spMock = new IServiceProviderMoq();
            spMock.AddService(typeof(IVsFileChangeEx), typeof(SVsFileChangeEx), IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(100));
            spMock.AddService(typeof(IVsSolution), typeof(SVsSolution), IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(150));

            var rlm = new ProjectReloadManager(IProjectThreadingServiceFactory.Create(), spMock, IUserNotificationServicesFactory.ImplementReportErrorInfo(), new Mock<IDialogServices>().Object);
            
            await rlm.Initialize();
            Assert.Equal<uint>(150, rlm.SolutionEventsCookie);
        }

        [Fact]
        public async Task Dispose_ValidateSolutionEventsEstablished()
        {
            var spMock = new IServiceProviderMoq();
            spMock.AddService(typeof(IVsFileChangeEx), typeof(SVsFileChangeEx), IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(100));
            spMock.AddService(typeof(IVsSolution), typeof(SVsSolution), IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(150));

            var rlm = new ProjectReloadManager(IProjectThreadingServiceFactory.Create(), spMock, IUserNotificationServicesFactory.ImplementReportErrorInfo(), new Mock<IDialogServices>().Object);
            
            await rlm.DisposeAsync();
            Assert.Equal<uint>(0, rlm.SolutionEventsCookie);
        }

        [Fact]
        public async Task Dispose_ValidateRegisterProject()
        {
            var spMock = new IServiceProviderMoq();
            spMock.AddService(typeof(IVsFileChangeEx), typeof(SVsFileChangeEx), IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(100));
            spMock.AddService(typeof(IVsSolution), typeof(SVsSolution), IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(150));

            var rlm = new ProjectReloadManager(IProjectThreadingServiceFactory.Create(), spMock, IUserNotificationServicesFactory.ImplementReportErrorInfo(), new Mock<IDialogServices>().Object);
            
            var reloadableProjectMock = new Mock<IReloadableProject>(MockBehavior.Strict);
            reloadableProjectMock.Setup(x => x.ProjectFile).Returns(@"c:\temp\project1.csproj");

            await rlm.RegisterProjectAsync(reloadableProjectMock.Object);
            Assert.Equal<int>(1, rlm.RegisteredProjects.Count);
            Assert.Same(reloadableProjectMock.Object, rlm.RegisteredProjects.First().Key);
            reloadableProjectMock.VerifyAll();
        }

        [Fact]
        public async Task Dispose_ValidateUnRegisterProject()
        {
            var spMock = new IServiceProviderMoq();
            spMock.AddService(typeof(IVsFileChangeEx), typeof(SVsFileChangeEx), IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(100));
            spMock.AddService(typeof(IVsSolution), typeof(SVsSolution), IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(150));

            var rlm = new ProjectReloadManager(IProjectThreadingServiceFactory.Create(), spMock, IUserNotificationServicesFactory.ImplementReportErrorInfo(), new Mock<IDialogServices>().Object);
            
            var reloadableProjectMock = new Mock<IReloadableProject>(MockBehavior.Strict);
            reloadableProjectMock.Setup(x => x.ProjectFile).Returns(@"c:\temp\project1.csproj");

            await rlm.RegisterProjectAsync(reloadableProjectMock.Object);
            Assert.Equal<int>(1, rlm.RegisteredProjects.Count);

            await rlm.UnregisterProjectAsync(reloadableProjectMock.Object);
            Assert.Equal<int>(0, rlm.RegisteredProjects.Count);
            reloadableProjectMock.VerifyAll();
        }

        [Theory]
        [InlineData(_VSFILECHANGEFLAGS.VSFILECHG_Size)]
        [InlineData(_VSFILECHANGEFLAGS.VSFILECHG_Time)]
        [InlineData(_VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Size)]
        public async Task FilesChanged_CallsProjectToReload(_VSFILECHANGEFLAGS flags)
        {
            string projectFile = @"c:\temp\project1.csproj";
            var spMock = new IServiceProviderMoq();
            spMock.AddService(typeof(IVsFileChangeEx), typeof(SVsFileChangeEx), IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(100));
            spMock.AddService(typeof(IVsSolution), typeof(SVsSolution), IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(150));

            var rlm = new ProjectReloadManager(IProjectThreadingServiceFactory.Create(), spMock, IUserNotificationServicesFactory.ImplementReportErrorInfo(), new Mock<IDialogServices>().Object);
            
            var reloadableProjectMock = new Mock<IReloadableProject>(MockBehavior.Strict);
            reloadableProjectMock.Setup(x => x.ProjectFile).Returns(projectFile);
            reloadableProjectMock.Setup(x => x.ReloadProjectAsync()).Returns(Task.FromResult(ProjectReloadResult.ReloadCompleted));

            await rlm.RegisterProjectAsync(reloadableProjectMock.Object);
            Assert.Equal<int>(1, rlm.RegisteredProjects.Count);

            rlm.FilesChanged(1, new string[1] {projectFile},  new uint[1] { (uint)flags });
            await rlm.ReloadDelayScheduler.LatestScheduledTask;
            reloadableProjectMock.VerifyAll();

        }
        [Fact]
        public async Task FilesChanged_SkipsNonRegisteredProjects()
        {
            string projectFile = @"c:\temp\project1.csproj";
            var spMock = new IServiceProviderMoq();
            spMock.AddService(typeof(IVsFileChangeEx), typeof(SVsFileChangeEx), IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(100));
            spMock.AddService(typeof(IVsSolution), typeof(SVsSolution), IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(150));

            var rlm = new ProjectReloadManager(IProjectThreadingServiceFactory.Create(), spMock, IUserNotificationServicesFactory.ImplementReportErrorInfo(), new Mock<IDialogServices>().Object);
            
            var reloadableProjectMock = new Mock<IReloadableProject>(MockBehavior.Strict);
            reloadableProjectMock.Setup(x => x.ProjectFile).Returns(projectFile);

            await rlm.RegisterProjectAsync(reloadableProjectMock.Object);
            Assert.Equal<int>(1, rlm.RegisteredProjects.Count);

            rlm.FilesChanged(1, new string[1] {projectFile + ".csproj"},  new uint[1] { (uint)_VSFILECHANGEFLAGS.VSFILECHG_Size });
            Assert.True(rlm.ReloadDelayScheduler.LatestScheduledTask == null);
            reloadableProjectMock.VerifyAll();
        }

        [Theory]
        [InlineData(_VSFILECHANGEFLAGS.VSFILECHG_Add)]
        [InlineData(_VSFILECHANGEFLAGS.VSFILECHG_Attr)]
        [InlineData(_VSFILECHANGEFLAGS.VSFILECHG_Del)]
        public async Task FilesChanged_SkipsNonInterestingChanges(_VSFILECHANGEFLAGS flags)
        {
            string projectFile = @"c:\temp\project1.csproj";
            var spMock = new IServiceProviderMoq();
            spMock.AddService(typeof(IVsFileChangeEx), typeof(SVsFileChangeEx), IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(100));
            spMock.AddService(typeof(IVsSolution), typeof(SVsSolution), IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(150));

            var rlm = new ProjectReloadManager(IProjectThreadingServiceFactory.Create(), spMock, IUserNotificationServicesFactory.ImplementReportErrorInfo(), new Mock<IDialogServices>().Object);
            
            var reloadableProjectMock = new Mock<IReloadableProject>(MockBehavior.Strict);
            reloadableProjectMock.Setup(x => x.ProjectFile).Returns(projectFile);

            await rlm.RegisterProjectAsync(reloadableProjectMock.Object);
            Assert.Equal<int>(1, rlm.RegisteredProjects.Count);

            rlm.FilesChanged(1, new string[1] {projectFile},  new uint[1] { (uint)flags});
            Assert.True(rlm.ReloadDelayScheduler.LatestScheduledTask == null);
            reloadableProjectMock.VerifyAll();
        }

        [Fact]
        public async Task OnAfterRenameProject_ValidateRenamedProjectIsRegisterd()
        {
            string projectFile = @"c:\temp\project1.csproj";
            var spMock = new IServiceProviderMoq();
            spMock.AddService(typeof(IVsFileChangeEx), typeof(SVsFileChangeEx), IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(100));
            spMock.AddService(typeof(IVsSolution), typeof(SVsSolution), IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(150));

            var rlm = new ProjectReloadManager(IProjectThreadingServiceFactory.Create(), spMock, IUserNotificationServicesFactory.ImplementReportErrorInfo(), new Mock<IDialogServices>().Object);
            
            var reloadableProjectMock = new Mock<IReloadableProject>(MockBehavior.Strict);
            var hierMock = new Mock<IVsHierarchy>();
            reloadableProjectMock.Setup(x => x.ProjectFile).Returns(() => projectFile);
            reloadableProjectMock.Setup(x => x.VsHierarchy).Returns(hierMock.Object);

            await rlm.RegisterProjectAsync(reloadableProjectMock.Object);
            rlm.OnAfterRenameProject(hierMock.Object);
            Assert.Same(reloadableProjectMock.Object, rlm.RegisteredProjects.First().Key);
            reloadableProjectMock.VerifyAll();
        }

    }
}
