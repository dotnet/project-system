// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Microsoft.Build.Execution;
using Microsoft.VisualStudio.IO;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    public class ProjectAssetsProjectEvaluationDependencyProviderTests
    {
        [Fact]
        public void GetContentIrrelevantProjectDependentFiles_ReturnsEmpty()
        {
            var provider = CreateInstance();

            var projectInstance = ProjectInstanceFactory.Create();
            var result = provider.GetContentIrrelevantProjectDependentFiles(projectInstance);

            Assert.Empty(result);
        }

        [Fact]
        public void GetContentSensitiveProjectDependentFileTimes_WhenProjectAssetsFileIsMissing_ReturnsEmpty()
        {
            string projectXml =
@"<Project>
</Project>";

            var projectInstance = ProjectInstanceFactory.Create(projectXml);

            var provider = CreateInstance();

            var result = provider.GetContentSensitiveProjectDependentFileTimes(projectInstance);

            Assert.Empty(result);
        }

        [Fact]
        public void GetContentSensitiveProjectDependentFileTimes_WhenProjectAssetsFileIsEmpty_ReturnsEmpty()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <ProjectAssetsFile></ProjectAssetsFile>
  </PropertyGroup>
</Project>";

            var projectInstance = ProjectInstanceFactory.Create(projectXml);

            var provider = CreateInstance();
            
            var result = provider.GetContentSensitiveProjectDependentFileTimes(projectInstance);

            Assert.Empty(result);
        }

        [Fact]
        public void GetContentSensitiveProjectDependentFileTimes_WhenProjectAssetsFileExists_ReturnsLastWriteTime()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <ProjectAssetsFile>C:\Project\project.assets.json</ProjectAssetsFile>
  </PropertyGroup>
</Project>";

            var lastWriteTime = DateTime.UtcNow;

            var projectInstance = ProjectInstanceFactory.Create(projectXml);

            var fileSystem = IFileSystemFactory.ImplementTryGetLastFileWriteTimeUtc((string path, out DateTime? result) => { result = lastWriteTime; return true; });

            var provider = CreateInstance(fileSystem);

            var result = provider.GetContentSensitiveProjectDependentFileTimes(projectInstance).First();

            Assert.Equal(@"C:\Project\project.assets.json", result.Key);
            Assert.Equal(lastWriteTime, result.Value);
        }

        [Fact]
        public void GetContentSensitiveProjectDependentFileTimes_WhenProjectAssetsFileDoesNotExist_ReturnsNullForLastWriteTime()
        {
            string projectXml =
@"<Project>
  <PropertyGroup>
     <ProjectAssetsFile>C:\Project\project.assets.json</ProjectAssetsFile>
  </PropertyGroup>
</Project>";

            var lastWriteTime = DateTime.UtcNow;

            var projectInstance = ProjectInstanceFactory.Create(projectXml);

            var fileSystem = IFileSystemFactory.ImplementTryGetLastFileWriteTimeUtc((string path, out DateTime? result) => { result = null; return false; });

            var provider = CreateInstance();

            var result = provider.GetContentSensitiveProjectDependentFileTimes(projectInstance).First();

            Assert.Equal(@"C:\Project\project.assets.json", result.Key);
            Assert.Null(result.Value);
        }

        private static ProjectAssetsProjectEvaluationDependencyProvider CreateInstance(IFileSystem? fileSystem = null)
        {
            fileSystem ??= IFileSystemFactory.Create();

            return new ProjectAssetsProjectEvaluationDependencyProvider(fileSystem);
        }
    }
}
