// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.ProjectSystem;

/// <summary>
/// A mock implementation of <see cref="IEnvironment"/> for testing purposes.
/// </summary>
internal class IEnvironmentMock : AbstractMock<IEnvironment>
{
    private readonly Dictionary<Environment.SpecialFolder, string> _specialFolders = new();
    private Architecture _processArchitecture = Architecture.X64;

    public IEnvironmentMock()
    {
        // Setup the mock to return values from our backing fields/dictionary
        SetupGet(m => m.ProcessArchitecture).Returns(() => _processArchitecture);
        Setup(m => m.GetFolderPath(It.IsAny<Environment.SpecialFolder>()))
            .Returns<Environment.SpecialFolder>(folder =>
            {
                if (_specialFolders.TryGetValue(folder, out string? path))
                {
                    return path;
                }
                return null;
            });
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
    public IEnvironmentMock SetFolderPath(Environment.SpecialFolder folder, string path)
    {
        _specialFolders[folder] = path;
        return this;
    }

    /// <summary>
    /// Sets the environment variable value to be returned for any name.
    /// </summary>
    /// <param name="value">The value to be returned.</param>
    /// <returns></returns>
    public IEnvironmentMock ImplementGetEnvironmentVariable(string value)
    {
        Setup(m => m.GetEnvironmentVariable(It.IsAny<string>())).Returns(value);
        return this;
    }

    /// <summary>
    /// Sets the environment variable value to be returned for any name.
    /// </summary>
    /// <param name="callback">The callback to invoke to retrieve the value to be returned.</param>
    /// <returns></returns>
    public IEnvironmentMock ImplementExpandEnvironmentVariables(Func<string, string> callback)
    {
        Setup(m => m.ExpandEnvironmentVariables(It.IsAny<string>())).Returns<string>((str) => callback(str));
        return this;
    }
}
