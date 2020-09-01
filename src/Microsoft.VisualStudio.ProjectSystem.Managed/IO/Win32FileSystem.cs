// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

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

        public Stream Create(string path)
        {
            return File.Create(path);
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

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public void WriteAllText(string path, string content)
        {
            File.WriteAllText(path, content);
        }

        public void WriteAllText(string path, string content, Encoding encoding)
        {
            File.WriteAllText(path, content, encoding);
        }

        public void WriteAllBytes(string path, byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
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

            result = null;
            return false;
        }

        public long FileLength(string path)
        {
            return new FileInfo(path).Length;
        }

        public bool DirectoryExists(string dirPath)
        {
            return Directory.Exists(dirPath);
        }

        public void CreateDirectory(string dirPath)
        {
            Directory.CreateDirectory(dirPath);
        }

        public void RemoveDirectory(string path, bool recursive)
        {
            Directory.Delete(path, recursive);
        }

        public void SetDirectoryAttribute(string path, FileAttributes newAttribute)
        {
            var di = new DirectoryInfo(path);
            if ((di.Attributes & newAttribute) != newAttribute)
            {
                di.Attributes |= newAttribute;
            }
        }

        public string GetCurrentDirectory()
        {
            return Directory.GetCurrentDirectory();
        }

        public void SetCurrentDirectory(string directory)
        {
            Directory.SetCurrentDirectory(directory);
        }

        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }

        public IEnumerable<string> EnumerateDirectories(string path)
        {
            return Directory.EnumerateDirectories(path);
        }

        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.EnumerateDirectories(path, searchPattern, searchOption);
        }

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.EnumerateFiles(path, searchPattern, searchOption);
        }

        public string GetTempDirectoryOrFileName()
        {
            string fileNameWithoutPath = Path.GetRandomFileName();

            return Path.Combine(Path.GetTempPath(), fileNameWithoutPath);
        }
    }
}
