using System;
using EnvDTE;
using Moq;
using VSLangProj;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    [ProjectSystemTrait]
    public class VsImportsTests
    {
        [Fact]
        public void Constructor_NullAsVsProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("vsProject", () =>
            {
                CreateInstance();
            });
        }

        [Fact]
        public void Constructor_NullAsThreadingService_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("threadingService", () =>
            {
                CreateInstance(Mock.Of<VSLangProj.VSProject>());
            });
        }

        [Fact]
        public void Constructor_NullAsActiveConfiguredProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("activeConfiguredProject", () =>
            {
                CreateInstance(
                    Mock.Of<VSLangProj.VSProject>(),
                    Mock.Of<IProjectThreadingService>());
            });
        }

        [Fact]
        public void Constructor_NullAsLockService_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("lockService", () =>
            {
                CreateInstance(
                    Mock.Of<VSLangProj.VSProject>(),
                    Mock.Of<IProjectThreadingService>(),
                    Mock.Of<ActiveConfiguredProject<ConfiguredProject>>());
            });
        }

        [Fact]
        public void Constructor_NullAsIUnconfiguredProjectVsServices_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("unconfiguredProjectVSServices", () =>
            {
                CreateInstance(
                    Mock.Of<VSLangProj.VSProject>(),
                    Mock.Of<IProjectThreadingService>(),
                    Mock.Of<ActiveConfiguredProject<ConfiguredProject>>(),
                    Mock.Of<IProjectLockService>());
            });
        }

        [Fact]
        public void Constructor_NotNull()
        {
            var vsimports = CreateInstance(
                                Mock.Of<VSLangProj.VSProject>(),
                                Mock.Of<IProjectThreadingService>(),
                                Mock.Of<ActiveConfiguredProject<ConfiguredProject>>(),
                                Mock.Of<IProjectLockService>(),
                                Mock.Of<IUnconfiguredProjectVsServices>());

            Assert.NotNull(vsimports);
        }

        [Fact]
        public void VsImports_PropertiesCheck()
        {
            var dte = Mock.Of<DTE>();
            var project = Mock.Of<Project>();

            var vsProjectMock = new Mock<VSLangProj.VSProject>();
            vsProjectMock.Setup(p => p.DTE)
                         .Returns(dte);
            vsProjectMock.Setup(p => p.Project)
                         .Returns(project);

            var vsimports = CreateInstance(
                                vsProjectMock.Object,
                                Mock.Of<IProjectThreadingService>(),
                                Mock.Of<ActiveConfiguredProject<ConfiguredProject>>(),
                                Mock.Of<IProjectLockService>(),
                                Mock.Of<IUnconfiguredProjectVsServices>());

            Assert.Equal(dte, vsimports.DTE);
            Assert.Equal(project, vsimports.ContainingProject);
        }

        [Fact]
        public void VsImports_ImportsAddRemoveCheck()
        {
            var dispImportsEventsMock = new Mock<_dispImportsEvents>();
            const string importName = "Something";
            dispImportsEventsMock.Setup(d => d.ImportAdded(It.Is<string>(s => s == importName)))
                                 .Verifiable();
            dispImportsEventsMock.Setup(d => d.ImportRemoved(It.Is<string>(s => s == importName)))
                                 .Verifiable();

            var vsimports = CreateInstance(
                    Mock.Of<VSLangProj.VSProject>(),
                    Mock.Of<IProjectThreadingService>(),
                    Mock.Of<ActiveConfiguredProject<ConfiguredProject>>(),
                    Mock.Of<IProjectLockService>(),
                    Mock.Of<IUnconfiguredProjectVsServices>());

            vsimports.OnSinkAdded(dispImportsEventsMock.Object);

            vsimports.OnImportAdded(importName);
            vsimports.OnImportRemoved(importName);

            dispImportsEventsMock.VerifyAll();

            vsimports.OnSinkRemoved(dispImportsEventsMock.Object);

            vsimports.OnImportAdded(importName);
            vsimports.OnImportRemoved(importName);

            dispImportsEventsMock.Verify(d => d.ImportAdded(It.IsAny<string>()), Times.Once);
            dispImportsEventsMock.Verify(d => d.ImportRemoved(It.IsAny<string>()), Times.Once);
        }

        private VisualBasicVSImports CreateInstance(
            VSLangProj.VSProject vsProject = null,
            IProjectThreadingService threadingService = null,
            ActiveConfiguredProject<ConfiguredProject> activeConfiguredProject = null,
            IProjectLockService lockService = null,
            IUnconfiguredProjectVsServices vsServices = null)
        {
            return new VisualBasicVSImports(vsProject, threadingService, activeConfiguredProject, lockService, vsServices);
        }
    }
}
