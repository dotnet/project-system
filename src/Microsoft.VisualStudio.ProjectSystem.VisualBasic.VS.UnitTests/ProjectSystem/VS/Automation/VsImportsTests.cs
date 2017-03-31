using System;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    [ProjectSystemTrait]
    internal class VsImportsTests
    {
        [Fact]
        public void Constructor_NullAsVsProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("vsProject", () =>
            {
                GetVSImports();
            });
        }

        [Fact]
        public void Constructor_NullAsThreadingService_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("threadingService", () =>
            {
                GetVSImports(Mock.Of<VSLangProj.VSProject>());
            });
        }

        [Fact]
        public void Constructor_NullAsActiveConfiguredProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("activeConfiguredProject", () =>
            {
                GetVSImports(
                    Mock.Of<VSLangProj.VSProject>(),
                    Mock.Of<IProjectThreadingService>());
            });
        }

        [Fact]
        public void Constructor_NullAsLockService_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("lockService", () =>
            {
                GetVSImports(
                    Mock.Of<VSLangProj.VSProject>(),
                    Mock.Of<IProjectThreadingService>(),
                    Mock.Of<ActiveConfiguredProject<ConfiguredProject>>());
            });
        }

        [Fact]
        public void Constructor_NotNull()
        {
            var vsimports = GetVSImports(
                                Mock.Of<VSLangProj.VSProject>(),
                                Mock.Of<IProjectThreadingService>(),
                                Mock.Of<ActiveConfiguredProject<ConfiguredProject>>(),
                                Mock.Of<IProjectLockService>());

            Assert.NotNull(vsimports);
        }

        private VSImports GetVSImports(
            VSLangProj.VSProject vsProject = null,
            IProjectThreadingService threadingService = null,
            ActiveConfiguredProject<ConfiguredProject> activeConfiguredProject = null,
            IProjectLockService lockService = null)
        {
            return new VSImports(vsProject, threadingService, activeConfiguredProject, lockService);
        }
    }
}
