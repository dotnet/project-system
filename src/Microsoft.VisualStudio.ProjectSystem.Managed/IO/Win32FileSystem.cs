// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.IO
{
    /// <summary>
    ///     Provides an implementation of <see cref="IFileSystem"/> that calls through the <see cref="Directory"/>
    ///     and <see cref="File"/> classes, and ultimately through Win32 APIs.
    /// </summary>
    [Export(typeof(IFileSystem))]
    internal class Win32FileSystem : IFileSystem
    {
        private static readonly DateTime s_minFileTime = DateTime.FromFileTimeUtc(0);

        public void Create(string path)
        {
            File.Create(path).Dispose();
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public bool PathExists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        public void RemoveFile(string path)
        {
            if (FileExists(path))
            {
                File.Delete(path);
            }
        }

        public void CopyFile(string source, string destination, bool overwrite)
        {
            File.Copy(source, destination, overwrite);
        }

        public async Task<string> ReadAllTextAsync(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        public Stream OpenTextStream(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        }

        public async Task WriteAllTextAsync(string path, string content)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: 4096, useAsync: true);
            using var writer = new StreamWriter(stream);
            await writer.WriteAsync(content);
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
            try
            {
                result = File.GetLastWriteTimeUtc(path);
                if (result != s_minFileTime)
                {
                    return true;
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (NotSupportedException)
            {
            }

            result = null;
            return false;
        }

        public bool TryGetFileSizeBytes(string path, out long result)
        {
            try
            {
                result = new FileInfo(path).Length;
                return true;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (NotSupportedException)
            {
            }

            result = default;
            return false;
        }

        public (long SizeBytes, DateTime WriteTimeUtc)? GetFileSizeAndWriteTimeUtc(string path)
        {
            var info = new FileInfo(path);

            if (info.Exists)
            {
                return (info.Length, info.LastWriteTimeUtc);
            }

            return null;
        }

        public bool DirectoryExists(string dirPath)
        {
            return Directory.Exists(dirPath);
        }

        public void CreateDirectory(string dirPath)
        {
            Directory.CreateDirectory(dirPath);
        }

        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }
    }
}
