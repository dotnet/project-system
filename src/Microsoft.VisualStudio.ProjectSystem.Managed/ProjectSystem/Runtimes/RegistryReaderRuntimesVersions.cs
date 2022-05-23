// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;
using Microsoft.Win32;

namespace Microsoft.VisualStudio.ProjectSystem.Runtimes
{
    internal class RegistryReaderRuntimesVersions : IRegistry
    {
        public object? ReadValueForCurrentUser(string keyPath, string name)
        {
            var regkey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            var subKey = regkey.OpenSubKey(keyPath + name);

            HashSet<string>? netCoreRegistryKeyValues = new(StringComparer.OrdinalIgnoreCase);

            foreach (string valueName in subKey.GetValueNames())
            {
                // There is guarantee to always have $(Major).$(Minor)
                string versionNumber = valueName.Substring(0, valueName.LastIndexOf('.'));
                netCoreRegistryKeyValues.Add($"v{versionNumber}");
            }

            return netCoreRegistryKeyValues;
        }
    }
}
