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
    /// </remarks>
    /// <returns>An enumeration of strings representing runtime versions in the format <c>v{MajorVersion}.{MinorVersion}</c> (i.e. <c>"v3.1").</c></returns>
    public static IEnumerable<string> ReadRuntimeVersionsInstalledInLocalMachine()
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
            _ => Enumerable.Empty<string>()
        };

        static IEnumerable<string> Read(string registryKeyPath)
        {
            using RegistryKey regKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using RegistryKey subKey = regKey.OpenSubKey(registryKeyPath);

            foreach (string valueName in subKey.GetValueNames())
            {
                // There is guarantee to always have $(Major).$(Minor)
                string versionNumber = valueName.Substring(0, valueName.LastIndexOf('.'));
                yield return $"v{versionNumber}";
            }
        }
    }
}
