// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;

namespace Microsoft.VisualStudio.IO
{
    /// <summary>
    /// A wrapper for <see cref="File"/> that gives us the ability to mock
    /// the type for testing.
    /// </summary>
    [Export(typeof(IFileSystem))]
    internal class FileSystem : IFileSystem
    {
        public FileStream Create(string filePath)
        {
            return File.Create(filePath);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public void RemoveFile(string referenceFile)
        {
            if (FileExists(referenceFile))
            {
                File.Delete(referenceFile);
            }
        }

        public void CopyFile(string source, string destination, bool overwrite)
        {
            File.Copy(source, destination, overwrite);
        }

        public string ReadAllText(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public void WriteAllText(string filePath, string content)
        {
            File.WriteAllText(filePath, content);
        }

        public void WriteAllText(string filePath, string content, Encoding encoding)
        {
            File.WriteAllText(filePath, content, encoding);
        }

        public void WriteAllBytes(string filePath, byte[] bytes)
        {
            File.WriteAllBytes(filePath, bytes);
        }

        public DateTime LastFileWriteTime(string filepath)
        {
            return File.GetLastWriteTime(filepath);
        }

        public DateTime LastFileWriteTimeUtc(string filepath)
        {
            return File.GetLastWriteTimeUtc(filepath);
        }

        public long FileLength(string filename)
        {
            return new FileInfo(filename).Length;
        }

        public bool DirectoryExists(string dirPath)
        {
            return Directory.Exists(dirPath);
        }

        public void CreateDirectory(string dirPath)
        {
            Directory.CreateDirectory(dirPath);
        }

        public void RemoveDirectory(string directoryPath, bool recursive)
        {
            Directory.Delete(directoryPath, recursive);
        }

        public void SetDirectoryAttribute(string directoryPath, FileAttributes newAttribute)
        {
            var di = new DirectoryInfo(directoryPath);
            if ((di.Attributes & newAttribute) != newAttribute)
            {
                di.Attributes |= newAttribute;
            }
        }

        public IEnumerable<string> EnumerateDirectories(string path)
        {
            return Directory.EnumerateDirectories(path);
        }

        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.GetDirectories(path, searchPattern, searchOption);
        }

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, System.IO.SearchOption searchOption)
        {
            return Directory.EnumerateFiles(path, searchPattern, searchOption);
        }

        public string GetTempDirectoryOrFileName()
        {
            var fileNameWithoutPath = Path.GetRandomFileName();

            return Path.Combine(Path.GetTempPath(), fileNameWithoutPath);
        }
    }
}
