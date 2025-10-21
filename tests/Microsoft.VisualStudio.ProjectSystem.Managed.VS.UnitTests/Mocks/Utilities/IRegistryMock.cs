// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Win32;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

/// <summary>
/// A mock implementation of <see cref="IRegistry"/> for testing purposes.
/// Use <see cref="SetValue"/> to configure registry values that should be returned by the mock.
/// </summary>
internal class IRegistryMock : IRegistry
{
    private readonly Dictionary<string, string> _registryData = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Sets a registry value for the mock to return.
    /// </summary>
    public void SetValue(RegistryHive hive, RegistryView view, string subKeyPath, string valueName, string value)
    {
        string key = BuildKey(hive, view, subKeyPath, valueName);
        _registryData[key] = value;
    }

    /// <inheritdoc/>
    public string? GetValue(RegistryHive hive, RegistryView view, string subKeyPath, string valueName)
    {
        string key = BuildKey(hive, view, subKeyPath, valueName);
        if (_registryData.TryGetValue(key, out string? value))
        {
            return value;
        }

        return null;
    }

    private static string BuildKey(RegistryHive hive, RegistryView view, string subKeyPath, string valueName)
    {
        return $"{hive}\\{view}\\{subKeyPath}\\{valueName}";
    }
}
