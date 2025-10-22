// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities;

/// <summary>
/// A mock implementation of <see cref="IEnvironment"/> for testing purposes.
/// </summary>
internal class IEnvironmentMock : AbstractMock<IEnvironment>
{
    private readonly Dictionary<Environment.SpecialFolder, string> _specialFolders = new();
    private bool _is64BitOperatingSystem = true;
    private Architecture _processArchitecture = Architecture.X64;

    public IEnvironmentMock()
    {
        // Setup the mock to return values from our backing fields/dictionary
        SetupGet(m => m.Is64BitOperatingSystem).Returns(() => _is64BitOperatingSystem);
        SetupGet(m => m.ProcessArchitecture).Returns(() => _processArchitecture);
        Setup(m => m.GetFolderPath(It.IsAny<Environment.SpecialFolder>()))
            .Returns<Environment.SpecialFolder>(folder =>
            {
                if (_specialFolders.TryGetValue(folder, out string? path))
                {
                    return path;
                }
                return string.Empty;
            });
    }

    /// <summary>
    /// Gets or sets a value indicating whether the current operating system is a 64-bit operating system.
    /// </summary>
    public bool Is64BitOperatingSystem
    {
        get => _is64BitOperatingSystem;
        set => _is64BitOperatingSystem = value;
    }

    /// <summary>
    /// Gets or sets the process architecture for the currently running process.
    /// </summary>
    public Architecture ProcessArchitecture
    {
        get => _processArchitecture;
        set => _processArchitecture = value;
    }

    /// <summary>
    /// Sets the path for a special folder.
    /// </summary>
    public void SetFolderPath(Environment.SpecialFolder folder, string path)
    {
        _specialFolders[folder] = path;
    }
}
