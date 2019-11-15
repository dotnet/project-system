// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Tree
{
    internal sealed partial class ProjectImportsSubTreeProvider
    {
        private sealed class ImplicitProjectCheck
        {
            private readonly string _programFiles86;
            private readonly string? _programFiles64;
            private readonly string _windows;

            public string? ProjectExtensionsPath { get; set; }

            public ImplicitProjectCheck()
            {
                _windows        = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                _programFiles86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                // In a 32-bit process, SpecialFolder.ProgramFiles returns the 32-bit path.
                // The 64-bit path is available via an environment variable however.
                _programFiles64 = Environment.GetEnvironmentVariable("ProgramW6432");
            }

            public bool IsImplicit(string importPath)
            {
                return (_programFiles64 != null && importPath.StartsWith(_programFiles64, StringComparisons.Paths))
                    || importPath.StartsWith(_programFiles86, StringComparisons.Paths)
                    || importPath.StartsWith(_windows, StringComparisons.Paths)
                    || (ProjectExtensionsPath != null && importPath.StartsWith(ProjectExtensionsPath, StringComparisons.Paths));
            }
        }
    }
}
