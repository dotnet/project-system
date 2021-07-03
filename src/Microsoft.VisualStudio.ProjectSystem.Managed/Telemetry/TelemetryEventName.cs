// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
        public const string UpToDateCheckSuccess = Prefix + "/uptodatecheck/success";

        /// <summary>
        ///     Indicates that a project's last build is considered out-of-date.
        /// </summary>
        public const string UpToDateCheckFail = Prefix + "/uptodatecheck/fail";

        /// <summary>
        ///     Indicates that the dependency tree was updated with unresolved dependencies.
        /// </summary>
        public const string TreeUpdatedUnresolved = Prefix + "/treeupdated/unresolved";

        /// <summary>
        ///     Indicates that the dependency tree was updated with all resolved dependencies.
        /// </summary>
        public const string TreeUpdatedResolved = Prefix + "/treeupdated/resolved";

        /// <summary>
        ///     Indicates that a design-time build has completed.
        /// </summary>
        public const string DesignTimeBuildComplete = Prefix + "/designtimebuildcomplete";

        /// <summary>
        ///     Indicates the .NET Core SDK version.
        /// </summary>
        public const string SDKVersion = Prefix + "/sdkversion";

        /// <summary>
        ///     Indicates that the TempPE compilation queue has been processed.
        /// </summary>
        public const string TempPEProcessQueue = Prefix + "/temppe/processcompilequeue";

        /// <summary>
        ///     Indicates that the TempPE compilation has occurred on demand from a designer.
        /// </summary>
        public const string TempPECompileOnDemand = Prefix + "/temppe/compileondemand";

        /// <summary>
        ///     Indicates that the summary of a project's dependencies is being reported during project unload.
        /// </summary>
        public const string ProjectUnloadDependencies = Prefix + "/projectunload/dependencies";
    }
}
