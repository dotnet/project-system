// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Telemetry
{
    /// <summary>
    ///     Provides telemetry event names used throughout this project.
    /// </summary>
    internal static class TelemetryEventName
    {
        // NOTE we don't extract the prefix (vs/projectsystem/managed) into a variable here, to make
        // it easier to search for the full event name across repositories and find this code as
        // a match.

        /// <summary>
        ///     Indicates the a fault in the managed project system.
        /// </summary>
        public const string Fault = "vs/projectsystem/managed/fault";

        /// <summary>
        ///     Indicates that a project's last build is considered up-to-date.
        /// </summary>
        public const string UpToDateCheckSuccess = "vs/projectsystem/managed/uptodatecheck/success";

        /// <summary>
        ///     Indicates that a project's last build is considered out-of-date.
        /// </summary>
        public const string UpToDateCheckFail = "vs/projectsystem/managed/uptodatecheck/fail";

        /// <summary>
        ///     Indicates that the dependency tree was updated with unresolved dependencies.
        /// </summary>
        public const string TreeUpdatedUnresolved = "vs/projectsystem/managed/treeupdated/unresolved";

        /// <summary>
        ///     Indicates that the dependency tree was updated with all resolved dependencies.
        /// </summary>
        public const string TreeUpdatedResolved = "vs/projectsystem/managed/treeupdated/resolved";

        /// <summary>
        ///     Indicates that a design-time build has completed.
        /// </summary>
        public const string DesignTimeBuildComplete = "vs/projectsystem/managed/designtimebuildcomplete";

        /// <summary>
        ///     Indicates the .NET Core SDK version.
        /// </summary>
        public const string SDKVersion = "vs/projectsystem/managed/sdkversion";

        /// <summary>
        ///     Indicates that the TempPE compilation queue has been processed.
        /// </summary>
        public const string TempPEProcessQueue = "vs/projectsystem/managed/temppe/processcompilequeue";

        /// <summary>
        ///     Indicates that the TempPE compilation has occurred on demand from a designer.
        /// </summary>
        public const string TempPECompileOnDemand = "vs/projectsystem/managed/temppe/compileondemand";

        /// <summary>
        ///     Indicates that the summary of a project's dependencies is being reported during project unload.
        /// </summary>
        public const string ProjectUnloadDependencies = "vs/projectsystem/managed/projectunload/dependencies";

        /// <summary>
        ///    Indicates that project was not up-to-date after build, meaning that incremental build is not
        ///    working correctly for the project.
        /// </summary>
        /// <remarks>
        ///    In some cases, we run the up-to-date check <i>after</i> a build completes, to determine whether
        ///    the project's incremental build is working correctly. When a build completes, it should be up-to-date.
        /// </remarks>
        public const string IncrementalBuildValidationFailure = "vs/projectsystem/managed/incrementalbuild/validationfailure";

        /// <summary>
        ///     Indicates that the user was notified of the suspected incremental build failure.
        /// </summary>
        public const string IncrementalBuildValidationFailureDisplayed = "vs/projectsystem/managed/incrementalbuild/validationfailure/displayed";

        /// <summary>
        ///     Indicates that the NuGet restore detected a cycle.
        /// </summary>
        public const string NuGetRestoreCycleDetected = "vs/projectsystem/managed/nugetrestore/cycledetected";
    }
}
