// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Telemetry;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemTrait]
    public class ReloadableProjectTests
    {
        [Fact]
        public async Task Initialize_ValidateProjectIsRegistered()
        {
            var spMock = new IServiceProviderMoq();
            spMock.AddService(typeof(IVsFileChangeEx), typeof(SVsFileChangeEx), IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(100));
            spMock.AddService(typeof(IVsSolution), typeof(SVsSolution), IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(150));

            var rlmMock = new Mock<IProjectReloadManager>(MockBehavior.Strict);
            rlmMock.Setup(x => x.RegisterProjectAsync(It.IsAny<IReloadableProject>())).Returns(Task.CompletedTask);

            var project = new ReloadableProject(IUnconfiguredProjectVsServicesFactory.Implement(), rlmMock.Object, ITelemetryServiceFactory.Create());

            await project.Initialize();

            rlmMock.VerifyAll();
        }

        [Fact]
        public async Task Dispose_ValidateProjectIsUnRegistered()
        {
            var spMock = new IServiceProviderMoq();
            spMock.AddService(typeof(IVsFileChangeEx), typeof(SVsFileChangeEx), IVsFileChangeExFactory.CreateWithAdviseUnadviseFileChange(100));
            spMock.AddService(typeof(IVsSolution), typeof(SVsSolution), IVsSolutionFactory.CreateWithAdviseUnadviseSolutionEvents(150));

            var rlmMock = new Mock<IProjectReloadManager>(MockBehavior.Strict);
            rlmMock.Setup(x => x.UnregisterProjectAsync(It.IsAny<IReloadableProject>())).Returns(Task.CompletedTask);

            var project = new ReloadableProject(IUnconfiguredProjectVsServicesFactory.Implement(), rlmMock.Object, ITelemetryServiceFactory.Create());

            await project.DisposeAsync();

            rlmMock.VerifyAll();
        }

    }
}
