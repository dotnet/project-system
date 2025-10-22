// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities;

/// <summary>
/// Provides access to environment information in a testable manner.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface IEnvironment
{
    /// <summary>
    /// Gets a value indicating whether the current operating system is a 64-bit operating system.
    /// </summary>
    bool Is64BitOperatingSystem { get; }

    /// <summary>
    /// Gets the process architecture for the currently running process.
    /// </summary>
    Architecture ProcessArchitecture { get; }

    /// <summary>
    /// Gets the path to the system special folder that is identified by the specified enumeration.
    /// </summary>
    /// <param name="folder">An enumerated constant that identifies a system special folder.</param>
    /// <returns>
    /// The path to the specified system special folder, if that folder physically exists on your computer;
    /// otherwise, an empty string ("").
    /// </returns>
    string GetFolderPath(Environment.SpecialFolder folder);
}
