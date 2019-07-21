// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
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
        public DateTime ProjectAssetsLastWriteTimeUtc
        {
            get;
        }

        /// <summary>
        ///     Gets the file path of the assets file.
        /// </summary>
        public string ProjectAssetsFilePath
        {
            get;
        }

        /// <summary>
        ///     Gets an indication if the restore was successful.
        /// </summary>
        public bool Succeeded
        {
            get;
        }
    }
}
