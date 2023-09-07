// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

internal sealed class NetCoreRuntimeVersionsRegistryReader
{
    private const string Arm64NetCoreRegistryKeyPath = """SOFTWARE\dotnet\Setup\InstalledVersions\ARM64\sharedfx\Microsoft.NETCore.App""";
    private const string X64NetCoreRegistryKeyPath = """SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App""";

    /// <summary>
    ///     Reads the list of installed .NET Core runtimes from the registry.
    /// </summary>
    /// <remarks>
    ///     This list contains both runtimes installed outside VS as standalone packages, and runtimes installed through VS Setup.
    /// </remarks>
    /// <returns>A list of strings representing runtime versions in the format <c>v{MajorVersion}.{MinorVersion}</c> (i.e. <c>"v3.1").</c></returns>
    public static HashSet<string> ReadRuntimeVersionsInstalledInLocalMachine()
    {
        // TODO:
        // We assume that the projects will run under the same architecture as VS.
        // This will be the common case, but it does not cover situations where
        // a project will run under emulation (e.g. an x64 build running on an ARM64
        // system.
        string? registryKeyPath = null;
        if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            registryKeyPath = X64NetCoreRegistryKeyPath;
        }
        else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            registryKeyPath = Arm64NetCoreRegistryKeyPath;
        }

        HashSet<string> runtimeVersions = new(StringComparer.OrdinalIgnoreCase);

        if (registryKeyPath is not null)
        {
            var regKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            var subKey = regKey.OpenSubKey(registryKeyPath);

            foreach (string valueName in subKey.GetValueNames())
            {
                // There is guarantee to always have $(Major).$(Minor)
                string versionNumber = valueName.Substring(0, valueName.LastIndexOf('.'));
                runtimeVersions.Add($"v{versionNumber}");
            }
        }

        return runtimeVersions;
    }
}
