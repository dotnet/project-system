// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Build.Construction;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Trait("UnitTest", "ProjectSystem")]
    public class ProjectReloadInterceptorTests
    {
        [Theory]
        // User sets TF or TFs, when there was none specified earlier.
        [InlineData(null, null, "net45", null)]
        [InlineData(null, null, null, "net45")]
        [InlineData(null, null, null, "net45;netcoreapp1.0")]
        // User switches from TF -> TFs.
        [InlineData(null, "net45", "net45", null)]
        [InlineData(null, "net45;netcoreapp1.0", "net45", null)]
        // User switches from TFs -> TF.
        [InlineData("net45", null, null, "net45")]
        [InlineData("net45", null, null, "net45netcoreapp1.0")]
        // Error cases.
        [InlineData(null, "net45;netcoreapp1.0", "net45;netcoreapp1.0", null)]
        [InlineData(null, null, "invalid;target;framework", null)]
        [InlineData(null, null, null, "invalid target frameworks")]
        [InlineData("invalid;target;framework", null, null, null)]
        [InlineData(null, "invalid target frameworks", null, null)]
        [InlineData(null, null, "netcoreapp1.0", "net45;netcoreapp1.0")]
        [InlineData("netcoreapp1.0", "net45;netcoreapp1.0", null, null)]
        public void Test_ProjectNeedsReload(string oldTargetFramework, string oldTargetFrameworks, string newTargetFramework, string newTargetFrameworks)
        {
            var oldProperties = CreatePropertiesCollection(oldTargetFramework, oldTargetFrameworks);
            var newProperties = CreatePropertiesCollection(newTargetFramework, newTargetFrameworks);
            var interceptor = new ProjectReloadInterceptor();
            var reloadResult = interceptor.InterceptProjectReload(oldProperties, newProperties);
            Assert.Equal(ProjectReloadResult.NeedsForceReload, reloadResult);
        }

        [Theory]
        // Unchanged TF/TFs.
        [InlineData(null, null, null, null)]
        [InlineData(null, "net45", null, "net45")]
        [InlineData(null, "net45;netcoreapp1.0", null, "net45;netcoreapp1.0")]
        // User edits the value of current TF/TFs.
        [InlineData("net45", null, "net46", null)]
        [InlineData(null, "net45;netcoreapp1.0", null, "net45")]
        [InlineData(null, "net45", null, "net45;netcoreapp1.0")]
        [InlineData("invalid;target;framework", null, "net46", null)]
        [InlineData(null, "invalid target frameworks", null, "net45;netcoreapp1.0")]
        public void Test_ProjectDoesntNeedReload(string oldTargetFramework, string oldTargetFrameworks, string newTargetFramework, string newTargetFrameworks)
        {
            var oldProperties = CreatePropertiesCollection(oldTargetFramework, oldTargetFrameworks);
            var newProperties = CreatePropertiesCollection(newTargetFramework, newTargetFrameworks);
            var interceptor = new ProjectReloadInterceptor();
            var reloadResult = interceptor.InterceptProjectReload(oldProperties, newProperties);
            Assert.Equal(ProjectReloadResult.NoAction, reloadResult);
        }

        private static ImmutableArray<ProjectPropertyElement> CreatePropertiesCollection(string targetFramework, string targetFrameworks)
        {
            var projectFileFormat = @"<Project> <PropertyGroup> {0}{1} </PropertyGroup> </Project>";
            var propertyFormat = @" <{0}>{1}</{0}> ";
            var targetFrameworkProperty = targetFramework == null ? string.Empty : string.Format(propertyFormat, ConfigurationGeneral.TargetFrameworkProperty, targetFramework);
            var targetFrameworksProperty = targetFrameworks == null ? string.Empty : string.Format(propertyFormat, ConfigurationGeneral.TargetFrameworksProperty, targetFrameworks);
            var projectFile = string.Format(projectFileFormat, targetFrameworkProperty, targetFrameworksProperty);

            using (Stream str = new MemoryStream(Encoding.UTF8.GetBytes(projectFile)))
            using (var xr = XmlReader.Create(str))
            {
                return ProjectRootElement.Create(xr).Properties.ToImmutableArray();
            }
        }
    }
}
