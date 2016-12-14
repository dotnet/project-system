// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.IO;

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
        };

        Dictionary<string, FileData> _files = new Dictionary<string, FileData>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> _folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, FileData> Files { get => _files; }

        public FileStream Create(string filePath)
        {
            WriteAllText(filePath, "");

            // No way to mock FileStream. Only caller does not check the return value.
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


        public void SetDirectoryAttribute(string directoryPath, FileAttributes newAttribute)
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

        public bool DirectoryExists(string dirPath)
        {
            bool val = _folders.Contains(dirPath);
            return val;
        }

        public void CreateDirectory(string dirPath)
        {
            if (!DirectoryExists(dirPath))
            {
                _folders.Add(dirPath);
            }
        }

        public void RemoveFile(string referenceFile)
        {
            if (!FileExists(referenceFile))
            {
                throw new System.IO.FileNotFoundException();
            }
            _files.Remove(referenceFile);
        }

        public void CopyFile(string source, string destination, bool overwrite)
        {
            if (!FileExists(destination))
            {
                _files.Add(destination, _files[source]);
            }
        }

        public string ReadAllText(string filePath)
        {
            if (!FileExists(filePath))
            {
                throw new System.IO.FileNotFoundException();
            }
            return _files[filePath].FileContents;
        }

        public void WriteAllText(string filePath, string content)
        {
            FileData curData;
            if (_files.TryGetValue(filePath, out curData))
            {
                // This makes sure each write to the file increases the timestamp
                curData.FileContents = content;
                SetLastWriteTime(curData);
            }
            else
            {
                _files[filePath] = new FileData() { FileContents = content };
                SetLastWriteTime(_files[filePath]);
            }
        }

        public void WriteAllText(string filePath, string content, Encoding encoding)
        {
            WriteAllText(filePath, content);
        }

        public DateTime LastFileWriteTime(string filepath)
        {
            return _files[filepath].LastWriteTime;
        }

        public DateTime LastFileWriteTimeUtc(string filepath)
        {
            return _files[filepath].LastWriteTime;
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

        public void WriteAllBytes(string filePath, byte[] bytes)
        {

        }

        public long FileLength(string filename)
        {
            return ReadAllText(filename).Length;
        }

    }
}
