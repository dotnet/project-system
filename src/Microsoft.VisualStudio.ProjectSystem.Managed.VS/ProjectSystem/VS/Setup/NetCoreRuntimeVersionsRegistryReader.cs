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
    ///     Returns both runtimes installed outside VS as standalone packages, and runtimes installed through VS Setup.
    ///     Values have the form <c>3.1.32</c>, <c>7.0.11</c>, <c>8.0.0-preview.7.23375.6</c>, <c>8.0.0-rc.1.23419.4</c>.
    ///     If results could not be determined, <see langword="null"/> is returned.
    /// </remarks>
    /// <returns>An array of runtime versions, or <see langword="null"/> if results could not be determined.</returns>
    public static string[]? ReadRuntimeVersionsInstalledInLocalMachine()
    {
        // TODO:
        // We assume that the projects will run under the same architecture as VS.
        // This will be the common case, but it does not cover situations where
        // a project will run under emulation (e.g. an x64 build running on an ARM64
        // system.
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => Read(X64NetCoreRegistryKeyPath),
            Architecture.Arm64 => Read(Arm64NetCoreRegistryKeyPath),
            _ => null
        };

        static string[]? Read(string registryKeyPath)
        {
            using RegistryKey regKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using RegistryKey? subKey = regKey.OpenSubKey(registryKeyPath);

            if (subKey is null)
            {
                // TODO We've seen this return null in RPS, which indicates a failure to open the sub key.
                // We should understand why this occurs, as failure to identify installed workloads here may
                // lead to us misreporting a need to install runtimes that exist on the machine outside of VS.
                System.Diagnostics.Debug.Fail("Failed to open registry sub key.");
                return null;
            }

            return subKey.GetValueNames();
        }
    }
}
