// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text;

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

        public Dictionary<string, FileData> Files { get; } = new Dictionary<string, FileData>(StringComparer.OrdinalIgnoreCase);

        public void Create(string path)
        {
            _ = WriteAllTextAsync(path, "");
        }

        public void AddFile(string path, DateTime? lastWriteTime = null)
        {
            Files[path] = new FileData
            {
                FileContents = "",
                LastWriteTimeUtc = lastWriteTime ?? DateTime.UtcNow
            };
        }

        public void AddFolder(string path)
        {
            _folders.Add(path);
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

        public string GetFullPath(string path)
        {
            if (_currentDirectory is not null)
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

        public Task<string> ReadAllTextAsync(string path)
        {
            return Task.FromResult(GetFileData(path).FileContents!);
        }

        public Stream OpenTextStream(string path)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(GetFileData(path).FileContents!));
        }

        public Task WriteAllTextAsync(string path, string content)
        {
            if (Files.TryGetValue(path, out FileData data))
            {
                // This makes sure each write to the file increases the timestamp
                data.FileContents = content;
                data.SetLastWriteTime();
            }
            else
            {
                Files[path] = new FileData
                {
                    FileContents = content,
                    LastWriteTimeUtc = DateTime.UtcNow
                };
            }

            return Task.CompletedTask;
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

        public bool TryGetFileSizeBytes(string path, out long result)
        {
            if (Files.TryGetValue(path, out FileData value))
            {
                result = value.FileContents?.Length ?? 0;
                return true;
            }

            result = default;
            return false;
        }

        public bool PathExists(string path)
        {
            throw new NotImplementedException();
        }

        public (long SizeBytes, DateTime WriteTimeUtc)? GetFileSizeAndWriteTimeUtc(string path)
        {
            if (Files.TryGetValue(path, out FileData value))
            {
                return (value.FileContents?.Length ?? 0, value.LastWriteTimeUtc);
            }

            return null;
        }

        private FileData GetFileData(string path)
        {
            if (!Files.TryGetValue(path, out FileData fileData))
            {
                throw new FileNotFoundException();
            }

            return fileData;
        }

        private FileData GetOrAddFileData(string path)
        {
            if (!Files.TryGetValue(path, out FileData fileData))
            {
                fileData = new();
                Files.Add(path, fileData);
            }

            return fileData;
        }
    }
}
