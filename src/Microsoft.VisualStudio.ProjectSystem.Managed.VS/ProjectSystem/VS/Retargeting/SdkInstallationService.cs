// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using IFileSystem = Microsoft.VisualStudio.IO.IFileSystem;
using IRegistry = Microsoft.VisualStudio.ProjectSystem.VS.Utilities.IRegistry;
using IEnvironment = Microsoft.VisualStudio.ProjectSystem.Utilities.IEnvironment;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

/// <summary>
/// Provides information about installed .NET SDKs by querying the dotnet CLI.
/// </summary>
[Export(typeof(ISdkInstallationService))]
internal class SdkInstallationService : ISdkInstallationService
{
    private readonly IFileSystem _fileSystem;
    private readonly IRegistry _registry;
    private readonly IEnvironment _environment;

    [ImportingConstructor]
    public SdkInstallationService(IFileSystem fileSystem, IRegistry registry, IEnvironment environment)
    {
        _fileSystem = fileSystem;
        _registry = registry;
        _environment = environment;
    }

    /// <inheritdoc/>
    public async Task<bool> IsSdkInstalledAsync(string sdkVersion)
    {
        try
        {
            string? dotnetPath = GetDotNetPath();
            if (dotnetPath is null)
            {
                return false;
            }

            // Run dotnet --list-sdks to get the list of installed SDKs
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = dotnetPath,
                Arguments = "--list-sdks",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            string output = await process.StandardOutput.ReadToEndAsync();
            
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return false;
            }

            // Parse the output to check if the SDK version is installed
            // Output format: "10.0.100 [C:\Program Files\dotnet\sdk]"
            using var reader = new StringReader(output);
            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
            {
                // Extract the version number (first part before the space)
                int spaceIndex = line.IndexOf(' ');
                if (spaceIndex > 0)
                {
                    string installedVersion = line.Substring(0, spaceIndex);
                    if (string.Equals(installedVersion, sdkVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
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
    public string? GetDotNetPath()
    {
        // First check the registry
        string archSubKey = _environment.Is64BitOperatingSystem ? "x64" : "x86";
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
        string programFiles = _environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string dotnetPath = Path.Combine(programFiles, "dotnet", "dotnet.exe");
        
        if (_fileSystem.FileExists(dotnetPath))
        {
            return dotnetPath;
        }

        return null;
    }
}
