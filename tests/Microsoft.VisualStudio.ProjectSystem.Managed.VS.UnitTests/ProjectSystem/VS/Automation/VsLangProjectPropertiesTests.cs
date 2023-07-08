// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using VSLangProj;
using VSLangProj110;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    public class VSProject_VSLangProjectPropertiesTests
    {
        [Fact]
        public void NotNull()
        {
            var unconfiguredProjectMock = new Mock<UnconfiguredProject>();
            unconfiguredProjectMock.SetupGet<IProjectCapabilitiesScope?>(p => p.Capabilities)
                                   .Returns((IProjectCapabilitiesScope?)null);

            var vsproject = CreateInstance(
                                Mock.Of<VSLangProj.VSProject>(),
                                threadingService: Mock.Of<IProjectThreadingService>(),
                                projectProperties: Mock.Of<IActiveConfiguredValue<ProjectProperties>>());
            Assert.NotNull(vsproject);
        }

        [Fact]
        public void ImportsAndEventsAsNull()
        {
            var imports = Mock.Of<Imports>();
            var events = Mock.Of<VSProjectEvents>();
            var innerVSProjectMock = new Mock<VSLangProj.VSProject>();

            innerVSProjectMock.Setup(p => p.Imports)
                              .Returns(imports);

            innerVSProjectMock.Setup(p => p.Events)
                              .Returns(events);

            var vsproject = CreateInstance(
                                innerVSProjectMock.Object,
                                threadingService: Mock.Of<IProjectThreadingService>(),
                                projectProperties: Mock.Of<IActiveConfiguredValue<ProjectProperties>>());
            Assert.NotNull(vsproject);
            Assert.True(imports.Equals(vsproject.Imports));
            Assert.Equal(events, vsproject.Events);
        }

        [Fact]
        public void ImportsAndEventsAsNonNull()
        {
            var imports = Mock.Of<Imports>();
            var importsImpl = new OrderPrecedenceImportCollection<Imports>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, (UnconfiguredProject?)null)
            {
                new Lazy<Imports, IOrderPrecedenceMetadataView>(() => imports, IOrderPrecedenceMetadataViewFactory.Create("VisualBasic"))
            };
            var events = Mock.Of<VSProjectEvents>();
            var vsProjectEventsImpl = new OrderPrecedenceImportCollection<VSProjectEvents>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, (UnconfiguredProject?)null)
            {
                new Lazy<VSProjectEvents, IOrderPrecedenceMetadataView>(() => events, IOrderPrecedenceMetadataViewFactory.Create("VisualBasic"))
            };

            var innerVSProjectMock = new Mock<VSLangProj.VSProject>();

            var unconfiguredProjectMock = new Mock<UnconfiguredProject>();
            unconfiguredProjectMock.SetupGet<IProjectCapabilitiesScope?>(p => p.Capabilities)
                                   .Returns((IProjectCapabilitiesScope?)null);

            var vsproject = new VSProjectTestImpl(
                                innerVSProjectMock.Object,
                                threadingService: Mock.Of<IProjectThreadingService>(),
                                projectProperties: Mock.Of<IActiveConfiguredValue<ProjectProperties>>(),
                                project: unconfiguredProjectMock.Object,
                                buildManager: Mock.Of<BuildManager>());

            vsproject.SetImportsImpl(importsImpl);
            vsproject.SetVSProjectEventsImpl(vsProjectEventsImpl);

            Assert.NotNull(vsproject);
            Assert.True(imports.Equals(vsproject.Imports));
            Assert.Equal(events, vsproject.Events);
        }

        [Fact]
        public void OutputTypeEx()
        {
            var setValues = new List<object>();
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData(ConfigurationGeneralBrowseObject.SchemaName, ConfigurationGeneralBrowseObject.OutputTypeProperty, 4, setValues);

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = IActiveConfiguredValueFactory.ImplementValue(() => projectProperties);

            var vsLangProjectProperties = CreateInstance(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal(prjOutputTypeEx.prjOutputTypeEx_AppContainerExe, vsLangProjectProperties.OutputTypeEx);

            vsLangProjectProperties.OutputTypeEx = prjOutputTypeEx.prjOutputTypeEx_WinExe;
            Assert.Equal(nameof(prjOutputTypeEx.prjOutputTypeEx_WinExe), setValues.Single().ToString());
        }

        [Fact]
        public void OutputType()
        {
            var setValues = new List<object>();
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData(ConfigurationGeneralBrowseObject.SchemaName, ConfigurationGeneralBrowseObject.OutputTypeProperty, 1, setValues);

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = IActiveConfiguredValueFactory.ImplementValue(() => projectProperties);

            var vsLangProjectProperties = CreateInstance(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal(prjOutputType.prjOutputTypeExe, vsLangProjectProperties.OutputType);

            vsLangProjectProperties.OutputType = prjOutputType.prjOutputTypeLibrary;
            Assert.Equal(prjOutputType.prjOutputTypeLibrary, setValues.Single());
        }

        [Fact]
        public void AssemblyName()
        {
            var setValues = new List<object>();
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.AssemblyNameProperty, "Blah", setValues);

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = IActiveConfiguredValueFactory.ImplementValue(() => projectProperties);

            var vsLangProjectProperties = CreateInstance(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal("Blah", vsLangProjectProperties.AssemblyName);

            var testValue = "Testing";
            vsLangProjectProperties.AssemblyName = testValue;
            Assert.Equal(setValues.Single(), testValue);
        }

        [Fact]
        public void FullPath()
        {
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.ProjectDirProperty, "somepath");

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = IActiveConfiguredValueFactory.ImplementValue(() => projectProperties);

            var vsLangProjectProperties = CreateInstance(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal("somepath", vsLangProjectProperties.FullPath);
        }

        [Fact]
        public void AbsoluteProjectDirectory()
        {
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData(ConfigurationGeneralBrowseObject.SchemaName, ConfigurationGeneralBrowseObject.FullPathProperty, "testvalue");

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = IActiveConfiguredValueFactory.ImplementValue(() => projectProperties);

            var vsLangProjectProperties = CreateInstance(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal("testvalue", vsLangProjectProperties.AbsoluteProjectDirectory);
        }

        [Fact]
        public void ExtenderCATID()
        {
            var vsproject = CreateInstance(
                Mock.Of<VSLangProj.VSProject>(),
                threadingService: Mock.Of<IProjectThreadingService>(),
                projectProperties: Mock.Of<IActiveConfiguredValue<ProjectProperties>>(),
                buildManager: Mock.Of<BuildManager>());
            Assert.Null(vsproject.ExtenderCATID);
        }

        private static VSProject CreateInstance(
            VSLangProj.VSProject vsproject,
            IProjectThreadingService threadingService,
            IActiveConfiguredValue<ProjectProperties> projectProperties,
            UnconfiguredProject? project = null,
            BuildManager? buildManager = null)
        {
            project ??= UnconfiguredProjectFactory.Create();

            return new VSProject(vsproject, threadingService, projectProperties, project, buildManager!);
        }

        internal class VSProjectTestImpl : VSProject
        {
            public VSProjectTestImpl(
                VSLangProj.VSProject vsProject,
                IProjectThreadingService threadingService,
                IActiveConfiguredValue<ProjectProperties> projectProperties,
                UnconfiguredProject project,
                BuildManager buildManager)
                : base(vsProject, threadingService, projectProperties, project, buildManager)
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
