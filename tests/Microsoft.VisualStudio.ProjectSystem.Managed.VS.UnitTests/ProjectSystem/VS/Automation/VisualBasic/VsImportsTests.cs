// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using EnvDTE;
using Moq;
using VSLangProj;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation.VisualBasic
{
    public class VsImportsTests
    {
        [Fact]
        public void Constructor_NotNull()
        {
            var vsimports = CreateInstance(
                                Mock.Of<VSLangProj.VSProject>(),
                                Mock.Of<IProjectThreadingService>(),
                                Mock.Of<ActiveConfiguredProject<ConfiguredProject>>(),
                                Mock.Of<IProjectAccessor>(),
                                Mock.Of<IUnconfiguredProjectVsServices>(),
                                new VisualBasicNamespaceImportsList());

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
                                Mock.Of<IProjectAccessor>(),
                                Mock.Of<IUnconfiguredProjectVsServices>(),
                                Mock.Of<VisualBasicNamespaceImportsList>());

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

        private static VisualBasicVSImports CreateInstance(
            VSLangProj.VSProject vsProject,
            IProjectThreadingService threadingService,
            ActiveConfiguredProject<ConfiguredProject> activeConfiguredProject,
            IProjectAccessor projectAccessor,
            IUnconfiguredProjectVsServices vsServices,
            VisualBasicNamespaceImportsList importList)
        {
            return new VisualBasicVSImports(vsProject, threadingService, activeConfiguredProject, projectAccessor, vsServices, importList);
        }
    }
}
