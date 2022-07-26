// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms;

/// <summary>
/// Provides access to properties in the myapp file.
/// </summary>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.System, Cardinality = Composition.ImportCardinality.ExactlyOne)]
internal interface IMyAppFileAccessor
{
    /// <summary>
    /// Returns the current value of property as stored in the myapp file.
    /// </summary>
    Task<bool?> GetMySubMainAsync();

    /// <summary>
    /// Sets the current value of property in the myapp file.
    /// </summary>
    Task SetMySubMainAsync(string mySubMain);

    /// <summary>
    /// Returns the current value of property as stored in the myapp file.
    /// </summary>
    Task<string?> GetMainFormAsync();

    /// <summary>
    /// Sets the current value of property in the myapp file.
    /// </summary>
    Task SetMainFormAsync(string mainForm);

    /// <summary>
    /// Returns the current value of property as stored in the myapp file.
    /// </summary>
    Task<bool?> GetSingleInstanceAsync();

    /// <summary>
    /// Sets the current value of property in the myapp file.
    /// </summary>
    Task SetSingleInstanceAsync(bool singleInstance);

    /// <summary>
    /// Returns the current value of property as stored in the myapp file.
    /// </summary>
    Task<int?> GetShutdownModeAsync();

    /// <summary>
    /// Sets the current value of property in the myapp file.
    /// </summary>
    Task SetShutdownModeAsync(int shutdownMode);

    /// <summary>
    /// Returns the current value of property as stored in the myapp file.
    /// </summary>
    Task<bool?> GetEnableVisualStylesAsync();

    /// <summary>
    /// Sets the current value of property in the myapp file.
    /// </summary>
    Task SetEnableVisualStylesAsync(bool enableVisualStyles);

    /// <summary>
    /// Returns the current value of property as stored in the myapp file.
    /// </summary>
    Task<int?> GetAuthenticationModeAsync();

    /// <summary>
    /// Sets the current value of property in the myapp file.
    /// </summary>
    Task SetAuthenticationModeAsync(int authenticationMode);

    /// <summary>
    /// Returns the current value of property as stored in the myapp file.
    /// </summary>
    Task<bool?> GetSaveMySettingsOnExitAsync();

    /// <summary>
    /// Sets the current value of property in the myapp file.
    /// </summary>
    Task SetSaveMySettingsOnExitAsync(bool saveMySettingsOnExit);

    /// <summary>
    /// Returns the current value of property as stored in the myapp file.
    /// </summary>
    Task<int?> GetHighDpiModeAsync();

    /// <summary>
    /// Sets the current value of property in the myapp file.
    /// </summary>
    Task SetHighDpiModeAsync(int highDpiMode);

    /// <summary>
    /// Returns the current value of property as stored in the myapp file.
    /// </summary>
    Task<string?> GetSplashScreenAsync();

    /// <summary>
    /// Sets the current value of property in the myapp file.
    /// </summary>
    Task SetSplashScreenAsync(string splashScreen);

    /// <summary>
    /// Returns the current value of property as stored in the myapp file.
    /// </summary>
    Task<int?> GetMinimumSplashScreenDisplayTimeAsync();

    /// <summary>
    /// Sets the current value of property in the myapp file.
    /// </summary>
    Task SetMinimumSplashScreenDisplayTimeAsync(int minimumSplashScreenDisplayTime);
}
