// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

internal sealed class NetCoreRuntimeVersionsRegistryReader
{
    /// <summary>
    ///     Reads the list of installed .NET Core runtimes for the specified architecture, from the registry.
    /// </summary>
    /// <remarks>
    ///     Returns runtimes installed both as standalone packages, and through VS Setup.
    ///     Values have the form <c>3.1.32</c>, <c>7.0.11</c>, <c>8.0.0-preview.7.23375.6</c>, <c>8.0.0-rc.1.23419.4</c>.
    ///     If results could not be determined, <see langword="null"/> is returned.
    /// </remarks>
    /// <param name="architecture">The runtime architecture to report results for.</param>
    /// <returns>An array of runtime versions, or <see langword="null"/> if results could not be determined.</returns>
    public static string[]? ReadRuntimeVersionsInstalledInLocalMachine(Architecture architecture)
    {
        // https://github.com/dotnet/designs/blob/96d2ddad13dcb795ff2c5c6a051753363bdfcf7d/accepted/2020/install-locations.md#globally-registered-install-location-new

        const string registryKeyPath = """SOFTWARE\dotnet\Setup\InstalledVersions\{0}\sharedfx\Microsoft.NETCore.App""";

        using RegistryKey regKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        using RegistryKey? subKey = regKey.OpenSubKey(string.Format(registryKeyPath, architecture.ToString().ToLower()));

        if (subKey is null)
        {
            System.Diagnostics.Debug.Fail("Failed to open registry sub key. This should never happen.");
            return null;
        }

        return subKey.GetValueNames();
    }
}
