// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using NuGet.SolutionRestoreManager;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Represents restore data for an <see cref="UnconfiguredProject"/>.
    /// </summary>
    internal class UnconfiguredProjectRestoreUpdate
    {
        public UnconfiguredProjectRestoreUpdate(IVsProjectRestoreInfo2 restoreInfo)
        {
            Requires.NotNull(restoreInfo, nameof(restoreInfo));

            RestoreInfo = restoreInfo;
        }
        
        /// <summary>
        ///     Gets the restore information produced in this update.
        /// </summary>
        public IVsProjectRestoreInfo2 RestoreInfo
        {
            get;
        }
    }
}
