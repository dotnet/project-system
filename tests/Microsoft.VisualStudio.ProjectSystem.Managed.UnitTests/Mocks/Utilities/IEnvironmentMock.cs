// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Utilities;

/// <summary>
/// A mock implementation of <see cref="IEnvironment"/> for testing purposes.
/// </summary>
internal class IEnvironmentMock : IEnvironment
{
    private readonly Dictionary<Environment.SpecialFolder, string> _specialFolders = new();

    /// <summary>
    /// Gets or sets a value indicating whether the current operating system is a 64-bit operating system.
    /// </summary>
    public bool Is64BitOperatingSystem { get; set; } = true;

    /// <summary>
    /// Sets the path for a special folder.
    /// </summary>
    public void SetFolderPath(Environment.SpecialFolder folder, string path)
    {
        _specialFolders[folder] = path;
    }

    /// <inheritdoc/>
    public string GetFolderPath(Environment.SpecialFolder folder)
    {
        if (_specialFolders.TryGetValue(folder, out string? path))
        {
            return path;
        }

        return string.Empty;
    }
}
