// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.Build
{
    [ProjectSystemTrait]
    public class BuildUtilitiesTests
    {
        [Fact]
        public void GetProperty_MissingProperty()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>MyPropertyValue</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildProjectFile(projectXml))
            {
                var property = BuildUtilities.GetProperty(project.Project, "NonExistantProperty");
                Assert.Null(property);
            }
        }

        [Fact]
        public void GetProperty_ExistentProperty()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>MyPropertyValue</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildProjectFile(projectXml))
            {
                var property = BuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
            }
        }

        [Fact]
        public void GetPropertyValues_SingleValue()
        {
            var values = BuildUtilities.GetPropertyValues("MyPropertyValue");
            Assert.Equal(1, values.Length);
            Assert.Equal("MyPropertyValue", values[0]);
        }

        [Fact]
        public void GetPropertyValues_MultipleValues()
        {
            var values = BuildUtilities.GetPropertyValues("1;2");
            Assert.Equal(2, values.Length);
            Assert.Equal("1", values[0]);
            Assert.Equal("2", values[1]);
        }

        [Fact]
        public void GetPropertyValues_NonDefaultDelimiter()
        {
            var values = BuildUtilities.GetPropertyValues("1|2", '|');
            Assert.Equal(2, values.Length);
            Assert.Equal("1", values[0]);
            Assert.Equal("2", values[1]);
        }

        [Fact]
        public void GetPropertyValues_EmptyValues()
        {
            var values = BuildUtilities.GetPropertyValues("1;   ;;;2");
            Assert.Equal(2, values.Length);
            Assert.Equal("1", values[0]);
            Assert.Equal("2", values[1]);
        }

        [Fact]
        public void GetOrAddProperty_NoGroups()
        {
            using (var project = new MsBuildProjectFile())
            {
                BuildUtilities.GetOrAddProperty(project.Project, "MyProperty");
                Assert.Equal(1, project.Project.Properties.Count);
                Assert.Equal(1, project.Project.PropertyGroups.Count);

                var group = project.Project.PropertyGroups.First();
                Assert.Equal(1, group.Properties.Count);

                var property = group.Properties.First();
                Assert.Equal(string.Empty, property.Value);
            }
        }

        [Fact]
        public void GetOrAddProperty_FirstGroup()
        {
            string projectXml =
@"<Project>
  <PropertyGroup/>
  <PropertyGroup/>
</Project>";

            using (var project = new MsBuildProjectFile(projectXml))
            {
                BuildUtilities.GetOrAddProperty(project.Project, "MyProperty");
                Assert.Equal(1, project.Project.Properties.Count);
                Assert.Equal(2, project.Project.PropertyGroups.Count);

                var group = project.Project.PropertyGroups.First();
                Assert.Equal(1, group.Properties.Count);

                var property = group.Properties.First();
                Assert.Equal(string.Empty, property.Value);
            }
        }

        [Fact]
        public void GetOrAddProperty_ExistingProperty()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
    <MyProperty>1</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildProjectFile(projectXml))
            {
                BuildUtilities.GetOrAddProperty(project.Project, "MyProperty");
                Assert.Equal(1, project.Project.Properties.Count);
                Assert.Equal(1, project.Project.PropertyGroups.Count);

                var group = project.Project.PropertyGroups.First();
                Assert.Equal(1, group.Properties.Count);

                var property = group.Properties.First();
                Assert.Equal("1", property.Value);
            }
        }

        [Fact]
        public void AppendPropertyValue_DefaultDelimiter()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1;2</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildProjectFile(projectXml))
            {
                BuildUtilities.AppendPropertyValue(project.Project, "1;2", "MyProperty", "3");
                var property = BuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1;2;3", property.Value);
            }
        }

        [Fact]
        public void AppendPropertyValue_EmptyProperty()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty/>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildProjectFile(projectXml))
            {
                BuildUtilities.AppendPropertyValue(project.Project, "", "MyProperty", "1");
                var property = BuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1", property.Value);
            }
        }

        [Fact]
        public void AppendPropertyValue_InheritedValue()
        {
            using (var project = new MsBuildProjectFile())
            {
                BuildUtilities.AppendPropertyValue(project.Project, "1;2", "MyProperty", "3");
                var property = BuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1;2;3", property.Value);
            }
        }

        [Fact]
        public void AppendPropertyValue_MissingProperty()
        {
            using (var project = new MsBuildProjectFile())
            {
                BuildUtilities.AppendPropertyValue(project.Project, "", "MyProperty", "1");
                var property = BuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1", property.Value);
            }
        }

        [Fact]
        public void AppendPropertyValue_NonDefaultDelimiter()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildProjectFile(projectXml))
            {
                BuildUtilities.AppendPropertyValue(project.Project, "1", "MyProperty", "2", '|');
                var property = BuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1|2", property.Value);
            }
        }

        [Fact]
        public void RemovePropertyValue_DefaultDelimiter()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1;2</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildProjectFile(projectXml))
            {
                BuildUtilities.RemovePropertyValue(project.Project, "1;2", "MyProperty", "2");
                var property = BuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1", property.Value);
            }
        }

        [Fact]
        public void RemovePropertyValue_NonDefaultDelimiter()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1|2|3</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildProjectFile(projectXml))
            {
                BuildUtilities.RemovePropertyValue(project.Project, "1|2|3", "MyProperty", "2", '|');
                var property = BuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1|3", property.Value);
            }
        }

        [Fact]
        public void RemovePropertyValue_EmptyAfterRemove()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildProjectFile(projectXml))
            {
                BuildUtilities.RemovePropertyValue(project.Project, "1", "MyProperty", "1");
                var property = BuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal(string.Empty, property.Value);
            }
        }

        [Fact]
        public void RemovePropertyValue_InheritedValue()
        {
            using (var project = new MsBuildProjectFile())
            {
                BuildUtilities.RemovePropertyValue(project.Project, "1;2", "MyProperty", "1");
                var property = BuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("2", property.Value);
            }
        }

        [Fact]
        public void RemovePropertyValue_MissingProperty()
        {
            using (var project = new MsBuildProjectFile())
            {
                Assert.Throws<ArgumentException>("valueToRemove", () => BuildUtilities.RemovePropertyValue(project.Project, "", "MyProperty", "1"));
                var property = BuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal(string.Empty, property.Value);
            }
        }

        [Fact]
        public void RenamePropertyValue_DefaultDelimiter()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1;2</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildProjectFile(projectXml))
            {
                BuildUtilities.RenamePropertyValue(project.Project, "1;2", "MyProperty", "2", "5");
                var property = BuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1;5", property.Value);
            }
        }

        [Fact]
        public void RenamePropertyValue_NonDefaultDelimiter()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1|2|3</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildProjectFile(projectXml))
            {
                BuildUtilities.RenamePropertyValue(project.Project, "1|2|3", "MyProperty", "2", "5", '|');
                var property = BuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1|5|3", property.Value);
            }
        }

        [Fact]
        public void RenamePropertyValue_InheritedValue()
        {
            using (var project = new MsBuildProjectFile())
            {
                BuildUtilities.RenamePropertyValue(project.Project, "1;2", "MyProperty", "1", "3");
                var property = BuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("3;2", property.Value);
            }
        }

        [Fact]
        public void RenamePropertyValue_MissingProperty()
        {
            using (var project = new MsBuildProjectFile())
            {
                Assert.Throws<ArgumentException>("oldValue", () => BuildUtilities.RenamePropertyValue(project.Project, "", "MyProperty", "1", "2"));
                var property = BuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal(string.Empty, property.Value);
            }
        }
    }
}
