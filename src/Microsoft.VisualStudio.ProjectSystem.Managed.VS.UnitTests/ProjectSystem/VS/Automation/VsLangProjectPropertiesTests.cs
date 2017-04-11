// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;
using VSLangProj;
using VSLangProj110;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    [ProjectSystemTrait]
    public class VSProject_VSLangProjectPropertiesTests
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
                GetVSProject(
                    Mock.Of<VSLangProj.VSProject>(),
                    threadingService: Mock.Of<IProjectThreadingService>());
            });
        }

        [Fact]
        public void Constructor_NullAsProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("project", () =>
            {
                new VSProject(
                    Mock.Of<VSLangProj.VSProject>(),
                    threadingService: Mock.Of<IProjectThreadingService>(),
                    projectProperties: Mock.Of<ActiveConfiguredProject<ProjectProperties>>(),
                    project: null);
            });
        }

        [Fact]
        public void VsLangProjectProperties_NotNull()
        {
            var unconfiguredProjectMock = new Mock<UnconfiguredProject>();
            unconfiguredProjectMock.Setup(p => p.Capabilities)
                                   .Returns((IProjectCapabilitiesScope)null);

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
            Assert.True(imports.Equals(vsproject.Imports));
            Assert.Equal(events, vsproject.Events);
        }


        [Fact]
        public void VsLangProjectProperties_ImportsAndEventsAsNonNull()
        {
            var imports = Mock.Of<Imports>();
            var importsImpl = new OrderPrecedenceImportCollection<Imports>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, (UnconfiguredProject)null)
            {
                new Lazy<Imports, IOrderPrecedenceMetadataView>(() => imports, IOrderPrecedenceMetadataViewFactory.Create("VisualBasic"))
            };
            var events = Mock.Of<VSProjectEvents>();
            var vsProjectEventsImpl = new OrderPrecedenceImportCollection<VSProjectEvents>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, (UnconfiguredProject)null)
            {
                new Lazy<VSProjectEvents, IOrderPrecedenceMetadataView>(() => events, IOrderPrecedenceMetadataViewFactory.Create("VisualBasic"))
            };

            var innerVSProjectMock = new Mock<VSLangProj.VSProject>();

            var unconfiguredProjectMock = new Mock<UnconfiguredProject>();
            unconfiguredProjectMock.Setup(p => p.Capabilities)
                                   .Returns((IProjectCapabilitiesScope)null);

            var vsproject = new VSProjectTestImpl(
                                innerVSProjectMock.Object,
                                threadingService: Mock.Of<IProjectThreadingService>(),
                                projectProperties: Mock.Of<ActiveConfiguredProject<ProjectProperties>>(),
                                project: unconfiguredProjectMock.Object);

            vsproject.SetImportsImpl(importsImpl);
            vsproject.SetVSProjectEventsImpl(vsProjectEventsImpl);

            Assert.NotNull(vsproject);
            Assert.True(imports.Equals(vsproject.Imports));
            Assert.Equal(events, vsproject.Events);
        }

        [Fact]
        public void VsLangProjectProperties_OutputTypeEx()
        {
            var setValues = new List<object>();
            var project = UnconfiguredProjectFactory.Create();
            var enumValue = new Mock<IEnumValue>();
            enumValue.Setup(s => s.DisplayName).Returns("2");
            var data = new PropertyPageData()
            {
                Category = ConfigurationGeneralBrowseObject.SchemaName,
                PropertyName = ConfigurationGeneralBrowseObject.OutputTypeExProperty,
                Value = enumValue.Object,
                SetValues = setValues
            };

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var vsLangProjectProperties = GetVSProject(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal(vsLangProjectProperties.OutputTypeEx, prjOutputTypeEx.prjOutputTypeEx_Library);

            var testValue = prjOutputTypeEx.prjOutputTypeEx_WinExe;
            vsLangProjectProperties.OutputTypeEx = testValue;
            Assert.Equal((prjOutputTypeEx)setValues.Single(), testValue);
        }

        [Fact]
        public void VsLangProjectProperties_OutputType()
        {
            var setValues = new List<object>();
            var project = UnconfiguredProjectFactory.Create();
            var enumValue = new Mock<IEnumValue>();
            enumValue.Setup(s => s.DisplayName).Returns("2");
            var data = new PropertyPageData()
            {
                Category = ConfigurationGeneralBrowseObject.SchemaName,
                PropertyName = ConfigurationGeneralBrowseObject.OutputTypeProperty,
                Value = enumValue.Object,
                SetValues = setValues
            };

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var vsLangProjectProperties = GetVSProject(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal(vsLangProjectProperties.OutputType, prjOutputType.prjOutputTypeLibrary);

            var testValue = prjOutputType.prjOutputTypeExe;
            vsLangProjectProperties.OutputType = testValue;
            Assert.Equal(setValues.Single(), testValue);
        }

        [Fact]
        public void VsLangProjectProperties_AssemblyName()
        {
            var setValues = new List<object>();
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData()
            {
                Category = ConfigurationGeneral.SchemaName,
                PropertyName = ConfigurationGeneral.AssemblyNameProperty,
                Value = "Blah",
                SetValues = setValues
            };

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var vsLangProjectProperties = GetVSProject(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal(vsLangProjectProperties.AssemblyName, "Blah");

            var testValue = "Testing";
            vsLangProjectProperties.AssemblyName = testValue;
            Assert.Equal(setValues.Single(), testValue);
        }

        [Fact]
        public void VsLangProjectProperties_FullPath()
        {
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData()
            {
                Category = ConfigurationGeneral.SchemaName,
                PropertyName = ConfigurationGeneral.TargetPathProperty,
                Value = "somepath",
            };

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var vsLangProjectProperties = GetVSProject(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal(vsLangProjectProperties.FullPath, "somepath");
        }

        [Fact]
        public void VsLangProjectProperties_AbsoluteProjectDirectory()
        {
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData()
            {
                Category = ConfigurationGeneralBrowseObject.SchemaName,
                PropertyName = ConfigurationGeneralBrowseObject.FullPathProperty,
                Value = "testvalue",
            };

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            var vsLangProjectProperties = GetVSProject(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal(vsLangProjectProperties.AbsoluteProjectDirectory, "testvalue");
        }

        [Fact]
        public void VsLangProjectProperties_ExtenderCATID()
        {
            var vsproject = GetVSProject(
                Mock.Of<VSLangProj.VSProject>(),
                threadingService: Mock.Of<IProjectThreadingService>(),
                projectProperties: Mock.Of<ActiveConfiguredProject<ProjectProperties>>());
            Assert.Null(vsproject.ExtenderCATID);
        }

        private static VSProject GetVSProject(
            VSLangProj.VSProject vsproject = null,
            IProjectThreadingService threadingService = null,
            ActiveConfiguredProject<ProjectProperties> projectProperties = null,
            UnconfiguredProject project = null)
        {
            if (project == null)
            {
                var unconfiguredProjectMock = new Mock<UnconfiguredProject>();
                unconfiguredProjectMock.Setup(p => p.Capabilities)
                                       .Returns((IProjectCapabilitiesScope)null);
                project = unconfiguredProjectMock.Object;
            }

            return new VSProject(vsproject, threadingService, projectProperties, project);
        }

        internal class VSProjectTestImpl : VSProject
        {
            public VSProjectTestImpl(
                VSLangProj.VSProject vsProject,
                IProjectThreadingService threadingService,
                ActiveConfiguredProject<ProjectProperties> projectProperties,
                UnconfiguredProject project)
                : base(vsProject, threadingService, projectProperties, project)
            {
            }

            internal void SetImportsImpl(OrderPrecedenceImportCollection<Imports> importsImpl)
            {
                ImportsImpl = importsImpl;
            }

            internal void SetVSProjectEventsImpl(OrderPrecedenceImportCollection<VSProjectEvents> vsProjectEventsImpl)
            {
                VSProjectEventsImpl = vsProjectEventsImpl;
            }
        }
    }
}
