// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using IFileSystem = Microsoft.VisualStudio.IO.IFileSystem;
using IRegistry = Microsoft.VisualStudio.ProjectSystem.VS.Utilities.IRegistry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

/// <summary>
/// Provides information about the .NET environment and installed SDKs by querying the Windows registry.
/// </summary>
[Export(typeof(IDotNetEnvironment))]
internal class DotNetEnvironment : IDotNetEnvironment
{
    private readonly IFileSystem _fileSystem;
    private readonly IRegistry _registry;
    private readonly IEnvironment _environment;

    [ImportingConstructor]
    public DotNetEnvironment(IFileSystem fileSystem, IRegistry registry, IEnvironment environment)
    {
        _fileSystem = fileSystem;
        _registry = registry;
        _environment = environment;
    }

    /// <inheritdoc/>
    public bool IsSdkInstalled(string sdkVersion)
    {
        try
        {
            string archSubKey = _environment.ProcessArchitecture.GetArchitectureString();
            string registryKey = $@"SOFTWARE\dotnet\Setup\InstalledVersions\{archSubKey}\sdk";

            // Get all value names from the sdk subkey
            string[] installedVersions = _registry.GetValueNames(
                Win32.RegistryHive.LocalMachine,
                Win32.RegistryView.Registry32,
                registryKey);

            // Check if the requested SDK version is in the list
            foreach (string installedVersion in installedVersions)
            {
                if (string.Equals(installedVersion, sdkVersion, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            // If we fail to check, assume the SDK is not installed
            return false;
        }
    }

    /// <inheritdoc/>
    public string? GetDotNetHostPath()
    {
        // First check the registry
        string archSubKey = _environment.ProcessArchitecture.GetArchitectureString();
        string registryKey = $@"SOFTWARE\dotnet\Setup\InstalledVersions\{archSubKey}";

        string? installLocation = _registry.GetValue(
            Win32.RegistryHive.LocalMachine,
            Win32.RegistryView.Registry32,
            registryKey,
            "InstallLocation");

        if (!string.IsNullOrEmpty(installLocation))
        {
            string dotnetExePath = Path.Combine(installLocation, "dotnet.exe");
            if (_fileSystem.FileExists(dotnetExePath))
            {
                return dotnetExePath;
            }
        }

        // Fallback to Program Files
        string? programFiles = _environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (programFiles is not null)
        {
            string dotnetPath = Path.Combine(programFiles, "dotnet", "dotnet.exe");

            if (_fileSystem.FileExists(dotnetPath))
            {
                return dotnetPath;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public string[]? GetInstalledRuntimeVersions(Architecture architecture)
    {
        // https://github.com/dotnet/designs/blob/96d2ddad13dcb795ff2c5c6a051753363bdfcf7d/accepted/2020/install-locations.md#globally-registered-install-location-new

        string archSubKey = _environment.ProcessArchitecture.GetArchitectureString();
        string registryKey = $@"SOFTWARE\dotnet\Setup\InstalledVersions\{archSubKey}\sharedfx\Microsoft.NETCore.App";

        string[] valueNames = _registry.GetValueNames(
            Win32.RegistryHive.LocalMachine,
            Win32.RegistryView.Registry32,
            registryKey);

        return valueNames.Length == 0 ? null : valueNames;
    }
}
