// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [ProjectSystemTrait]
    public class MsBuildUtilitiesTest
    {
        [Fact]
        public void MsBuildUtilities_GetProperty_MissingProperty()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>MyPropertyValue</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                var property = MsBuildUtilities.GetProperty(project.Project, "NonExistantProperty");
                Assert.Null(property);
            }
        }

        [Fact]
        public void MsBuildUtilities_GetProperty_ExistentProperty()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>MyPropertyValue</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                var property = MsBuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
            }
        }

        [Fact]
        public void MsBuildUtilities_GetPropertyValues_ProjectRootElementNoProperty()
        {
            string projectXml =
@"<Project>
  <PropertyGroup/>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                var values = MsBuildUtilities.GetPropertyValues(project.Project, "MyProperty");
                Assert.Equal(0, values.Length);
            }
        }

        [Fact]
        public void MsBuildUtilities_GetPropertyValues_PropertyNoValue()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty/>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                var values = MsBuildUtilities.GetPropertyValues(project.Project, "MyProperty");
                Assert.Equal(0, values.Length);
            }
            
        }

        [Fact]
        public void MsBuildUtilities_GetPropertyValues_SingleValue()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>MyPropertyValue</MyProperty>
  </PropertyGroup>
</Project>";
            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                var values = MsBuildUtilities.GetPropertyValues(project.Project, "MyProperty");
                Assert.Equal(1, values.Length);
                Assert.Equal("MyPropertyValue", values[0]);
            }
        }

        [Fact]
        public void MsBuildUtilities_GetPropertyValues_MultipleValues()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1;2</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                var values = MsBuildUtilities.GetPropertyValues(project.Project, "MyProperty");
                Assert.Equal(2, values.Length);
                Assert.Equal("1", values[0]);
                Assert.Equal("2", values[1]);
            }
        }

        [Fact]
        public void MsBuildUtilities_GetPropertyValues_NonDefaultDelimiter()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1|2</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                var values = MsBuildUtilities.GetPropertyValues(project.Project, "MyProperty", '|');
                Assert.Equal(2, values.Length);
                Assert.Equal("1", values[0]);
                Assert.Equal("2", values[1]);
            }
        }

        [Fact]
        public void MsBuildUtilities_GetPropertyValues_EmptyValues()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1;   ;;;2</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                var values = MsBuildUtilities.GetPropertyValues(project.Project, "MyProperty");
                Assert.Equal(2, values.Length);
                Assert.Equal("1", values[0]);
                Assert.Equal("2", values[1]);
            }
        }

        [Fact]
        public void MsBuildUtilities_AddProperty_NoGroups()
        {
            using (var project = new MsBuildTempProjectFile())
            {
                MsBuildUtilities.AddProperty(project.Project, "MyProperty", "1");
                Assert.Equal(1, project.Project.PropertyGroups.Count);
                var group = project.Project.PropertyGroups.First();
                Assert.Equal(1, group.Properties.Count);
                var property = group.Properties.First();
                Assert.Equal("1", property.Value);
            }
        }

        [Fact]
        public void MsBuildUtilities_AddProperty_FirstGroup()
        {
            string projectXml =
@"<Project>
  <PropertyGroup/>
  <PropertyGroup/>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                MsBuildUtilities.AddProperty(project.Project, "MyProperty", "1");
                Assert.Equal(2, project.Project.PropertyGroups.Count);
                var group = project.Project.PropertyGroups.First();
                Assert.Equal(1, group.Properties.Count);
                var property = group.Properties.First();
                Assert.Equal("1", property.Value);
            }
        }

        [Fact]
        public void MsBuildUtilities_AppendPropertyValue_DefaultDelimiter()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1;2</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                MsBuildUtilities.AppendPropertyValue(project.Project, "MyProperty", "3");
                var property = MsBuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1;2;3", property.Value);
            }
        }

        [Fact]
        public void MsBuildUtilities_AppendPropertyValue_EmptyProperty()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty/>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                MsBuildUtilities.AppendPropertyValue(project.Project, "MyProperty", "1");
                var property = MsBuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1", property.Value);
            }
        }

        [Fact]
        public void MsBuildUtilities_AppendPropertyValue_MissingProperty()
        {
            using (var project = new MsBuildTempProjectFile())
            {
                MsBuildUtilities.AppendPropertyValue(project.Project, "MyProperty", "1");
                var property = MsBuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1", property.Value);
            }
        }

        [Fact]
        public void MsBuildUtilities_AppendPropertyValue_NonDefaultDelimiter()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                MsBuildUtilities.AppendPropertyValue(project.Project, "MyProperty", "2", '|');
                var property = MsBuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1|2", property.Value);
            }
        }

        [Fact]
        public void MsBuildUtilities_RemovePropertyValue_DefaultDelimiter()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1;2</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                MsBuildUtilities.RemovePropertyValue(project.Project, "MyProperty", "2");
                var property = MsBuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1", property.Value);
            }
        }

        [Fact]
        public void MsBuildUtilities_RemovePropertyValue_NonDefaultDelimiter()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1|2|3</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                MsBuildUtilities.RemovePropertyValue(project.Project, "MyProperty", "2", '|');
                var property = MsBuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1|3", property.Value);
            }
        }

        [Fact]
        public void MsBuildUtilities_RemovePropertyValue_EmptyAfterRemove()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                MsBuildUtilities.RemovePropertyValue(project.Project, "MyProperty", "1");
                var property = MsBuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal(string.Empty, property.Value);
            }
        }

        [Fact]
        public void MsBuildUtilities_RemovePropertyValue_MissingProperty()
        {
            using (var project = new MsBuildTempProjectFile())
            {
                MsBuildUtilities.RemovePropertyValue(project.Project, "MyProperty", "1");
                var property = MsBuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.Null(property);
            }
        }

        [Fact]
        public void MsBuildUtilities_RenamePropertyValue_DefaultDelimiter()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1;2</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                MsBuildUtilities.RenamePropertyValue(project.Project, "MyProperty", "2", "5");
                var property = MsBuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1;5", property.Value);
            }
        }

        [Fact]
        public void MsBuildUtilities_RenamePropertyValue_NonDefaultDelimiter()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <MyProperty>1|2|3</MyProperty>
  </PropertyGroup>
</Project>";

            using (var project = new MsBuildTempProjectFile(projectXml))
            {
                MsBuildUtilities.RenamePropertyValue(project.Project, "MyProperty", "2", "5", '|');
                var property = MsBuildUtilities.GetProperty(project.Project, "MyProperty");
                Assert.NotNull(property);
                Assert.Equal("1|5|3", property.Value);
            }
        }
    }
}
