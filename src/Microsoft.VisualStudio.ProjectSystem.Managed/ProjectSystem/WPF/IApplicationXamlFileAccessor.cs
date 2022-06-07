// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.WPF;

/// <summary>
/// Provides access to interesting properties of the Application.xaml file.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Extension, Cardinality = Composition.ImportCardinality.ExactlyOne)]
internal interface IApplicationXamlFileAccessor
{
    /// <summary>
    /// Returns the current value of the <see cref="System.Windows.Application.StartupUri"/> property as stored in the Application.xaml file.
    /// </summary>
    /// <returns><see langword="null"/> if the Application.xaml does not exist or the property is not specified; the current value otherwise.</returns>
    Task<string?> GetStartupUriAsync();

    /// <summary>
    /// Sets the current value of the <see cref="System.Windows.Application.StartupUri"/> property in the Application.xaml file. Attempts to create the Application.xaml file if it does not exist.
    /// </summary>
    Task SetStartupUriAsync(string startupUri);

    /// <summary>
    /// Returns the current value of the <see cref="System.Windows.Application.ShutdownMode"/> property as stored in the Application.xaml file.
    /// </summary>
    /// <returns><see langword="null"/> if the Application.xaml does not exist or the property is not specified; the current value otherwise.</returns>
    Task<string?> GetShutdownModeAsync();

    /// <summary>
    /// Sets the current value of the <see cref="System.Windows.Application.ShutdownMode"/> property as stored in the Application.xaml file.
    /// </summary>
    Task SetShutdownModeAsync(string shutdownMode);
}
