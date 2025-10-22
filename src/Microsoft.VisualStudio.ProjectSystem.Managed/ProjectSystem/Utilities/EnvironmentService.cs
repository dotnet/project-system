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
    public bool Is64BitOperatingSystem => Environment.Is64BitOperatingSystem;

    /// <inheritdoc/>
    public Architecture ProcessArchitecture => RuntimeInformation.ProcessArchitecture;

    /// <inheritdoc/>
    public string GetFolderPath(Environment.SpecialFolder folder) => Environment.GetFolderPath(folder);
}
