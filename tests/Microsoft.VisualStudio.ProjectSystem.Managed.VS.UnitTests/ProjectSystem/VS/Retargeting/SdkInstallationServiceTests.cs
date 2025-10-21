// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Win32;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

public class SdkInstallationServiceTests
{
    [Fact]
    public async Task IsSdkInstalledAsync_WhenDotNetPathIsNull_ReturnsFalse()
    {
        var fileSystem = new IFileSystemMock();
        var registry = new IRegistryMock();
        var environment = new IEnvironmentMock();

        // Configure environment to return a non-existent path
        environment.SetFolderPath(Environment.SpecialFolder.ProgramFiles, @"C:\NonExistent");

        var service = CreateInstance(fileSystem, registry, environment);

        bool result = await service.IsSdkInstalledAsync("8.0.100");

        Assert.False(result);
    }

    [Fact]
    public void GetDotNetPath_WhenRegistryHasInstallLocation_ReturnsPathFromRegistry()
    {
        var fileSystem = new IFileSystemMock();
        var registry = new IRegistryMock();
        var environment = new IEnvironmentMock();

        environment.Is64BitOperatingSystem = true;

        string installLocation = @"C:\CustomPath\dotnet";
        string dotnetExePath = Path.Combine(installLocation, "dotnet.exe");

        registry.SetValue(
            RegistryHive.LocalMachine,
            RegistryView.Registry32,
            @"SOFTWARE\dotnet\Setup\InstalledVersions\x64",
            "InstallLocation",
            installLocation);

        fileSystem.AddFile(dotnetExePath);

        var service = CreateInstance(fileSystem, registry, environment);

        string? result = service.GetDotNetPath();

        Assert.Equal(dotnetExePath, result);
    }

    [Fact]
    public void GetDotNetPath_WhenRegistryPathDoesNotExist_FallsBackToProgramFiles()
    {
        var fileSystem = new IFileSystemMock();
        var registry = new IRegistryMock();
        var environment = new IEnvironmentMock();

        environment.Is64BitOperatingSystem = true;
        environment.SetFolderPath(Environment.SpecialFolder.ProgramFiles, @"C:\Program Files");

        string dotnetPath = @"C:\Program Files\dotnet\dotnet.exe";
        fileSystem.AddFile(dotnetPath);

        var service = CreateInstance(fileSystem, registry, environment);

        string? result = service.GetDotNetPath();

        Assert.Equal(dotnetPath, result);
    }

    [Fact]
    public void GetDotNetPath_WhenDotNetNotFound_ReturnsNull()
    {
        var fileSystem = new IFileSystemMock();
        var registry = new IRegistryMock();
        var environment = new IEnvironmentMock();

        environment.SetFolderPath(Environment.SpecialFolder.ProgramFiles, @"C:\Program Files");

        var service = CreateInstance(fileSystem, registry, environment);

        string? result = service.GetDotNetPath();

        Assert.Null(result);
    }

    [Theory]
    [InlineData(true, "x64")]
    [InlineData(false, "x86")]
    public void GetDotNetPath_UsesCorrectArchitecture(bool is64Bit, string expectedArch)
    {
        var fileSystem = new IFileSystemMock();
        var registry = new IRegistryMock();
        var environment = new IEnvironmentMock();

        environment.Is64BitOperatingSystem = is64Bit;

        string installLocation = @"C:\dotnet";
        string dotnetExePath = Path.Combine(installLocation, "dotnet.exe");

        registry.SetValue(
            RegistryHive.LocalMachine,
            RegistryView.Registry32,
            $@"SOFTWARE\dotnet\Setup\InstalledVersions\{expectedArch}",
            "InstallLocation",
            installLocation);

        fileSystem.AddFile(dotnetExePath);

        var service = CreateInstance(fileSystem, registry, environment);

        string? result = service.GetDotNetPath();

        Assert.Equal(dotnetExePath, result);
    }

    [Fact]
    public void GetDotNetPath_WhenRegistryReturnsInvalidPath_FallsBackToProgramFiles()
    {
        var fileSystem = new IFileSystemMock();
        var registry = new IRegistryMock();
        var environment = new IEnvironmentMock();

        environment.Is64BitOperatingSystem = true;
        environment.SetFolderPath(Environment.SpecialFolder.ProgramFiles, @"C:\Program Files");

        // Registry points to non-existent path
        registry.SetValue(
            RegistryHive.LocalMachine,
            RegistryView.Registry32,
            @"SOFTWARE\dotnet\Setup\InstalledVersions\x64",
            "InstallLocation",
            @"C:\NonExistent");

        // But Program Files has it
        string dotnetPath = @"C:\Program Files\dotnet\dotnet.exe";
        fileSystem.AddFile(dotnetPath);

        var service = CreateInstance(fileSystem, registry, environment);

        string? result = service.GetDotNetPath();

        Assert.Equal(dotnetPath, result);
    }

    private static SdkInstallationService CreateInstance(
        IFileSystem? fileSystem = null,
        IRegistry? registry = null,
        IEnvironment? environment = null)
    {
        fileSystem ??= new IFileSystemMock();
        registry ??= new IRegistryMock();
        environment ??= new IEnvironmentMock();

        return new SdkInstallationService(fileSystem, registry, environment);
    }
}
