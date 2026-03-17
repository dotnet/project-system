// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

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

        return subKey is null
            ? []
            : GetValueNames(subKey);
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

    /// <summary>
    /// Functional equivalent of <see cref="RegistryKey.GetValueNames"/>. Implemented
    /// as net framework implementation of this method allocates 32 KB buffer on every
    /// invocation.
    /// </summary>
    private static string[] GetValueNames(RegistryKey key)
    {
        List<string> names = new List<string>();
        char[] name = new char[128];
        int nameLength = name.Length;
        SafeRegistryHandle handle = key.Handle;
        int enumIndex = 0;

        int result = NativeMethods.RegEnumValue(handle, enumIndex, name, ref nameLength, IntPtr.Zero, null, null, null);
        while (result != NativeMethods.ERROR_NO_MORE_ITEMS)
        {
            if (result == NativeMethods.ERROR_SUCCESS)
            {
                names.Add(new string(name, 0, nameLength));
                enumIndex++;
            }
            else if (result == NativeMethods.ERROR_MORE_DATA)
            {
                // Buffer was too small, increase it's size and call again for the same index
                name = new char[name.Length * 2];
            }
            else
            {
                // Unknown error, skip this item
                enumIndex++;
            }

            // Always set the name length back to the buffer size
            nameLength = name.Length;

            result = NativeMethods.RegEnumValue(handle, enumIndex, name, ref nameLength, IntPtr.Zero, null, null, null);
        }

        return names.ToArray();
    }

    private static class NativeMethods
    {
        internal const int ERROR_SUCCESS = 0;
        internal const int ERROR_MORE_DATA = 234;
        internal const int ERROR_NO_MORE_ITEMS = 259;

        [DllImport("advapi32.dll")]
        internal static extern int RegEnumValue(
            SafeRegistryHandle hKey,
            int dwIndex,
            [Out] char[] lpValueName,
            ref int lpcbValueName,
            IntPtr lpReserved_MustBeZero,
            int[]? lpType,
            byte[]? lpData,
            int[]? lpcbData);
    }
}
