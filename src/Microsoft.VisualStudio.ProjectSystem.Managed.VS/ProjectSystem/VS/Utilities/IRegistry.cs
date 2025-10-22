// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Win32;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

/// <summary>
/// Provides access to the Windows registry in a testable manner.
/// </summary>
[ProjectSystem.ProjectSystemContract(ProjectSystem.ProjectSystemContractScope.Global, ProjectSystem.ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface IRegistry
{
    /// <summary>
    /// Opens a registry key with the specified path under the given base key.
    /// </summary>
    /// <param name="hive">The registry hive to open (e.g., LocalMachine, CurrentUser).</param>
    /// <param name="view">The registry view to use (e.g., Registry32, Registry64).</param>
    /// <param name="subKeyPath">The path to the subkey to open.</param>
    /// <param name="valueName">The name of the value to retrieve.</param>
    /// <returns>
    /// The registry key value as a string if found; otherwise, <see langword="null"/>.
    /// </returns>
    string? GetValue(RegistryHive hive, RegistryView view, string subKeyPath, string valueName);

    /// <summary>
    /// Gets the names of all values under the specified registry key.
    /// </summary>
    /// <param name="hive">The registry hive to open (e.g., LocalMachine, CurrentUser).</param>
    /// <param name="view">The registry view to use (e.g., Registry32, Registry64).</param>
    /// <param name="subKeyPath">The path to the subkey to open.</param>
    /// <returns>
    /// An array of value names if the key exists; otherwise, an empty array.
    /// </returns>
    string[] GetValueNames(RegistryHive hive, RegistryView view, string subKeyPath);
}
