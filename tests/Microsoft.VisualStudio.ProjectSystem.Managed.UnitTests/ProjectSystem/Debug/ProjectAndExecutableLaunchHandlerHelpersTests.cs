// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.Debug;

public class ProjectAndExecutableLaunchHandlerHelpersTests
{
    [Theory]
    [InlineData("C:\\OutputDirectory", "/C:/OutputDirectory", "C:\\OutputDirectory")] // Windows
    [InlineData("\\mnt\\OutputDirectory", "/mnt/OutputDirectory", "/mnt/OutputDirectory")] // Linux
    [InlineData("\\mnt\\OutputDirectory", "/mnt/OutputDirectory", "/mnt\\OutputDirectory")] // mixed
    public async Task GetOutputDirectoryAsync_Returns_OutputDirectory_When_FullPath(string expectedOutputDirectoryOnWindows, string expectedOutputDirectoryOnLinux, string actualOutputDirectory)
    {
        var expectedOutputDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? expectedOutputDirectoryOnWindows : expectedOutputDirectoryOnLinux;
        var project = CreateConfiguredProject(new() { { "OutDir", expectedOutputDirectory } });

        // Act
        var outputDirectory = await ProjectAndExecutableLaunchHandlerHelpers.GetOutputDirectoryAsync(project);

        // Assert
        Assert.Equal(expectedOutputDirectory, outputDirectory);
    }

    [Fact]
    public async Task GetOutputDirectoryAsync_Returns_OutputDirectory_When_RelativePath()
    {
        // Arrange
        var expectedOutputDirectory = @"OutputDirectory";
        var project = CreateConfiguredProject(new() { { "OutDir", expectedOutputDirectory } });

        // Act
        var outputDirectory = await ProjectAndExecutableLaunchHandlerHelpers.GetOutputDirectoryAsync(project);

        // Assert
        Assert.Equal(expectedOutputDirectory, outputDirectory);
    }

    [Fact]
    public async Task GetDefaultWorkingDirectoryAsync_Returns_OutputDirectory_If_Exists()
    {
        // Arrange
        var expectedOutputDirectory = @"C:\ProjectFolder\OutputDirectory";
        var projectFolderFullPath = @"C:\ProjectFolder";
        var configuredProject = CreateConfiguredProject(new() { { "OutDir", @"OutputDirectory" } });
        var fileSystem = new IFileSystemMock();

        // Act
        fileSystem.AddFolder(expectedOutputDirectory);
        var defaultWorkingDirectory = await ProjectAndExecutableLaunchHandlerHelpers.GetDefaultWorkingDirectoryAsync(configuredProject, projectFolderFullPath, fileSystem);

        // Assert
        Assert.Equal(expectedOutputDirectory, defaultWorkingDirectory);
    }

    [Fact]
    public async Task GetDefaultWorkingDirectoryAsync_Returns_ProjectFolderFullPath_If_OutputDirectory_Does_Not_Exist()
    {
        // Arrange
        var projectFolderFullPath = @"C:\ProjectFolder";
        var configuredProject = CreateConfiguredProject(new() { { "OutDir", @"OutputDirectory" } });
        var fileSystem = new IFileSystemMock();

        // Act
        var defaultWorkingDirectory = await ProjectAndExecutableLaunchHandlerHelpers.GetDefaultWorkingDirectoryAsync(configuredProject, projectFolderFullPath, fileSystem);

        // Assert
        Assert.Equal(projectFolderFullPath, defaultWorkingDirectory);
    }

    [Fact]
    public async Task GetDefaultWorkingDirectoryAsync_Returns_ProjectFolderFullPath_If_OutputDirectory_Is_Null_Or_Empty()
    {
        // Arrange
        var projectFolderFullPath = @"C:\ProjectFolder";
        var configuredProject = CreateConfiguredProject(new() { { "OutDir", @"" } });
        var fileSystem = new IFileSystemMock();

        // Act
        var defaultWorkingDirectory = await ProjectAndExecutableLaunchHandlerHelpers.GetDefaultWorkingDirectoryAsync(configuredProject, projectFolderFullPath, fileSystem);

        // Assert
        Assert.Equal(projectFolderFullPath, defaultWorkingDirectory);
    }

    [Fact]
    public void GetFullPathOfExeFromEnvironmentPath_Returns_FullPath_If_Exists()
    {
        // Arrange
        var exeName = "ExeName.exe";
        var exeFullPath = @"C:\ExeName.exe";
        var path = @"C:\Windows\System32;C:\";
        var fileSystem = new IFileSystemMock();
        var environment = new IEnvironmentMock().ImplementGetEnvironmentVariable(path).Object;

        // Act
        fileSystem.AddFile(exeFullPath);
        var fullPathOfExeFromEnvironmentPath = ProjectAndExecutableLaunchHandlerHelpers.GetFullPathOfExeFromEnvironmentPath(exeName, environment, fileSystem);

        // Assert
        Assert.Equal(exeFullPath, fullPathOfExeFromEnvironmentPath);
    }

    [Fact]
    public void GetFullPathOfExeFromEnvironmentPath_Returns_Null_If_Not_Exists()
    {
        // Arrange
        var exeName = "ExeName.exe";
        var path = @"C:\Windows\System32;C:\";
        var fileSystem = new IFileSystemMock();
        var environment = new IEnvironmentMock().ImplementGetEnvironmentVariable(path).Object;

        // Act
        var fullPathOfExeFromEnvironmentPath = ProjectAndExecutableLaunchHandlerHelpers.GetFullPathOfExeFromEnvironmentPath(exeName, environment, fileSystem);

        // Assert
        Assert.Null(fullPathOfExeFromEnvironmentPath);
    }

    private static ConfiguredProject CreateConfiguredProject(Dictionary<string, string?> propertyNamesAndValues)
    {
        return ConfiguredProjectFactory.Create(
            services: ConfiguredProjectServicesFactory.Create(
                projectPropertiesProvider: IProjectPropertiesProviderFactory.Create(
                    commonProps: IProjectPropertiesFactory.CreateWithPropertiesAndValues(
                        propertyNamesAndValues))));
    }
}
