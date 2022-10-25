// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Acquisition;

internal sealed class NetCoreRuntimeVersionsRegistryReader
{
    private static readonly string s_arm64NetCoreRegistryKeyPath = "SOFTWARE\\dotnet\\Setup\\InstalledVersions\\ARM64\\sharedfx\\";
    private static readonly string s_x64NetCoreRegistryKeyPath = "SOFTWARE\\WOW6432Node\\dotnet\\Setup\\InstalledVersions\\x64\\sharedfx\\";
    private static readonly string s_netCoreRegistryKeyName = "Microsoft.NETCore.App";

    /// <summary>
    ///     Open the registry key to read the list of versions of NetCore runtimes installed in this machine.
    /// </summary>
    /// <remarks>
    ///     This list contains all runtimes installed outside VS as standalone packages and the ones installed through VS Setup.
    /// </remarks>
    /// <returns>A list of strings representing runtime versions in the format v{Majorversion}.{MinorVersion}. i.e. "v3.1"</returns>
    public static HashSet<string>? ReadRuntimeVersionsInstalledInLocalMachine()
    {
        // TODO:
        // We assume that the projects will run under the same architecture as VS.
        // This will be the common case, but it does not cover situations where
        // a project will run under emulation (e.g. an x64 build running on an ARM64
        // system.
        string? registryKeyPath = null;
        if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            registryKeyPath = s_x64NetCoreRegistryKeyPath;
        }
        else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            registryKeyPath = s_arm64NetCoreRegistryKeyPath;
        }

        HashSet<string> runtimeVersions = new(StringComparer.OrdinalIgnoreCase);

        if (registryKeyPath is not null)
        {
            var regkey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            var subKey = regkey.OpenSubKey(registryKeyPath + s_netCoreRegistryKeyName);

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
