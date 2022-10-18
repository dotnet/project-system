// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore
{
    /// <summary>
    ///     Represents restore data for a package restore operation.
    /// </summary>
    internal class RestoreData
    {
        public RestoreData(string projectAssetsFilePath, DateTime projectAssetsLastWriteTimeUtc, bool succeeded = true)
        {
            ProjectAssetsFilePath = projectAssetsFilePath;
            ProjectAssetsLastWriteTimeUtc = projectAssetsLastWriteTimeUtc;
            Succeeded = succeeded;
        }

        /// <summary>
        ///     Gets the last write time of the assets file at the end of the last restore
        ///     or <see cref="DateTime.MinValue"/> if the file did not exist.
        /// </summary>
        public DateTime ProjectAssetsLastWriteTimeUtc { get; }

        /// <summary>
        ///     Gets the file path of the assets file.
        /// </summary>
        public string ProjectAssetsFilePath { get; }

        /// <summary>
        ///     Gets an indication if the restore was successful.
        /// </summary>
        public bool Succeeded { get; }
    }
}
