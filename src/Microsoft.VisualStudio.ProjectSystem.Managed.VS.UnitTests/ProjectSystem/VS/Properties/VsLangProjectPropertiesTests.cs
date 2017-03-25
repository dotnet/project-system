// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;
using VSLangProj;
using VSLangProj110;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ProjectSystemTrait]
    public class VsLangProjectPropertiesTests
    {
        [ProjectSystemTrait]
        public class VsLangProjectPropertiesProviderTests
        {
            [Fact]
            public void Constructor_NullAsVsProject_ThrowsArgumentNull()
            {
                Assert.Throws<ArgumentNullException>("vsProject", () =>
                {
                    GetVsLangProjectProperties();
                });
            }

            [Fact]
            public void Constructor_NullAsThreadingService_ThrowsArgumentNull()
            {
                Assert.Throws<ArgumentNullException>("threadingService", () =>
                {
                    GetVsLangProjectProperties(Mock.Of<VSProject>());
                });
            }

            [Fact]
            public void Constructor_NullAsProjectProperties_ThrowsArgumentNull()
            {
                Assert.Throws<ArgumentNullException>("projectProperties", () =>
                {
                    GetVsLangProjectProperties(Mock.Of<VSProject>(), Mock.Of<IProjectThreadingService>());
                });
            }

            [Fact]
            public void VsLangProjectProperties_NotNull()
            {
                var provider = GetVsLangProjectProperties(Mock.Of<VSProject>(), Mock.Of<IProjectThreadingService>(), Mock.Of<ActiveConfiguredProject<ProjectProperties>>());
                Assert.NotNull(provider);
            }

            [Fact]
            public void VsLangProjectProperties_OutputTypeEx_Get()
            {
                var project = UnconfiguredProjectFactory.Create();
                var enumValue = new Mock<IEnumValue>();
                enumValue.Setup(s => s.DisplayName).Returns("2");
                var data = new PropertyPageData()
                {
                    Category = ConfigurationGeneralBrowseObject.SchemaName,
                    PropertyName = ConfigurationGeneralBrowseObject.OutputTypeExProperty,
                    Value = enumValue.Object,
                };

                var projectProperties = ProjectPropertiesFactory.Create(project, data);
                var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

                var vsLangProjectProperties = new VsLangProjectProperties(Mock.Of<VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
                Assert.Equal(vsLangProjectProperties.OutputTypeEx, prjOutputTypeEx.prjOutputTypeEx_Library);
            }

            [Fact]
            public void VsLangProjectProperties_OutputType_Get()
            {
                var project = UnconfiguredProjectFactory.Create();
                var enumValue = new Mock<IEnumValue>();
                enumValue.Setup(s => s.DisplayName).Returns("2");
                var data = new PropertyPageData()
                {
                    Category = ConfigurationGeneralBrowseObject.SchemaName,
                    PropertyName = ConfigurationGeneralBrowseObject.OutputTypeProperty,
                    Value = enumValue.Object,
                };

                var projectProperties = ProjectPropertiesFactory.Create(project, data);
                var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

                var vsLangProjectProperties = new VsLangProjectProperties(Mock.Of<VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
                Assert.Equal(vsLangProjectProperties.OutputType, prjOutputType.prjOutputTypeLibrary);
            }

            [Fact]
            public void VsLangProjectProperties_AssemblyName_Get()
            {
                var project = UnconfiguredProjectFactory.Create();
                var data = new PropertyPageData()
                {
                    Category = ConfigurationGeneral.SchemaName,
                    PropertyName = ConfigurationGeneral.AssemblyNameProperty,
                    Value = "Blah",
                };

                var projectProperties = ProjectPropertiesFactory.Create(project, data);
                var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

                var vsLangProjectProperties = new VsLangProjectProperties(Mock.Of<VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
                Assert.Equal(vsLangProjectProperties.AssemblyName, "Blah");
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

                var vsLangProjectProperties = new VsLangProjectProperties(Mock.Of<VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
                Assert.Equal(vsLangProjectProperties.FullPath , "somepath");
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

                var vsLangProjectProperties = new VsLangProjectProperties(Mock.Of<VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
                Assert.Equal(vsLangProjectProperties.AbsoluteProjectDirectory, "testvalue");
            }

            [Fact]
            public void VsLangProjectProperties_ExtenderCATID()
            {
                var vsLangProjectProperties = GetVsLangProjectProperties(Mock.Of<VSProject>(), Mock.Of<IProjectThreadingService>(), Mock.Of<ActiveConfiguredProject<ProjectProperties>>());
                Assert.Null(vsLangProjectProperties.ExtenderCATID);
            }

            private static VsLangProjectProperties GetVsLangProjectProperties(
                VSProject vsproject = null, IProjectThreadingService threadingService = null, ActiveConfiguredProject<ProjectProperties> projectProperties = null)
            {
                return new VsLangProjectProperties(vsproject, threadingService, projectProperties);
            }
        }
    }
}
