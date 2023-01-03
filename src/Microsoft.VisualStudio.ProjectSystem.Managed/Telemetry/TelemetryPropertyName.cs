// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Telemetry
{
    /// <summary>
    ///     Provides telemetry property names used throughout this project.
    /// </summary>
    internal static class TelemetryPropertyName
    {
        // NOTE we don't extract the prefix (vs.projectsystem.managed) into a variable here, to make
        // it easier to search for the full property name across repositories and find this code as
        // a match.

        public static class UpToDateCheck
        {
            /// <summary>
            ///     Indicates the reason that a project's last build is considered out-of-date.
            /// </summary>
            public const string FailReason = "vs.projectsystem.managed.uptodatecheck.fail.reason2";

            /// <summary>
            ///     Indicates the duration of the up-to-date check, in milliseconds. Includes wait time and execution time.
            /// </summary>
            public const string DurationMillis = "vs.projectsystem.managed.uptodatecheck.durationmillis";

            /// <summary>
            ///     Indicates the duration of time between when the check was requested, and when we actually
            ///     start execution.
            /// </summary>
            /// <remarks>
            ///     During this time we await the latest project data, which can take quite some time.
            ///     We also acquire a lock, and query the host for the status of the up-to-date check.
            ///     We report this wait time separately via telemetry in order to properly attribute the source
            ///     of delays in the up-to-date check. This time is also included in <see cref="DurationMillis"/>
            ///     for historical reasons. Ideally they would not overlap, but changing that now would
            ///     make analysis of historical data difficult.
            /// </remarks>
            public const string WaitDurationMillis = "vs.projectsystem.managed.uptodatecheck.waitdurationmillis";

            /// <summary>
            ///     Indicates the number of file system timestamps that were queried during the up-to-date check.
            /// </summary>
            public const string FileCount = "vs.projectsystem.managed.uptodatecheck.filecount";

            /// <summary>
            ///     Indicates the number of (implicitly active) configurations that were included in the the up-to-date check.
            /// </summary>
            /// <remarks>
            ///     The up-to-date check runs for the active configuration only, but will consider state from all
            ///     implicitly active configurations. Generally, for a single targeting project this will equal one,
            ///     and for multi-targeting projects this will equal the number of target frameworks being targeted.
            /// </remarks>
            public const string ConfigurationCount = "vs.projectsystem.managed.uptodatecheck.configurationcount";

            /// <summary>
            ///     Indicates the user's chosen logging level. Values from the <see cref="LogLevel"/> enum.
            /// </summary>
            public const string LogLevel = "vs.projectsystem.managed.uptodatecheck.loglevel";

            /// <summary>
            ///     Indicates any ignore kinds provided to the fast up-to-date check.
            ///     Used to skip analyzers during indirect builds (for debug or unit tests).
            /// </summary>
            public const string IgnoreKinds = "vs.projectsystem.managed.uptodatecheck.ignorekinds";

            /// <summary>
            ///     Identifies the project to which data in the telemetry event applies.
            /// </summary>
            public const string Project = "vs.projectsystem.managed.uptodatecheck.projectid";

            /// <summary>
            ///     Indicates the number of checks performed for this project so far in the current session, starting at one.
            ///     This number resets when the project is reloaded.
            /// </summary>
            public const string CheckNumber = "vs.projectsystem.managed.uptodatecheck.checknumber";

            /// <summary>
            ///     The outcome of the FUTDC's Build Acceleration evaluation.
            /// </summary>
            public const string AccelerationResult = "vs.projectsystem.managed.uptodatecheck.accelerationresult";

            /// <summary>
            ///     The number of files copied as part of Build Acceleration. Zero if disabled or no files were copied.
            ///     See <see cref="AccelerationResult"/> to understand why the value may be zero.
            /// </summary>
            public const string AcceleratedCopyCount = "vs.projectsystem.managed.uptodatecheck.acceleratedcopycount";
        }

        public static class TreeUpdated
        {
            /// <summary>
            ///     Indicates the project when the dependency tree is updated with all resolved dependencies.
            /// </summary>
            public const string ResolvedProject = "vs.projectsystem.managed.treeupdated.resolved.project";

            /// <summary>
            ///     Indicates the project when the dependency tree is updated with unresolved dependencies.
            /// </summary>
            public const string UnresolvedProject = "vs.projectsystem.managed.treeupdated.unresolved.project";

            /// <summary>
            ///     Indicates whether seen all rules initialized when the dependency tree is updated with all resolved dependencies.
            /// </summary>
            public const string ResolvedObservedAllRules = "vs.projectsystem.managed.treeupdated.resolved.observedallrules";

            /// <summary>
            ///      Indicates whether seen all rules initialized when the dependency tree is updated with unresolved dependencies.
            /// </summary>
            public const string UnresolvedObservedAllRules = "vs.projectsystem.managed.treeupdated.unresolved.observedallrules";
        }

        public static class ProjectUnload
        {
            /// <summary>
            ///     Identifies the project to which data in the telemetry event applies.
            /// </summary>
            public const string DependenciesProject = "vs.projectsystem.managed.projectunload.dependencies.project";

            /// <summary>
            ///     Identifies the version of project unload dependencies telemetry being sent.
            /// </summary>
            public const string DependenciesVersion = "vs.projectsystem.managed.projectunload.dependencies.version";

            /// <summary>
            ///     Identifies the time between project load and unload, in milliseconds.
            /// </summary>
            public const string ProjectAgeMillis = "vs.projectsystem.managed.projectunload.dependencies.projectagemillis";

            /// <summary>
            ///     Identifies the total number of visible dependencies in the project.
            ///     If a project multi-targets (i.e. <see cref="TargetFrameworkCount"/> is greater than one) then the count of dependencies
            ///     in each target is summed together to produce this single value. If a breakdown is required, <see cref="DependencyBreakdown"/>
            ///     may be used.
            /// </summary>
            public const string TotalDependencyCount = "vs.projectsystem.managed.projectunload.dependencies.totaldependencycount";

            /// <summary>
            ///     Identifies the total number of visible unresolved dependencies in the project.
            ///     If a project multi-targets (i.e. <see cref="TargetFrameworkCount"/> is greater than one) then the count of unresolved dependencies
            ///     in each target is summed together to produce this single value. If a breakdown is required, <see cref="DependencyBreakdown"/>
            ///     may be used.
            /// </summary>
            public const string UnresolvedDependencyCount = "vs.projectsystem.managed.projectunload.dependencies.unresolveddependencycount";

            /// <summary>
            ///     Identifies the number of frameworks this project targets.
            /// </summary>
            public const string TargetFrameworkCount = "vs.projectsystem.managed.projectunload.dependencies.targetframeworkcount";

            /// <summary>
            ///     Contains structured data describing the number of total/unresolved dependencies broken down by target framework and dependency type.
            /// </summary>
            public const string DependencyBreakdown = "vs.projectsystem.managed.projectunload.dependencies.dependencybreakdown";
        }

        public static class DesignTimeBuildComplete
        {
            /// <summary>
            ///     Indicates whether a design-time build has completed without errors.
            /// </summary>
            public const string Succeeded = "vs.projectsystem.managed.designtimebuildcomplete.succeeded";

            /// <summary>
            ///     Indicates the targets and their times during a design-time build.
            /// </summary>
            public const string Targets = "vs.projectsystem.managed.designtimebuildcomplete.targets";
        }

        public static class SDKVersion
        {
            /// <summary>
            ///     Indicates the project that contains the SDK version.
            /// </summary>
            public const string Project = "vs.projectsystem.managed.sdkversion.project";

            /// <summary>
            ///     Indicates the actual underlying version of .NET Core SDK.
            /// </summary>
            public const string NETCoreSDKVersion = "vs.projectsystem.managed.sdkversion.netcoresdkversion";
        }

        public static class TempPE
        {
            /// <summary>
            ///     Indicates the number of TempPE DLLs compiled
            /// </summary>
            public const string CompileCount = "vs.projectsystem.managed.temppe.processcompilequeue.compilecount";

            /// <summary>
            ///     Indicates the starting length of the TempPE compilation queue
            /// </summary>
            public const string InitialQueueLength = "vs.projectsystem.managed.temppe.processcompilequeue.queuelength";

            /// <summary>
            ///     Indicates whether the TempPE compilation was cancelled
            /// </summary>
            public const string CompileWasCancelled = "vs.projectsystem.managed.temppe.processcompilequeue.cancelled";

            /// <summary>
            ///     Indicates the duration of the TempPE compilation
            /// </summary>
            public const string CompileDuration = "vs.projectsystem.managed.temppe.processcompilequeue.duration";
        }

        public static class IncrementalBuildValidation
        {
            /// <summary>
            ///     Indicates the reason the project was not up-to-date immediately after build.
            /// </summary>
            public const string FailureReason = "vs.projectsystem.managed.incrementalbuild.validationfailure.reason";

            /// <summary>
            ///     Indicates the duration of the up-to-date check performed immediately after build to find incremental build breaks.
            /// </summary>
            public const string DurationMillis = "vs.projectsystem.managed.incrementalbuild.validationfailure.durationmillis";
        }

        public static class NuGetRestoreCycleDetected
        {
            /// <summary>
            ///     Indicates the duration of the NuGet restore to detect a cycle
            /// </summary>
            public const string RestoreDurationMillis = "vs.projectsystem.managed.nugetrestore.cycledetected.durationmillis";

            /// <summary>
            ///     Indicates the number of time NuGet restore have succeeded until now.
            /// </summary>
            public const string RestoreSuccesses = "vs.projectsystem.managed.nugetrestore.cycledetected.restoresuccesses";

            /// <summary>
            ///     Indicates the number of times NuGet restore have detected cycles until now.
            /// </summary>
            public const string RestoreCyclesDetected = "vs.projectsystem.managed.nugetrestore.cycledetected.cyclesdetected";
        }
    }
}
