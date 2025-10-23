// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Win32;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

/// <summary>
/// A mock implementation of <see cref="IRegistry"/> for testing purposes.
/// Use <see cref="SetValue"/> to configure registry values that should be returned by the mock.
/// </summary>
internal class IRegistryMock : AbstractMock<IRegistry>
{
    private readonly Dictionary<string, string> _registryData = new(StringComparer.OrdinalIgnoreCase);

    public IRegistryMock()
    {
        // Setup the mock to return values from our backing dictionary
        Setup(m => m.GetValue(It.IsAny<RegistryHive>(), It.IsAny<RegistryView>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<RegistryHive, RegistryView, string, string>((hive, view, subKeyPath, valueName) =>
            {
                string key = BuildKey(hive, view, subKeyPath, valueName);
                if (_registryData.TryGetValue(key, out string? value))
                {
                    return value;
                }
                return null;
            });

        Setup(m => m.GetValueNames(It.IsAny<RegistryHive>(), It.IsAny<RegistryView>(), It.IsAny<string>()))
            .Returns<RegistryHive, RegistryView, string>((hive, view, subKeyPath) =>
            {
                string keyPrefix = BuildKeyPrefix(hive, view, subKeyPath);
                var valueNames = new List<string>();

                foreach (var key in _registryData.Keys)
                {
                    if (key.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract the value name from the key
                        string valueName = key.Substring(keyPrefix.Length);
                        valueNames.Add(valueName);
                    }
                }

                return valueNames.ToArray();
            });
    }

    /// <summary>
    /// Sets a registry value for the mock to return.
    /// </summary>
    public void SetValue(RegistryHive hive, RegistryView view, string subKeyPath, string valueName, string value)
    {
        string key = BuildKey(hive, view, subKeyPath, valueName);
        _registryData[key] = value;
    }

    private static string BuildKey(RegistryHive hive, RegistryView view, string subKeyPath, string valueName)
    {
        return $"{hive}\\{view}\\{subKeyPath}\\{valueName}";
    }

    private static string BuildKeyPrefix(RegistryHive hive, RegistryView view, string subKeyPath)
    {
        return $"{hive}\\{view}\\{subKeyPath}\\";
    }
}
