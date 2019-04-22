// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    /// <summary>
    ///     Represents restore data for a single <see cref="ConfiguredProject"/>.
    /// </summary>
    internal class ProjectRestoreUpdate
    {
        public ProjectRestoreUpdate(ProjectConfiguration projectConfiguration, IVsProjectRestoreInfo restoreInfo)
        {
            Requires.NotNull(projectConfiguration, nameof(projectConfiguration));
            Requires.NotNull(restoreInfo, nameof(restoreInfo));

            ProjectConfiguration = projectConfiguration;
            RestoreInfo = restoreInfo;
        }
        
        /// <summary>
        ///     Gets the restore information produced in this update.
        /// </summary>
        public IVsProjectRestoreInfo RestoreInfo
        {
            get;
        }

        /// <summary>
        ///     Gets the configuration of the <see cref="ConfiguredProject"/> 
        ///     this update was produced from.
        /// </summary>
        public ProjectConfiguration ProjectConfiguration
        {
            get;
        }
    }
}
