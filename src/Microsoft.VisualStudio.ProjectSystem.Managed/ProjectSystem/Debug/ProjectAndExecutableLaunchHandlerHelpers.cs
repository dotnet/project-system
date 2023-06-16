// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.Debug;

internal static class ProjectAndExecutableLaunchHandlerHelpers
{
    /// <summary>
    ///   Returns the output directory as specified by the project's <c>OutDir</c> property.
    ///   For example, <c>bin\net8.0\Debug\</c>.
    /// </summary>
    /// <returns>
    ///   The evaluated value of the <c>OutDir</c> property, or the empty string if the property is not set.
    /// </returns>
    public static async Task<string> GetOutputDirectoryAsync(this ConfiguredProject configuredProject)
    {
        Assumes.Present(configuredProject.Services.ProjectPropertiesProvider);

        IProjectProperties properties = configuredProject.Services.ProjectPropertiesProvider.GetCommonProperties();

        return await properties.GetEvaluatedPropertyValueAsync(ConfigurationGeneral.OutDirProperty);
    }

    /// <summary>
    ///   Gets the default working directory for launching the project.
    /// </summary>
    /// <returns>
    ///   <list type="bullet">
    ///     <item>
    ///       The full path to the output directory if the project specifies an output directory -and- it exists.
    ///     </item>
    ///     <item>
    ///       The <paramref name="projectFolderFullPath"/> if the project does not specify an output directory -or- the
    ///       output directory does not exist.
    ///     </item>
    ///   </list> 
    /// </returns>
    public static async Task<string> GetDefaultWorkingDirectoryAsync(ConfiguredProject configuredProject, string projectFolderFullPath, IFileSystem fileSystem)
    {
        string defaultWorkingDir = await configuredProject.GetOutputDirectoryAsync();
        if (defaultWorkingDir.Length == 0)
        {
            defaultWorkingDir = projectFolderFullPath;
        }
        else
        {
            if (!Path.IsPathRooted(defaultWorkingDir))
            {
                defaultWorkingDir = fileSystem.GetFullPath(Path.Combine(projectFolderFullPath, defaultWorkingDir));
            }

            // If the directory at OutDir doesn't exist, fall back to the project folder
            if (!fileSystem.DirectoryExists(defaultWorkingDir))
            {
                defaultWorkingDir = projectFolderFullPath;
            }
        }

        return defaultWorkingDir;
    }

    /// <summary>
    ///   Searches the path variable for the first match of <paramref name="exeToSearchFor"/>. Returns <see langword="null"/>
    ///   if not found.
    /// </summary>
    public static string? GetFullPathOfExeFromEnvironmentPath(string exeToSearchFor, IEnvironmentHelper environmentHelper, IFileSystem fileSystem)
    {
        string? pathEnv = environmentHelper.GetEnvironmentVariable("Path");

        if (Strings.IsNullOrEmpty(pathEnv))
        {
            return null;
        }

        foreach (string path in new LazyStringSplit(pathEnv, ';'))
        {
            // We don't want one bad path entry to derail the search
            try
            {
                string exePath = Path.Combine(path, exeToSearchFor);
                if (fileSystem.FileExists(exePath))
                {
                    return exePath;
                }
            }
            catch (ArgumentException)
            {
            }
        }

        return null;
    }

