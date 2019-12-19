// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.ProjectImports
{
    internal sealed partial class ImportTreeProvider
    {
        private sealed class ImplicitProjectCheck
        {
            private readonly string _windows;
            private readonly string _programFiles86;
            private readonly string? _programFiles64;
            private readonly string? _vsInstallationDirectory;

            /// <summary>
            /// Gets and sets the <c>MSBuildProjectExtensionsPath</c> property value for this project.
            /// Project files under this folder are considered implicit.
            /// </summary>
            /// <remarks>
            /// Example value: <c>"C:\repos\MySolution\MyProject\obj\"</c>
            /// </remarks>
            public string? ProjectExtensionsPath { get; set; }

            public ImplicitProjectCheck()
            {
                _windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                _programFiles86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                // In a 32-bit process, SpecialFolder.ProgramFiles returns the 32-bit path.
                // The 64-bit path is available via an environment variable however.
                _programFiles64 = Environment.GetEnvironmentVariable("ProgramW6432");

                _vsInstallationDirectory = GetVSInstallationDirectory();

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

                    if (dir != null && dir.EndsWith(DevEnvExeRelativePath))
                    {
                        dir = dir.Substring(0, dir.Length - DevEnvExeRelativePath.Length);
                    }

                    return dir;
                }
            }

            public bool IsImplicit(string importPath)
            {
                return (_programFiles64 != null && importPath.StartsWith(_programFiles64, StringComparisons.Paths))
                    || importPath.StartsWith(_programFiles86, StringComparisons.Paths)
                    || importPath.StartsWith(_windows, StringComparisons.Paths)
                    || (ProjectExtensionsPath != null && importPath.StartsWith(ProjectExtensionsPath, StringComparisons.Paths)
                    || (_vsInstallationDirectory != null && importPath.StartsWith(_vsInstallationDirectory, StringComparisons.Paths)));
            }
        }
    }
}
