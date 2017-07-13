// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ProjectSystemTrait]
    public class VisualBasicProjectConfigurationPropertiesTests
    {
        [Fact]
        public void Constructor_NullAsProjectProperties_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("projectProperties", () => {
                new VisualBasicProjectConfigurationProperties(null, null);
            });
        }

        [Fact]
        public void Constructor_NullAsThreadingService_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("threadingService", () =>
            {
                new VisualBasicProjectConfigurationProperties(ProjectPropertiesFactory.CreateEmpty(), null);
            });
        }

        [Fact]
        public void VisualBasicProjectConfigurationProperties_CodeAnalysisRuleSet()
        {
            var setValues = new List<object>();
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData()
            {
                Category = ConfiguredBrowseObject.SchemaName,
                PropertyName = ConfiguredBrowseObject.CodeAnalysisRuleSetProperty,
                Value = "Blah",
                SetValues = setValues
            };

            var projectProperties = ProjectPropertiesFactory.Create(project, data);

            var vsLangProjectProperties = CreateInstance(projectProperties, IProjectThreadingServiceFactory.Create());
            Assert.Equal(vsLangProjectProperties.CodeAnalysisRuleSet, "Blah");

            var testValue = "Testing";
            vsLangProjectProperties.CodeAnalysisRuleSet = testValue;
            Assert.Equal(setValues.Single(), testValue);
        }

        [Fact]
        public void VisualBasicProjectConfigurationProperties_LangVersion()
        {
            var setValues = new List<object>();
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData()
            {
                Category = ConfiguredBrowseObject.SchemaName,
                PropertyName = ConfiguredBrowseObject.LangVersionProperty,
                Value = "9",
                SetValues = setValues
            };

            var projectProperties = ProjectPropertiesFactory.Create(project, data);

            var vsLangProjectProperties = CreateInstance(projectProperties, IProjectThreadingServiceFactory.Create());
            Assert.Equal(vsLangProjectProperties.LanguageVersion, "9");

            var testValue = "10";
            vsLangProjectProperties.LanguageVersion = testValue;
            Assert.Equal(setValues.Single(), testValue);
        }

        [Fact]
        public void VisualBasicProjectConfigurationProperties_OutputPath()
        {
            var setValues = new List<object>();
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData()
            {
                Category = ConfiguredBrowseObject.SchemaName,
                PropertyName = ConfiguredBrowseObject.OutputPathProperty,
                Value = "OldPath",
                SetValues = setValues
            };

            var projectProperties = ProjectPropertiesFactory.Create(project, data);

            var vsLangProjectProperties = CreateInstance(projectProperties, IProjectThreadingServiceFactory.Create());
            Assert.Equal(vsLangProjectProperties.OutputPath, "OldPath");

            var testValue = "NewPath";
            vsLangProjectProperties.OutputPath = testValue;
            Assert.Equal(setValues.Single(), testValue);
        }

        private VisualBasicProjectConfigurationProperties CreateInstance(ProjectProperties projectProperties, IProjectThreadingService projectThreadingService)
        {
            return new VisualBasicProjectConfigurationProperties(projectProperties, projectThreadingService);
        }
    }
}
