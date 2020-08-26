// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal class RemoteCacheFile
    {
        internal RemoteCacheFile(string cacheFilePath, string downloadUrl, TimeSpan cacheExpiration, IFileSystem fileSystem,
                                 Lazy<IHttpClient> httpClient)
        {
            _downloadUrl = downloadUrl;
            _cacheFilePath = cacheFilePath;
            _fileSystem = fileSystem;
            _httpClient = httpClient;
            _cacheExpiration = cacheExpiration;
        }

        private readonly string _downloadUrl;
        private readonly string _cacheFilePath;
        private readonly IFileSystem _fileSystem;
        private readonly Lazy<IHttpClient> _httpClient;
        private readonly TimeSpan _cacheExpiration;

        public bool CacheFileIsStale()
        {
            return _fileSystem.GetLastFileWriteTimeOrMinValueUtc(_cacheFilePath).Add(_cacheExpiration) < DateTime.UtcNow;
        }

        /// <summary>
        /// If the cached file exists reads the data and returns it as a string
        /// </summary>
        public string? ReadCacheFile()
        {
            try
            {
                // If the cached file exists read it
                if (_fileSystem.FileExists(_cacheFilePath))
                {
                    return _fileSystem.ReadAllText(_cacheFilePath);
                }
            }
            catch (System.IO.IOException)
            {
            }
            return null;
        }

        /// <summary>
        /// Downloads from the downloadUri to the cached file.
        /// </summary>
        public async Task TryToUpdateCacheFileAsync(Action? callBackOnSuccess = null)
        {
            try
            {
                string downLoadedVersionData = await _httpClient.Value.GetStringAsync(new Uri(_downloadUrl));

                // Make sure it is valid data before we write to the file (will throw on failure, we don't need the returned data)
                VersionCompatibilityData.DeserializeVersionData(downLoadedVersionData);
                _fileSystem.WriteAllText(_cacheFilePath, downLoadedVersionData);
                callBackOnSuccess?.Invoke();
            }
            catch
            {
            }
        }
    }
}
