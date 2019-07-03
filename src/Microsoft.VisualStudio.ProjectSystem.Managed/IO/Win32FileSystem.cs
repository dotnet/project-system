// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
        public Stream Create(string path)
        {
            return File.Create(path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
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

        public DateTime LastFileWriteTime(string path)
        {
            return File.GetLastWriteTime(path);
        }

        public DateTime LastFileWriteTimeUtc(string path)
        {
            return File.GetLastWriteTimeUtc(path);
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
