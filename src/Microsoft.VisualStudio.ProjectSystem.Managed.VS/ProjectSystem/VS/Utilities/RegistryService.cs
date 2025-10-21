// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Win32;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

/// <summary>
/// Provides access to the Windows registry.
/// </summary>
[Export(typeof(IRegistry))]
internal class RegistryService : IRegistry
{
    /// <inheritdoc/>
    public string? GetValue(RegistryHive hive, RegistryView view, string subKeyPath, string valueName)
    {
        try
        {
            using RegistryKey? baseKey = RegistryKey.OpenBaseKey(hive, view);
            if (baseKey is null)
            {
                return null;
            }

            using RegistryKey? subKey = baseKey.OpenSubKey(subKeyPath);
            if (subKey is null)
            {
                return null;
            }

            return subKey.GetValue(valueName) as string;
        }
        catch
        {
            // Return null on any registry access error
            return null;
        }
    }
}
