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
                GetVsLangProjectProperties();
            });
        }

        [Fact]
        public void Constructor_NullAsThreadingService_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("threadingService", () =>
            {
                GetVsLangProjectProperties(Mock.Of<VSLangProj.VSProject>());
            });
        }

        [Fact]
        public void Constructor_NullAsProjectProperties_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("projectProperties", () =>
            {
                GetVsLangProjectProperties(Mock.Of<VSLangProj.VSProject>(), Mock.Of<IProjectThreadingService>());
            });
        }

        [Fact]
        public void VsLangProjectProperties_NotNull()
        {
            var properties = GetVsLangProjectProperties(Mock.Of<VSLangProj.VSProject>(), Mock.Of<IProjectThreadingService>(), Mock.Of<ActiveConfiguredProject<ProjectProperties>>());
            Assert.NotNull(properties);
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

            var vsLangProjectProperties = new VSProject(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
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

            var vsLangProjectProperties = new VSProject(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
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

            var vsLangProjectProperties = new VSProject(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
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

            var vsLangProjectProperties = new VSProject(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
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

            var vsLangProjectProperties = new VSProject(Mock.Of<VSLangProj.VSProject>(), IProjectThreadingServiceFactory.Create(), activeConfiguredProject);
            Assert.Equal(vsLangProjectProperties.AbsoluteProjectDirectory, "testvalue");
        }

        [Fact]
        public void VsLangProjectProperties_ExtenderCATID()
        {
            var vsLangProjectProperties = GetVsLangProjectProperties(Mock.Of<VSLangProj.VSProject>(), Mock.Of<IProjectThreadingService>(), Mock.Of<ActiveConfiguredProject<ProjectProperties>>());
            Assert.Null(vsLangProjectProperties.ExtenderCATID);
        }

        private static VSProject GetVsLangProjectProperties(
            VSLangProj.VSProject vsproject = null, IProjectThreadingService threadingService = null, ActiveConfiguredProject<ProjectProperties> projectProperties = null)
        {
            return new VSProject(vsproject, threadingService, projectProperties);
        }
    }
}
