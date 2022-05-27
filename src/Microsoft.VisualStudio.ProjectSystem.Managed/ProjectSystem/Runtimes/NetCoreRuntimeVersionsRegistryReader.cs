// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Win32;

namespace Microsoft.VisualStudio.ProjectSystem.Runtimes
{
    internal sealed class NetCoreRuntimeVersionsRegistryReader
    {
        private static readonly string s_netCoreRegistryKeyPath = "SOFTWARE\\WOW6432Node\\dotnet\\Setup\\InstalledVersions\\x64\\sharedfx\\";
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
            var regkey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            var subKey = regkey.OpenSubKey(s_netCoreRegistryKeyPath + s_netCoreRegistryKeyName);

            HashSet<string> runtimeVersions = new(StringComparer.OrdinalIgnoreCase);

            foreach (string valueName in subKey.GetValueNames())
            {
                // There is guarantee to always have $(Major).$(Minor)
                string versionNumber = valueName.Substring(0, valueName.LastIndexOf('.'));
                runtimeVersions.Add($"v{versionNumber}");
            }

            return runtimeVersions;
        }
    }
}
