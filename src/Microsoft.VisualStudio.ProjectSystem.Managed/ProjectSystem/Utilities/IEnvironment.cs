// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.ProjectSystem;

/// <summary>
/// Provides access to environment information in a testable manner.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface IEnvironment
{
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
    /// otherwise, null.
    /// </returns>
    string? GetFolderPath(Environment.SpecialFolder folder);

    /// <summary>
    /// Retrieves the value of an environment variable from the current process.
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>
    /// The value of the environment variable specified by <paramref name="name"/>, or <see langword="null"/> if the environment variable is not found.
    /// </returns>
    string? GetEnvironmentVariable(string name);

    /// <summary>
    /// Replaces the name of each environment variable embedded in the specified string with the string equivalent of the value of the variable, then returns the resulting string.
    /// </summary>
    /// <param name="name">A string containing the names of zero or more environment variables. Each environment variable is quoted with the percent sign character (%).</param>
    /// <returns>
    /// A string with each environment variable replaced by its value.
    /// </returns>
    string ExpandEnvironmentVariables(string name);
}
