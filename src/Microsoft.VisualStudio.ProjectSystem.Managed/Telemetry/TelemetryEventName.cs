// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    /// <summary>
    ///     Provides telemetry event names used throughout this project.
    /// </summary>
    internal static class TelemetryEventName
    {
        /// <summary>
        ///     Indicates the prefix (vs/projectsystem/managed/) of all event names throughout this project.
        /// </summary>
        public const string Prefix = "vs/projectsystem/managed";

        /// <summary>
        ///     Indicates that a project's last build is considered up-to-date.
        /// </summary>
        public static readonly string UpToDateCheckSuccess = BuildEventName("UpToDateCheck/Success");

        /// <summary>
        ///     Indicates that a project's last build is considered out-of-date.
        /// </summary>
        public static readonly string UpToDateCheckFail = BuildEventName("UpToDateCheck/Fail");

        /// <summary>
        ///     Indicates that the dependency tree was updated with unresolved dependencies.
        /// </summary>
        public static readonly string TreeUpdatedUnresolved = BuildEventName("TreeUpdated/Unresolved");

        /// <summary>
        ///     Indicates that the dependency tree was updated with all resolved dependencies.
        /// </summary>
        public static readonly string TreeUpdatedResolved = BuildEventName("TreeUpdated/Resolved");

        /// <summary>
        ///     Indicates that a design-time build has completed.
        /// </summary>
        public static readonly string DesignTimeBuildComplete = BuildEventName("DesignTimeBuildComplete");

        /// <summary>
        ///     Indicates that a failure occurred during creation or disposal of an <see cref="IWorkspaceProjectContext"/> instance.
        /// </summary>
        public static readonly string LanguageServiceInitFault = BuildEventName("LanguageServiceInit/Fault");

        /// <summary>
        ///     Indicates that .NET Core SDK version.
        /// </summary>
        public static readonly string SDKVersion = BuildEventName("SDKVersion");

        private static string BuildEventName(string eventName)
        {
            return Prefix + "/" + eventName.ToLowerInvariant();
        }
    }
}
