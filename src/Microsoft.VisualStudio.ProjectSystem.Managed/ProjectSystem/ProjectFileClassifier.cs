// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    /// Attempts to classify project files for various purposes such as safety and performance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The term "project files" refers to the root project file (e.g. <c>MyProject.csproj</c>) and
    /// any other <c>.props</c> and <c>.targets</c> files it imports.
    /// </para>
    /// <para>
    /// Classifications provided are:
    /// <list type="number">
    ///   <item>
    ///     <see cref="IsNonUserEditable"/> which indicates the file is not intended to be edited by the
    ///     user. It may be generated, or ship as part of Visual Studio, MSBuild, a NuGet package, etc.
    ///   </item>
    ///   <item>
    ///     <see cref="IsNonModifiable"/> which indicates the file is not expected to change over time,
    ///     other than when it is first created. This is a subset of non-user-editable files and
    ///     generally excludes generated files which can be regenerated in response to user actions.
    ///   </item>
    /// </list>
    /// </para>
    /// </remarks>
    internal sealed class ProjectFileClassifier
    {
        private static readonly string s_windows;
        private static readonly string s_programFiles86;
        private static readonly string? s_programFiles64;
        private static readonly string? s_vsInstallationDirectory;

        private string[] _nuGetPackageFolders = Array.Empty<string>();
        private string _nuGetPackageFoldersString = "";
        private string? _projectExtensionsPath;

        /// <summary>
        /// Gets and sets the <c>MSBuildProjectExtensionsPath</c> property value for this project.
        /// Project files under this folder are considered non-modifiable.
        /// </summary>
        /// <remarks>
        /// This value is only needed for <see cref="IsNonUserEditable"/>. Files under this path
        /// are changed over time by tooling, so do not satisfy <see cref="IsNonModifiable"/>.
        /// </remarks>
        /// <remarks>
        /// Example value: <c>"C:\repos\MySolution\MyProject\obj\"</c>
        /// </remarks>
        public string? ProjectExtensionsPath
        {
            get => _projectExtensionsPath;
            set
            {
                _projectExtensionsPath = value;
                EnsureTrailingSlash(ref _projectExtensionsPath);
            }
        }

        /// <summary>
        /// Gets and sets the paths found in the <c>NuGetPackageFolders</c> property value for this project.
        /// Project files under any of these folders are considered non-modifiable. May be empty.
        /// </summary>
        /// <remarks>
        /// This value is used by both <see cref="IsNonUserEditable"/> and <see cref="IsNonModifiable"/>.
        /// Files in the NuGet package cache are not expected to change over time, once they are created.
        /// </remarks>
        /// <remarks>
        /// Example value: <c>"C:\Users\myusername\.nuget\;D:\LocalNuGetCache\"</c>
        /// </remarks>
        public string NuGetPackageFolders
        {
            get => _nuGetPackageFoldersString;
            set
            {
                Requires.NotNull(value, nameof(value));

                if (!string.Equals(_nuGetPackageFoldersString, value, StringComparisons.Paths))
                {
                    _nuGetPackageFoldersString = value;
                    _nuGetPackageFolders = value.Split(Delimiter.Semicolon, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < _nuGetPackageFolders.Length; i++)
                    {
                        EnsureTrailingSlash(ref _nuGetPackageFolders[i]);
                    }
                }
            }
        }

        static ProjectFileClassifier()
        {
            s_windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            s_programFiles86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            // In a 32-bit process, SpecialFolder.ProgramFiles returns the 32-bit path.
            // The 64-bit path is available via an environment variable however.
            s_programFiles64 = Environment.GetEnvironmentVariable("ProgramW6432");

            s_vsInstallationDirectory = GetVSInstallationDirectory();

            EnsureTrailingSlash(ref s_windows);
            EnsureTrailingSlash(ref s_programFiles86);
            EnsureTrailingSlash(ref s_programFiles64);
            EnsureTrailingSlash(ref s_vsInstallationDirectory);

            return;

            static string? GetVSInstallationDirectory()
            {
                string? dir = Environment.GetEnvironmentVariable("VSAPPIDDIR");

                // The path provided is not the installation root, but rather the location of devenv.exe.
                // __VSSPROPID.VSSPROPID_InstallDirectory has the same value.
                // Failing a better way to obtain the installation root, remove that suffix.
                // Obviously this is brittle against changes to the relative path of devenv.exe, however that seems
                // unlikely and should be easy to work around if ever needed.
                const string DevEnvExeRelativePath = "Common7\\IDE\\";

                if (dir?.EndsWith(DevEnvExeRelativePath, StringComparisons.Paths) == true)
                {
                    dir = dir.Substring(0, dir.Length - DevEnvExeRelativePath.Length);
                }

                return dir;
            }
        }

        private static void EnsureTrailingSlash([AllowNull] ref string path)
        {
            if (path is not null)
            {
                path = PathHelper.EnsureTrailingSlash(path);
            }
        }

        /// <summary>
        /// Gets whether this file is not intended to be edited by the user.
        /// </summary>
        /// <param name="filePath">The path to the file to test.</param>
        /// <returns><see langword="true"/> if the file is non-user-editable, otherwise <see langword="false"/>.</returns>
        public bool IsNonUserEditable(string filePath)
        {
            return IsNonModifiable(filePath)
                || (_projectExtensionsPath is not null && filePath.StartsWith(_projectExtensionsPath, StringComparisons.Paths));
        }

        /// <summary>
        /// Gets whether a file is expected to not be modified in place on disk once it has been created.
        /// </summary>
        /// <param name="filePath">The path to the file to test.</param>
        /// <returns><see langword="true"/> if the file is non-modifiable, otherwise <see langword="false"/>.</returns>
        public bool IsNonModifiable(string filePath)
        {
            return (s_programFiles64 is not null && filePath.StartsWith(s_programFiles64, StringComparisons.Paths))
                || filePath.StartsWith(s_programFiles86, StringComparisons.Paths)
                || filePath.StartsWith(s_windows, StringComparisons.Paths)
                || _nuGetPackageFolders.Any(static (nuGetFolder, filePath) => filePath.StartsWith(nuGetFolder, StringComparisons.Paths), filePath)
                || (s_vsInstallationDirectory is not null && filePath.StartsWith(s_vsInstallationDirectory, StringComparisons.Paths));
        }
    }
}
