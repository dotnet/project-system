// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Represents restore data for an <see cref="UnconfiguredProject"/>.
    /// </summary>
    internal class UnconfiguredProjectRestoreUpdate
    {
        public UnconfiguredProjectRestoreUpdate(IVsProjectRestoreInfo2? restoreInfo, IReadOnlyCollection<ConfiguredProjectRestoreUpdate> configuredProjectRestoreUpdates)
        {
            RestoreInfo = restoreInfo;
            ConfiguredProjectRestoreUpdates = configuredProjectRestoreUpdates;
        }
        
        /// <summary>
        ///     Gets the restore information produced in this update. Can be <see langword="null"/> if
        ///     the project has no active configurations.
        /// </summary>
        public IVsProjectRestoreInfo2? RestoreInfo
        {
            get;
        }

        /// <summary>
        ///     Gets the <see cref="ConfiguredProjectRestoreUpdate"/> instances that contributed to <see cref="RestoreInfo"/>.
        /// </summary>
        public IReadOnlyCollection<ConfiguredProjectRestoreUpdate> ConfiguredProjectRestoreUpdates
        {
            get;
        }
    }
}
