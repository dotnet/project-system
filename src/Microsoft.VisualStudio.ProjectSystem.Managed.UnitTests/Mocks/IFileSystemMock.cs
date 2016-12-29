// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
            public string FileContents;
            public DateTime LastWriteTime = DateTime.MaxValue;
            public Encoding FileEncoding = Encoding.Default;
        };

        Dictionary<string, FileData> _files = new Dictionary<string, FileData>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> _folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, FileData> Files { get => _files; }

        public Stream Create(string path)
        {
            WriteAllText(path, "");

            // Caller does not check the return value.
            return null;
        }

        private void SetLastWriteTime(FileData data)
        {
            // Every write should increase in time and just using DateTime.Now can cause issues where
            // two very fast writes return the same value. The following better simulates writes in the real world
            if (data.LastWriteTime == DateTime.MaxValue)
            {
                data.LastWriteTime = DateTime.Now;
            }
            else if (data.LastWriteTime == DateTime.Now)
            {
                data.LastWriteTime = DateTime.Now.AddMilliseconds(new Random().NextDouble() * 10000);
            }
            else
            {
                data.LastWriteTime = data.LastWriteTime.AddMilliseconds(new Random().NextDouble() * 10000);
            }
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

                var subPath = folderPath.Substring(path.Length);
                if (subPath[0] == Path.DirectorySeparatorChar)
                {
                    subPath = subPath.Substring(1);
                }
                return !subPath.Contains(Path.DirectorySeparatorChar);
            });
        }

        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, System.IO.SearchOption searchOption)
        {
            return EnumerateDirectories(path);
        }

        // SearchOption is ignored and always considered fully recursive.
        // Now supports search patterns
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, System.IO.SearchOption searchOption)
        {
            var files = _files.Keys.Where(filePath => filePath.StartsWith(path));


            // Need to handle at least simple wildcards. *.* and *.ext
            if (string.IsNullOrEmpty(searchPattern))
            {
                return files;
            }
            else
            {
                Regex regex = new Regex(WildcardToRegex(searchPattern), RegexOptions.IgnoreCase);
                string ext = searchPattern.Substring(1);
                return files.Where(filePath => regex.IsMatch(Path.GetFileName(filePath)));
            }
        }

        // Convert the wildcard to a regex
        public static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", ".") + "$";
        }

        public bool FileExists(string path)
        {
            return _files.ContainsKey(path);
        }

        public bool DirectoryExists(string path)
        {
            bool val = _folders.Contains(path);
            return val;
        }

        public void CreateDirectory(string path)
        {
            if (!DirectoryExists(path))
            {
                _folders.Add(path);
            }
        }

        public void RemoveFile(string path)
        {
            if (!FileExists(path))
            {
                throw new System.IO.FileNotFoundException();
            }
            _files.Remove(path);
        }

        public void CopyFile(string source, string destination, bool overwrite)
        {
            if (!FileExists(destination))
            {
                _files.Add(destination, _files[source]);
            }
        }

        public string ReadAllText(string path)
        {
            if (!FileExists(path))
            {
                throw new System.IO.FileNotFoundException();
            }
            return _files[path].FileContents;
        }

        public void WriteAllText(string path, string content)
        {
            WriteAllText(path, content, Encoding.Default);
        }

        public void WriteAllText(string path, string content, Encoding encoding)
        {
            if (_files.TryGetValue(path, out FileData curData))
            {
                // This makes sure each write to the file increases the timestamp
                curData.FileContents = content;
                curData.FileEncoding = encoding;
                SetLastWriteTime(curData);
            }
            else
            {
                _files[path] = new FileData() { FileContents = content, FileEncoding = encoding };
                SetLastWriteTime(_files[path]);
            }
        }

        public DateTime LastFileWriteTime(string path)
        {
            return _files[path].LastWriteTime;
        }

        public DateTime LastFileWriteTimeUtc(string path)
        {
            return _files[path].LastWriteTime;
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
                foreach (var item in _files.Where(file => file.Key.StartsWith(directoryPath)).ToList())
                {
                    _files.Remove(item.Key);
                }
            }
        }

        private string _tempFile;
        public void SetTempFile(string tempFile)
        {
            _tempFile = tempFile;
        }

        public string GetTempDirectoryOrFileName()
        {
            return _tempFile;
        }

        public void WriteAllBytes(string path, byte[] bytes)
        {

        }

        public long FileLength(string filename)
        {
            return ReadAllText(filename).Length;
        }

    }
}
