// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Represents restore input data for a single <see cref="ConfiguredProject"/>.
    /// </summary>
    internal class PackageRestoreConfiguredInput
    {
        public PackageRestoreConfiguredInput(ConfiguredProject project, IVsProjectRestoreInfo2 restoreInfo)
        {
            Project = project;
            RestoreInfo = restoreInfo;
        }

        /// <summary>
        ///     Gets the restore information produced in this input.
        /// </summary>
        public IVsProjectRestoreInfo2 RestoreInfo
        {
            get;
        }

        /// <summary>
        ///     Gets the <see cref="ConfiguredProject"/> this input was produced from.
        /// </summary>
        public ConfiguredProject Project
        {
            get;
        }
    }
}
