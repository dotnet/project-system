// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Telemetry
{
    /// <summary>
    ///     Provides telemetry property names used throughout this project.
    /// </summary>
    internal static class TelemetryPropertyName
    {
        /// <summary>
        ///     Indicates the prefix (vs.projectsystem.managed) of all property names throughout this project.
        /// </summary>
        public const string Prefix = "vs.projectsystem.managed";

        /// <summary>
        ///     Indicates the reason that a project's last build is considered out-of-date.
        /// </summary>
        public static readonly string UpToDateCheckFailReason = BuildPropertyName(TelemetryEventName.UpToDateCheckFail, "Reason");

        /// <summary>
        ///     Indicates the project when the dependency tree is updated with all resolved dependencies.
        /// </summary>
        public static readonly string TreeUpdatedResolvedProject = BuildPropertyName(TelemetryEventName.TreeUpdatedResolved, "Project");

        /// <summary>
        ///     Indicates the project when when the dependency tree is updated with unresolved dependencies.
        /// </summary>
        public static readonly string TreeUpdatedUnresolvedProject = BuildPropertyName(TelemetryEventName.TreeUpdatedUnresolved, "Project");

        /// <summary>
        ///     Indicates whether seen all rules initialized when the dependency tree is updated with all resolved dependencies.
        /// </summary>
        public static readonly string TreeUpdatedResolvedObservedAllRules = BuildPropertyName(TelemetryEventName.TreeUpdatedResolved, "ObservedAllRules");

        /// <summary>
        ///      Indicates whether seen all rules initialized when the dependency tree is updated with unresolved dependencies.
        /// </summary>
        public static readonly string TreeUpdatedUnresolvedObservedAllRules = BuildPropertyName(TelemetryEventName.TreeUpdatedUnresolved, "ObservedAllRules");

        /// <summary>
        ///     Indicates whether a design-time build has completed without errors.
        /// </summary>
        public static readonly string DesignTimeBuildCompleteSucceeded = BuildPropertyName(TelemetryEventName.DesignTimeBuildComplete, "Succeeded");

        /// <summary>
        ///     Indicates the targets and their times during a design-time build.
        /// </summary>
        public static readonly string DesignTimeBuildCompleteTargets = BuildPropertyName(TelemetryEventName.DesignTimeBuildComplete, "Targets");

        /// <summary>
        ///     Indicates the project that contains the SDK version.
        /// </summary>
        public static readonly string SDKVersionProject = BuildPropertyName(TelemetryEventName.SDKVersion, "Project");

        /// <summary>
        ///     Indicates the actual underlying version of .NET Core SDK.
        /// </summary>
        public static readonly string SDKVersionNETCoreSdkVersion = BuildPropertyName(TelemetryEventName.SDKVersion, "NETCoreSdkVersion");

        /// <summary>
        ///     Indicates the number of TempPE DLLs compiled
        /// </summary>
        public static readonly string TempPECompileCount = BuildPropertyName(TelemetryEventName.TempPEProcessQueue, "CompileCount");

        /// <summary>
        ///     Indicates the starting length of the TempPE compilation queue
        /// </summary>
        public static readonly string TempPEInitialQueueLength = BuildPropertyName(TelemetryEventName.TempPEProcessQueue, "QueueLength");

        /// <summary>
        ///     Indicates whether the TempPE compilation was cancelled
        /// </summary>
        public static readonly string TempPECompileWasCancelled = BuildPropertyName(TelemetryEventName.TempPEProcessQueue, "Cancelled");

        /// <summary>
        ///     Indicates the duration of the TempPE compilation
        /// </summary>
        public static readonly string TempPECompileDuration = BuildPropertyName(TelemetryEventName.TempPEProcessQueue, "Duration");

        private static string BuildPropertyName(string eventName, string propertyName)
        {
            // Property names use the event names, but with slashes replaced by periods.
            // For example, vs/myevent would translate to vs.myevent.myproperty.
            string prefix = eventName.Replace('/', '.');

            Assumes.False(prefix.EndsWith("."));

            return prefix + "." + propertyName.ToLowerInvariant();
        }
    }
}
