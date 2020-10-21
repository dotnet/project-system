// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.IO
{
    /// <summary>
    /// A useful helper moq for IFileSystem.
    /// Use AddFile, AddFolder method to add file paths and folders paths to the file system.
    /// Maintaining the integrity of file system is the responsibility of caller. (Like creating
    /// files and folders in a proper way)
    /// </summary>
    internal class IFileSystemMock : IFileSystem
    {
        internal class FileData
        {
            public string? FileContents;
            public DateTime LastWriteTimeUtc = DateTime.MaxValue;
            public Encoding FileEncoding = Encoding.Default;

            public void SetLastWriteTime()
            {
                // Every write should increase in time and just using DateTime.UtcNow can cause issues where
                // two very fast writes return the same value. The following better simulates writes in the real world
                if (LastWriteTimeUtc == DateTime.MaxValue)
                {
                    LastWriteTimeUtc = DateTime.UtcNow;
                }
                else if (LastWriteTimeUtc == DateTime.UtcNow)
                {
                    LastWriteTimeUtc = DateTime.UtcNow.AddMilliseconds(new Random().NextDouble() * 10000);
                }
                else
                {
                    LastWriteTimeUtc = LastWriteTimeUtc.AddMilliseconds(new Random().NextDouble() * 10000);
                }
            }
        }

        private readonly HashSet<string> _folders = new(StringComparer.OrdinalIgnoreCase);

        private string? _currentDirectory;
        private string? _tempFile;

        public Dictionary<string, FileData> Files { get; } = new Dictionary<string, FileData>(StringComparer.OrdinalIgnoreCase);

        public Stream Create(string path)
        {
            WriteAllText(path, "");

            // Caller does not check the return value.
            return null!;
        }

        public void AddFile(string path, DateTime? lastWriteTime = null)
        {
            Files[path] = new FileData
            {
                FileContents = "",
                FileEncoding = Encoding.UTF8,
                LastWriteTimeUtc = lastWriteTime ?? DateTime.UtcNow
            };
        }

        public void AddFolder(string path)
        {
            _folders.Add(path);
        }

        public void SetDirectoryAttribute(string path, FileAttributes newAttribute)
        {
        }

        public IEnumerable<string> EnumerateDirectories(string path)
        {
            return _folders.Where(folderPath =>
            {
                if (!folderPath.StartsWith(path, StringComparison.OrdinalIgnoreCase) || folderPath.Equals(path, StringComparison.OrdinalIgnoreCase))
                    return false;

                var offset = folderPath[path.Length] == Path.DirectorySeparatorChar
                    ? path.Length + 1
                    : path.Length;

                return folderPath.IndexOf(Path.DirectorySeparatorChar, offset) == -1;
            });
        }

        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
        {
            return EnumerateDirectories(path);
        }

        // SearchOption is ignored and always considered fully recursive.
        // Now supports search patterns
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        {
            var files = Files.Keys.Where(filePath => filePath.StartsWith(path));

            // Need to handle at least simple wildcards. *.* and *.ext
            if (string.IsNullOrEmpty(searchPattern))
            {
                return files;
            }
            else
            {
                var regex = new Regex(WildcardToRegex(searchPattern), RegexOptions.IgnoreCase);
                return files.Where(filePath => regex.IsMatch(Path.GetFileName(filePath)));
            }
        }

        // Convert the wildcard to a regex
        public static string WildcardToRegex(string pattern)
        {
            return "^" +
               Regex.Escape(pattern)
                   .Replace("\\*", ".*")
                   .Replace("\\?", ".") + "$";
        }

        public bool FileExists(string path)
        {
            return Files.ContainsKey(path);
        }

        public bool DirectoryExists(string path)
        {
            return _folders.Contains(path);
        }

        public void CreateDirectory(string path)
        {
            if (!DirectoryExists(path))
            {
                _folders.Add(path);
            }
        }

        public void SetCurrentDirectory(string directory)
        {
            CreateDirectory(directory);
            _currentDirectory = directory;
        }

        public string GetCurrentDirectory()
        {
            return _currentDirectory!;
        }

        public string GetFullPath(string path)
        {
            if (_currentDirectory != null)
            {
                var pathRoot = Path.GetPathRoot(path);
                if (pathRoot == @"\")
                {
                    return Path.GetPathRoot(_currentDirectory) + path.Substring(1);
                }
                else if (!Path.IsPathRooted(path))
                {
                    return Path.Combine(_currentDirectory, path);
                }
            }

            return Path.GetFullPath(path);
        }

        public void RemoveFile(string path)
        {
            if (!FileExists(path))
            {
                throw new FileNotFoundException();
            }
            Files.Remove(path);
        }

        public void CopyFile(string source, string destination, bool overwrite)
        {
            if (!FileExists(destination))
            {
                Files.Add(destination, Files[source]);
            }
        }

        public string ReadAllText(string path)
        {
            if (!FileExists(path))
            {
                throw new FileNotFoundException();
            }
            return Files[path].FileContents!;
        }

        public void WriteAllText(string path, string content)
        {
            WriteAllText(path, content, Encoding.Default);
        }

        public void WriteAllText(string path, string content, Encoding encoding)
        {
            if (Files.TryGetValue(path, out FileData data))
            {
                // This makes sure each write to the file increases the timestamp
                data.FileContents = content;
                data.FileEncoding = encoding;
                data.SetLastWriteTime();
            }
            else
            {
                Files[path] = new FileData
                {
                    FileContents = content,
                    FileEncoding = encoding,
                    LastWriteTimeUtc = DateTime.UtcNow
                };
            }
        }

        public DateTime GetLastFileWriteTimeOrMinValueUtc(string path)
        {
            if (TryGetLastFileWriteTimeUtc(path, out DateTime? result))
            {
                return result.Value;
            }

            return DateTime.MinValue;
        }

        public bool TryGetLastFileWriteTimeUtc(string path, [NotNullWhen(true)]out DateTime? result)
        {
            if (Files.TryGetValue(path, out FileData value))
            {
                result = value.LastWriteTimeUtc;
                return true;
            }

            result = null;
            return false;
        }

        public void RemoveDirectory(string directoryPath, bool recursive)
        {
            bool found = _folders.Remove(directoryPath);
            if (!found)
            {
                throw new DirectoryNotFoundException();
            }

            if (recursive)
            {
                foreach (var item in Files.Where(file => file.Key.StartsWith(directoryPath)).ToList())
                {
                    Files.Remove(item.Key);
                }
            }
        }

        public void SetTempFile(string tempFile)
        {
            _tempFile = tempFile;
        }

        public string GetTempDirectoryOrFileName()
        {
            return _tempFile!;
        }

        public void WriteAllBytes(string path, byte[] bytes)
        {
        }

        public long FileLength(string filename)
        {
            return ReadAllText(filename).Length;
        }

        public bool PathExists(string path)
        {
            throw new NotImplementedException();
        }
    }
}