    /// <summary>
    ///   Returns the path to the executable to run based on the <c>RunCommand</c> property.
    /// </summary>
    /// <remarks>
    ///   If the returned value is not <see langword="null"/> it is guaranteed to end in ".exe" (on platforms
    ///   that use that extension), but it is not guaranteed to be a full path or that the file exists.
    /// </remarks>
    /// <returns>
    ///   <list type="bullet">
    ///     <item>
    ///       If <c>RunCommand</c> is not set: <see langword="null"/>.
    ///     </item> 
    ///     <item>
    ///       If <c>RunCommand</c> is just the name of an exe: the full path to the executable on disk based on
    ///       the "Path" environment variable.
    ///     </item>
    ///     <item>
    ///       Otherwise: the value of <c>RunCommand</c>.
    ///     </item>
    ///   </list>
    /// </returns>
    public static async Task<string?> GetRunCommandAsync(IProjectProperties properties, IEnvironmentHelper environment, IFileSystem fileSystem)
    {
        string runCommand = await properties.GetEvaluatedPropertyValueAsync("RunCommand");

        if (string.IsNullOrEmpty(runCommand))
        {
            return null;
        }

        // If dotnet.exe is used runCommand returns just "dotnet". The debugger is going to require a full path so we need to append the .exe
        // extension.
        if (Environment.OSVersion.Platform == PlatformID.Win32NT
            && !runCommand.EndsWith(".exe", StringComparisons.Paths))
        {
            runCommand += ".exe";
        }

        // If the path is just the name of an exe like dotnet.exe then we try to find it on the path
        if (runCommand.IndexOf(Path.DirectorySeparatorChar) == -1)
        {
            string? executable = GetFullPathOfExeFromEnvironmentPath(runCommand, environment, fileSystem);
            if (executable is not null)
            {
                runCommand = executable;
            }
        }

        return runCommand;
    }

    /// <summary>
    ///   Returns the command string to use for debugging the project, or <see langword="null"/> if the project is not a valid debug target.
    /// </summary>
    public static async Task<string?> GetTargetCommandAsync(
        IProjectProperties properties,
        IEnvironmentHelper environment,
        IFileSystem fileSystem,
        IOutputTypeChecker outputTypeChecker,
        bool validateSettings)
    {
        // First try "RunCommand" property
        string? runCommand = await GetRunCommandAsync(properties, environment, fileSystem);

        if (Strings.IsNullOrEmpty(runCommand))
        {
            // If we're launching for debug purposes, prevent someone F5'ing a class library
            if (validateSettings && await outputTypeChecker.IsLibraryAsync())
            {
                return null;
            }

            // Otherwise, fall back to "TargetPath"
            runCommand = await properties.GetEvaluatedPropertyValueAsync(ConfigurationGeneral.TargetPathProperty);
        }

        return runCommand;
    }

    /// <summary>
    ///   Queries properties from the project to get information on how to run the application. The returned Tuple contains:
    ///   <list type="bullet">
    ///     <item>exeToRun</item>
    ///     <item>arguments</item>
    ///     <item>workingDir</item>
    ///   </list>
    /// </summary>
    /// <returns>
    ///   <see langword="null"/> if the exeToRun string is <see langword="null"/> or empty. Otherwise, the tuple containing the runnable project information.
    /// </returns>
    public static async Task<(string ExeToRun, string Arguments, string WorkingDirectory)?> GetRunnableProjectInformationAsync(
        ConfiguredProject project,
        IEnvironmentHelper environment,
        IFileSystem fileSystem,
        IOutputTypeChecker outputTypeChecker,
        bool validateSettings)
    {
        Assumes.Present(project.Services.ProjectPropertiesProvider);

        IProjectProperties properties = project.Services.ProjectPropertiesProvider.GetCommonProperties();

        string? exeToRun = await GetTargetCommandAsync(properties, environment, fileSystem, outputTypeChecker, validateSettings);
        if (Strings.IsNullOrWhiteSpace(exeToRun))
        {
            return null;
        }

        string runWorkingDirectory = await properties.GetEvaluatedPropertyValueAsync("RunWorkingDirectory");
        // If the working directory is relative, it will be relative to the project root so make it a full path
        if (!string.IsNullOrWhiteSpace(runWorkingDirectory) && !Path.IsPathRooted(runWorkingDirectory))
        {
            runWorkingDirectory = Path.Combine(Path.GetDirectoryName(project.UnconfiguredProject.FullPath) ?? string.Empty, runWorkingDirectory);
        }

        string runArguments = await properties.GetEvaluatedPropertyValueAsync("RunArguments");

        return (exeToRun, runArguments, runWorkingDirectory);
    }
}
