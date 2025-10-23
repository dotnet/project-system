// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities;

/// <summary>
/// Provides access to environment information.
/// </summary>
[Export(typeof(IEnvironment))]
internal class EnvironmentService : IEnvironment
{
    /// <inheritdoc/>
    public Architecture ProcessArchitecture => RuntimeInformation.ProcessArchitecture;

    /// <inheritdoc/>
    public string? GetFolderPath(Environment.SpecialFolder folder)
    {
        string path = Environment.GetFolderPath(folder);
        return string.IsNullOrEmpty(path) ? null : path;    
    }

    /// <inheritdoc/>
    public string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

    /// <inheritdoc/>
    public string ExpandEnvironmentVariables(string name)
    {
        if (name.IndexOf('%') == -1)
        {
            // There cannot be any environment variables in this string.
            // Avoid several allocations in the .NET Framework's implementation
            // of Environment.ExpandEnvironmentVariables.
            return name;
        }

        return Environment.ExpandEnvironmentVariables(name);
    }
}
