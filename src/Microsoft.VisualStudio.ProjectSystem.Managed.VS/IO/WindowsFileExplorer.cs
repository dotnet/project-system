// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Path = Microsoft.IO.Path;

namespace Microsoft.VisualStudio.IO
{
    [Export(typeof(IFileExplorer))]
    internal class WindowsFileExplorer : IFileExplorer
    {
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public WindowsFileExplorer(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void OpenContainingFolder(string path)
        {
            Requires.NotNullOrEmpty(path, nameof(path));

            // When 'path' doesn't exist, Explorer just opens the default
            // "Quick Access" page, so try to something better than that.
            if (_fileSystem.PathExists(path))
            {
                // Tell Explorer to open the parent folder of the item, selecting the item
                ShellExecute(string.Empty, "explorer.exe", parameters: $"/select,\"{path}\"");
            }
            else
            {
                string? parentPath = GetParentPath(path);
                if (parentPath is not null && _fileSystem.DirectoryExists(parentPath))
                {
                    OpenFolder(parentPath);
                }
            }
        }

        public void OpenFolder(string path)
        {
            Requires.NotNullOrEmpty(path, nameof(path));

            // Tell Explorer just open the contents of the folder, selecting nothing
            ShellExecute("explore", path);
        }

        protected static void ShellExecute(string operation, string filePath, string? parameters = null)
        {
            // Workaround of CLR bug 1134711; System.Diagnostics.Process.Start() does not support GB18030
            _ = ShellExecute(IntPtr.Zero, operation, filePath, parameters, lpDirectory: null, 1);
        }

        private static string? GetParentPath(string path)
        {
            // Remove trailing slashes, so that GetDirectoryName returns 
            // "Foo" in C:\Foo\Project\" instead of "C:\Foo\Project".
            if (Path.EndsInDirectorySeparator(path))
            {
                path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            return Path.GetDirectoryName(path);
        }

        [DllImport("shell32.dll", EntryPoint = "ShellExecuteW")]
        internal static extern IntPtr ShellExecute(
            IntPtr hwnd,
            [MarshalAs(UnmanagedType.LPWStr)] string lpOperation,
            [MarshalAs(UnmanagedType.LPWStr)] string lpFile,
            [MarshalAs(UnmanagedType.LPWStr)] string? lpParameters,
            [MarshalAs(UnmanagedType.LPWStr)] string? lpDirectory,
            int nShowCmd);
    }
}
