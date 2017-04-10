// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class TelemetryEvents
    {
        /// <summary>
        /// Root path for all project system events. New events should start with this path
        /// </summary>
        public const string ProjectSystemRootPath = "vs/projectsystem/managed/";

        /// <summary>
        /// Path for the result of live-reloading the project.
        /// </summary>
        public const string ReloadResultOperationPath = ProjectSystemRootPath + "projectreload/reload-result";

        /// <summary>
        /// Path for exceptions raised during live-reloading the project.
        /// </summary>
        public const string ReloadFailedNFWPath = ProjectSystemRootPath + "projectreload/reload-failure";
    }
}
