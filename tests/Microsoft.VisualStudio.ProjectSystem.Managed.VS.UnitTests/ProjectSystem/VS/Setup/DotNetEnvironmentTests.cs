// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

public class DotNetEnvironmentTests
{
    [Fact]
    public async Task IsSdkInstalledAsync_WhenSdkNotInRegistry_ReturnsFalse()
    {
        var fileSystem = new IFileSystemMock();
        var registry = new IRegistryMock();
        var environment = new IEnvironmentMock();

        var service = CreateInstance(fileSystem, registry, environment);

        bool result = await service.IsSdkInstalledAsync("8.0.100");

        Assert.False(result);
    }

    [Fact]
    public async Task IsSdkInstalledAsync_WhenSdkIsInRegistry_ReturnsTrue()
    {
        var fileSystem = new IFileSystemMock();
        var registry = new IRegistryMock();
        var environment = new IEnvironmentMock();

        // Setup SDK in registry
        registry.SetValue(
            RegistryHive.LocalMachine,
            RegistryView.Registry32,
            @"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sdk",
            "8.0.100",
            "8.0.100");

        registry.SetValue(
            RegistryHive.LocalMachine,
            RegistryView.Registry32,
            @"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sdk",
            "8.0.200",
            "8.0.200");

        var service = CreateInstance(fileSystem, registry, environment);

        bool result = await service.IsSdkInstalledAsync("8.0.100");

        Assert.True(result);
    }

    [Fact]
    public async Task IsSdkInstalledAsync_WithDifferentVersion_ReturnsFalse()
    {
        var fileSystem = new IFileSystemMock();
        var registry = new IRegistryMock();
        var environment = new IEnvironmentMock();

        // Setup different SDK version in registry
        registry.SetValue(
            RegistryHive.LocalMachine,
            RegistryView.Registry32,
            @"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sdk",
            "8.0.200",
            "8.0.200");

        var service = CreateInstance(fileSystem, registry, environment);

        bool result = await service.IsSdkInstalledAsync("8.0.100");

        Assert.False(result);
    }

    [Theory]
    [InlineData(Architecture.X64, "x64")]
    [InlineData(Architecture.X86, "x86")]
    [InlineData(Architecture.Arm64, "arm64")]
    [InlineData(Architecture.Arm, "arm")]
    public async Task IsSdkInstalledAsync_UsesCorrectArchitecture(Architecture architecture, string expectedArch)
    {
        var fileSystem = new IFileSystemMock();
        var registry = new IRegistryMock();
        var environment = new IEnvironmentMock();

        environment.ProcessArchitecture = architecture;

        // Setup SDK in registry for the correct architecture
        registry.SetValue(
            RegistryHive.LocalMachine,
            RegistryView.Registry32,
            $@"SOFTWARE\dotnet\Setup\InstalledVersions\{expectedArch}\sdk",
            "8.0.100",
            "8.0.100");

        var service = CreateInstance(fileSystem, registry, environment);

        bool result = await service.IsSdkInstalledAsync("8.0.100");

        Assert.True(result);
    }

    [Fact]
    public void GetDotNetHostPath_WhenRegistryHasInstallLocation_ReturnsPathFromRegistry()
    {
        var fileSystem = new IFileSystemMock();
        var registry = new IRegistryMock();
        var environment = new IEnvironmentMock();

        environment.SetFolderPath(Environment.SpecialFolder.ProgramFiles, @"C:\Program Files");

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

        string? result = service.GetDotNetHostPath();

        Assert.Equal(dotnetExePath, result);
    }

    [Fact]
    public void GetDotNetHostPath_WhenRegistryPathDoesNotExist_FallsBackToProgramFiles()
    {
        var fileSystem = new IFileSystemMock();
        var registry = new IRegistryMock();
        var environment = new IEnvironmentMock();

        environment.SetFolderPath(Environment.SpecialFolder.ProgramFiles, @"C:\Program Files");

        string dotnetPath = @"C:\Program Files\dotnet\dotnet.exe";
        fileSystem.AddFile(dotnetPath);

        var service = CreateInstance(fileSystem, registry, environment);

        string? result = service.GetDotNetHostPath();

        Assert.Equal(dotnetPath, result);
    }

    [Fact]
    public void GetDotNetHostPath_WhenDotNetNotFound_ReturnsNull()
    {
        var fileSystem = new IFileSystemMock();
        var registry = new IRegistryMock();
        var environment = new IEnvironmentMock();

        environment.SetFolderPath(Environment.SpecialFolder.ProgramFiles, @"C:\Program Files");

        var service = CreateInstance(fileSystem, registry, environment);

        string? result = service.GetDotNetHostPath();

        Assert.Null(result);
    }

    [Theory]
    [InlineData(Architecture.X64, "x64")]
    [InlineData(Architecture.X86, "x86")]
    [InlineData(Architecture.Arm64, "arm64")]
    [InlineData(Architecture.Arm, "arm")]
    public void GetDotNetHostPath_UsesCorrectArchitecture(Architecture architecture, string expectedArch)
    {
        var fileSystem = new IFileSystemMock();
        var registry = new IRegistryMock();
        var environment = new IEnvironmentMock();

        environment.ProcessArchitecture = architecture;
        environment.SetFolderPath(Environment.SpecialFolder.ProgramFiles, @"C:\Program Files");
        string installLocation = @"C:\Program Files\";

        string dotnetExePath = Path.Combine(installLocation, "dotnet.exe");

        registry.SetValue(
            RegistryHive.LocalMachine,
            RegistryView.Registry32,
            $@"SOFTWARE\dotnet\Setup\InstalledVersions\{expectedArch}",
            "InstallLocation",
            installLocation);

        fileSystem.AddFile(dotnetExePath);

        var service = CreateInstance(fileSystem, registry, environment);

        string? result = service.GetDotNetHostPath();

        Assert.Equal(dotnetExePath, result);
    }

    [Fact]
    public void GetDotNetHostPath_WhenRegistryReturnsInvalidPath_FallsBackToProgramFiles()
    {
        var fileSystem = new IFileSystemMock();
        var registry = new IRegistryMock();
        var environment = new IEnvironmentMock();

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

        string? result = service.GetDotNetHostPath();

        Assert.Equal(dotnetPath, result);
    }

    private static DotNetEnvironment CreateInstance(
        IFileSystem? fileSystem = null,
        IRegistryMock? registry = null,
        IEnvironmentMock? environment = null)
    {
        fileSystem ??= new IFileSystemMock();
        registry ??= new IRegistryMock();
        environment ??= new IEnvironmentMock();
        
        return new DotNetEnvironment(fileSystem, registry.Object, environment.Object);
    }
}
