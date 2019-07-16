// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Represents restore input data for a single <see cref="ConfiguredProject"/>.
    /// </summary>
    internal class PackageRestoreConfiguredInput
    {
        public PackageRestoreConfiguredInput(ProjectConfiguration projectConfiguration, ProjectRestoreInfo restoreInfo)
        {
            ProjectConfiguration = projectConfiguration;
            RestoreInfo = restoreInfo;
        }

        /// <summary>
        ///     Gets the configuration of the <see cref="ConfiguredProject"/> this input was produced from.
        /// </summary>
        public ProjectConfiguration ProjectConfiguration
        {
            get;
        }

        /// <summary>
        ///     Gets the restore information produced in this input.
        /// </summary>
        public ProjectRestoreInfo RestoreInfo
        {
            get;
        }
    }
}
