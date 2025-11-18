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
        using RegistryKey? subKey = OpenSubKey(hive, view, subKeyPath);
        return subKey?.GetValue(valueName) as string;
    }

    /// <inheritdoc/>
    public string[] GetValueNames(RegistryHive hive, RegistryView view, string subKeyPath)
    {
        using RegistryKey? subKey = OpenSubKey(hive, view, subKeyPath);
        return subKey?.GetValueNames() ?? [];
    }

    private static RegistryKey? OpenSubKey(RegistryHive hive, RegistryView view, string subKeyPath)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view);
            return baseKey.OpenSubKey(subKeyPath);
        }
        catch (Exception ex) when (ex.IsCatchable())
        {
            // Return null on catchable registry access errors
            return null;
        }
    }
}
