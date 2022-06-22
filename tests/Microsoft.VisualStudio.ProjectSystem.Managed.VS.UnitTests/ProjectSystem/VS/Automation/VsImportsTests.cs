// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using EnvDTE;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    public class VsImportsTests
    {
        [Fact]
        public void Constructor_NotNull()
        {
            var vsimports = CreateInstance(
                                Mock.Of<VSLangProj.VSProject>(),
                                Mock.Of<IProjectThreadingService>(),
                                Mock.Of<IActiveConfiguredValue<ConfiguredProject>>(),
                                Mock.Of<IProjectAccessor>(),
                                Mock.Of<IUnconfiguredProjectVsServices>(),
                                VisualBasicNamespaceImportsListFactory.CreateInstance());

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
                                Mock.Of<IActiveConfiguredValue<ConfiguredProject>>(),
                                Mock.Of<IProjectAccessor>(),
                                Mock.Of<IUnconfiguredProjectVsServices>(),
                                VisualBasicNamespaceImportsListFactory.CreateInstance());

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
                    Mock.Of<IActiveConfiguredValue<ConfiguredProject>>(),
                    Mock.Of<IProjectAccessor>(),
                    Mock.Of<IUnconfiguredProjectVsServices>(),
                    VisualBasicNamespaceImportsListFactory.CreateInstance("A", "B"));

            vsimports.OnSinkAdded(dispImportsEventsMock.Object);

            vsimports.OnImportAdded(importName);
            vsimports.OnImportRemoved(importName);

            dispImportsEventsMock.VerifyAll();

            vsimports.OnSinkRemoved(dispImportsEventsMock.Object);

            vsimports.OnImportAdded(importName);
            vsimports.OnImportRemoved(importName);

            dispImportsEventsMock.Verify(d => d.ImportAdded(It.IsAny<string>()), Times.Once);
            dispImportsEventsMock.Verify(d => d.ImportRemoved(It.IsAny<string>()), Times.Once);

            Assert.Equal(2, vsimports.Count);
        }

        private static DotNetVSImports CreateInstance(
            VSLangProj.VSProject vsProject,
            IProjectThreadingService threadingService,
            IActiveConfiguredValue<ConfiguredProject> activeConfiguredProject,
            IProjectAccessor projectAccessor,
            IUnconfiguredProjectVsServices vsServices,
            DotNetNamespaceImportsList importList)
        {
            return new DotNetVSImports(vsProject, threadingService, activeConfiguredProject, projectAccessor, vsServices, importList);
        }
    }
}
