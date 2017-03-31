// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;
using VSLangProj;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    [ProjectSystemTrait]
    internal class VsProjectTests
    {
        [Fact]
        public void Constructor_NullAsVsProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("vsProject", () =>
            {
                GetVSProject();
            });
        }

        [Fact]
        public void Constructor_NullAsThreadingService_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("threadingService", () =>
            {
                GetVSProject(Mock.Of<VSLangProj.VSProject>());
            });
        }

        [Fact]
        public void Constructor_NullAsProjectProperties_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("projectProperties", () =>
            {
                GetVSProject(Mock.Of<VSLangProj.VSProject>(), threadingService: Mock.Of<IProjectThreadingService>());
            });
        }

        [Fact]
        public void VsLangProjectProperties_NotNull()
        {
            var vsproject = GetVSProject(
                                Mock.Of<VSLangProj.VSProject>(),
                                threadingService: Mock.Of<IProjectThreadingService>(),
                                projectProperties: Mock.Of<ActiveConfiguredProject<ProjectProperties>>());
            Assert.NotNull(vsproject);
        }

        [Fact]
        public void VsLangProjectProperties_ImportsAndEventsAsNull()
        {
            var imports = Mock.Of<Imports>();
            var events = Mock.Of<VSProjectEvents>();
            var innerVSProjectMock = new Mock<VSLangProj.VSProject>();

            innerVSProjectMock.Setup(p => p.Imports)
                              .Returns(imports);

            innerVSProjectMock.Setup(p => p.Events)
                              .Returns(events);
            
            var vsproject = GetVSProject(
                                innerVSProjectMock.Object,
                                threadingService: Mock.Of<IProjectThreadingService>(),
                                projectProperties: Mock.Of<ActiveConfiguredProject<ProjectProperties>>());
            Assert.NotNull(vsproject);
            Assert.Equal(imports, vsproject.Imports);
            Assert.Equal(events, vsproject.Events);
        }

        [Fact]
        public void Constructor_Import_NotNull()
        {
            var imports = Mock.Of<Imports>();
            var vsproject = GetVSProject(
                                Mock.Of<VSLangProj.VSProject>(),
                                imports: imports,
                                threadingService: Mock.Of<IProjectThreadingService>(),
                                projectProperties: Mock.Of<ActiveConfiguredProject<ProjectProperties>>());
            Assert.NotNull(vsproject);
            Assert.Equal(imports, vsproject.Imports);
        }

        [Fact]
        public void Constructor_Events_NotNull()
        {
            var events = Mock.Of<VSProjectEvents>();
            var vsproject = GetVSProject(
                                Mock.Of<VSLangProj.VSProject>(),
                                projectEvents: events,
                                threadingService: Mock.Of<IProjectThreadingService>(),
                                projectProperties: Mock.Of<ActiveConfiguredProject<ProjectProperties>>());
            Assert.NotNull(vsproject);
            Assert.Equal(events, vsproject.Events);
        }

        protected static VSProject GetVSProject(
            VSLangProj.VSProject vsproject = null,
            VSProjectEvents projectEvents = null,
            Imports imports = null,
            IProjectThreadingService threadingService = null,
            ActiveConfiguredProject<ProjectProperties> projectProperties = null)
        {
            return new VSProject(vsproject, projectEvents, imports, threadingService, projectProperties);
        }
    }
}
