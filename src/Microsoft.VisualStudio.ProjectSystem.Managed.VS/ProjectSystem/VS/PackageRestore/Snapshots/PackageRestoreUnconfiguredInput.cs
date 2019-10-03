// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Represents restore input data for an <see cref="UnconfiguredProject"/>.
    /// </summary>
    internal class PackageRestoreUnconfiguredInput
    {
        public PackageRestoreUnconfiguredInput(ProjectRestoreInfo? restoreInfo, IReadOnlyCollection<PackageRestoreConfiguredInput> configuredInputs)
        {
            RestoreInfo = restoreInfo;
            ConfiguredInputs = configuredInputs;
        }

        /// <summary>
        ///     Gets the restore information produced in this input. Can be <see langword="null"/> if
        ///     the project has no active configurations.
        /// </summary>
        public ProjectRestoreInfo? RestoreInfo
        {
            get;
        }

        /// <summary>
        ///     Gets the <see cref="PackageRestoreConfiguredInput"/> instances that contributed to <see cref="RestoreInfo"/>.
        /// </summary>
        public IReadOnlyCollection<PackageRestoreConfiguredInput> ConfiguredInputs
        {
            get;
        }
    }
}
