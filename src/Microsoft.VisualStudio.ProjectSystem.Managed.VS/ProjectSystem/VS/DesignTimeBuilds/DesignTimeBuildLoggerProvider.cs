// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.UI.InfoBars;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.DesignTimeBuilds;

/// <summary>
/// An MSBuild logger provider, that observes design-time builds for diagnostic purposes.
/// </summary>
/// <remarks>
/// This logger is only active during design-time builds. It captures the most time-consuming targets
/// and reports them via telemetry. It also captures data about failed DTBs.
/// </remarks>
[method: ImportingConstructor]
[Export(typeof(IBuildLoggerProviderAsync))]
[AppliesTo(ProjectCapability.DotNet)]
internal sealed class DesignTimeBuildLoggerProvider(ITelemetryService telemetryService, IInfoBarService infoBarService, IProjectFaultHandlerService projectFaultHandlerService, ConfiguredProject project) : IBuildLoggerProviderAsync
{
    public Task<IImmutableSet<ILogger>> GetLoggersAsync(IReadOnlyList<string> targets, IImmutableDictionary<string, string> properties, CancellationToken cancellationToken)
    {
        IImmutableSet<ILogger> loggers = ImmutableHashSet<ILogger>.Empty;

        if (properties.GetBoolProperty("DesignTimeBuild") is true)
        {
            loggers = loggers.Add(new DesignTimeTelemetryLogger(telemetryService, infoBarService, projectFaultHandlerService, project));
        }

        return Task.FromResult(loggers);
    }

    private sealed class DesignTimeTelemetryLogger(ITelemetryService telemetryService, IInfoBarService infoBarService, IProjectFaultHandlerService projectFaultHandlerService, ConfiguredProject project) : ILogger
    {
        /// <summary>
        /// Stores hashed target names so that we don't recompute these hashes for every project configuration.
        /// The set of target names is relatively small.
        /// </summary>
        private static ImmutableDictionary<(string Target, bool IsMicrosoft), string> s_hashByTargetName = ImmutableDictionary<(string Target, bool IsMicrosoft), string>.Empty;

        /// <summary>
        /// Stores whether a target is from a Microsoft SDK or VS install directory. If so, we don't have to hash the target name.
        /// </summary>
        private static ImmutableDictionary<string, bool> s_isMicrosoftTargetByFile = ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparers.Paths);

        /// <summary>
        /// Stores data about each target that's reported during build.
        /// </summary>
        /// <remarks>
        /// The initial capacity here is based on an empty .NET Console App's DTB, which has
        /// around 120 targets, and aims to avoid resizing the dictionary during the build.
        ///
        /// Targets can run concurrently, so use of this collection must be protected by a lock.
        /// </remarks>
        private readonly Dictionary<int, TargetRecord> _targetRecordById = new(capacity: 140);

        /// <summary>
        /// Data about any errors that occurred during the build. Note that design-time build
        /// targets are supposed to be authored to respect ContinueOnError, so errors might
        /// not fail the build. However it's still useful to know which targets reported errors.
        /// 
        /// Similarly, this collection may be empty even when the build fails.
        /// </summary>
        /// <remarks>
        /// Targets can run concurrently, so use of this collection must be protected by a lock.
        /// </remarks>
        private readonly List<ErrorData> _errors = [];

        /// <summary>
        /// Whether the build succeeded.
        /// </summary>
        private bool? _succeeded;

        LoggerVerbosity ILogger.Verbosity { get; set; }

        string? ILogger.Parameters { get; set; }

        void ILogger.Initialize(IEventSource eventSource)
        {
            eventSource.TargetStarted += OnTargetStarted;
            eventSource.TargetFinished += OnTargetFinished;
            eventSource.ErrorRaised += OnErrorRaised;
            eventSource.BuildFinished += OnBuildFinished;

            void OnTargetStarted(object sender, TargetStartedEventArgs e)
            {
                if (e.BuildEventContext is { TargetId: var id } && id > 0)
                {
                    bool isMicrosoft = ImmutableInterlocked.GetOrAdd(
                        ref s_isMicrosoftTargetByFile,
                        e.TargetFile,
                        file => ProjectFileClassifier.IsShippedByMicrosoft(e.TargetFile));

                    lock (_targetRecordById)
                    {
                        _targetRecordById[id] = new TargetRecord(e.TargetName, IsMicrosoft: isMicrosoft, e.Timestamp);
                    }
                }
            }

            void OnTargetFinished(object sender, TargetFinishedEventArgs e)
            {
                if (TryGetTargetRecord(e.BuildEventContext, out TargetRecord? record))
                {
                    record.Ended = e.Timestamp;
                }
            }

            void OnErrorRaised(object sender, BuildErrorEventArgs e)
            {
                string? targetName = null;

                if (TryGetTargetRecord(e.BuildEventContext, out TargetRecord? record))
                {
                    targetName = GetHashedTargetName(record);
                }

                lock (_errors)
                {
                    _errors.Add(new(targetName, e.Code));
                }
            }

            void OnBuildFinished(object sender, BuildFinishedEventArgs e)
            {
                _succeeded = e.Succeeded;
            }

            bool TryGetTargetRecord(BuildEventContext? context, [NotNullWhen(returnValue: true)] out TargetRecord? record)
            {
                lock (_targetRecordById)
                {
                    if (context is { TargetId: int id } && _targetRecordById.TryGetValue(id, out record))
                    {
                        return true;
                    }
                }

                record = null;
                return false;
            }
        }

        void ILogger.Shutdown()
        {
            SendTelemetry();

            ReportBuildErrors();

            void SendTelemetry()
            {
                object[][] targetDurations;
                ErrorData[] errors;

                lock (_targetRecordById)
                {
                    // Filter out very fast targets (to reduce the cost of ordering) then take the top ten by elapsed time.
                    // Note that targets can run multiple times, so the same target may appear more than once in the results.
                    targetDurations = _targetRecordById.Values
                        .Where(static record => record.Elapsed > new TimeSpan(ticks: 5 * TimeSpan.TicksPerMillisecond))
                        .OrderByDescending(record => record.Elapsed)
                        .Take(10)
                        .Select(record => new object[] { GetHashedTargetName(record), record.Elapsed.Milliseconds })
                        .ToArray();

                    errors = _errors.ToArray();
                }

                telemetryService.PostProperties(
                    TelemetryEventName.DesignTimeBuildComplete,
                    [
                        (TelemetryPropertyName.DesignTimeBuildComplete.Succeeded, _succeeded),
                        (TelemetryPropertyName.DesignTimeBuildComplete.Targets, new ComplexPropertyValue(targetDurations)),
                        (TelemetryPropertyName.DesignTimeBuildComplete.ErrorCount, errors.Length),
                        (TelemetryPropertyName.DesignTimeBuildComplete.Errors, new ComplexPropertyValue(errors)),
                    ]);
            }

            void ReportBuildErrors()
            {
                // Disabled for now, as we have too many false positives. Will revisit.

                // (These lines prevent compile warnings.)
                _ = projectFaultHandlerService;
                _ = infoBarService;
                _ = project;

                /*
                if (_succeeded is not false)
                {
                    // Only report a failure if the build failed. Specific targets can have
                    // errors, yet if ContinueOnError is set accordingly they won't fail the
                    // build. We don't want to report those to the user.
                    return;
                }

                projectFaultHandlerService.Forget(
                    infoBarService.ShowInfoBarAsync(
                        VSResources.DesignTimeBuildErrorDetectedInfoBarMessage,
                        KnownMonikers.StatusError,
                        CancellationToken.None),
                    project.UnconfiguredProject);
                */
            }
        }

        /// <summary>
        /// Gets the hashed name of the target, if required.
        /// </summary>
        /// <remarks>
        /// If the target is shipped by Microsoft (or the user is internal), the target's name is returned unchanged.
        /// </remarks>
        private string GetHashedTargetName(TargetRecord record)
        {
            return ImmutableInterlocked.GetOrAdd(
                ref s_hashByTargetName,
                (record.TargetName, record.IsMicrosoft),
                GetHashedTargetName,
                telemetryService);

            static string GetHashedTargetName((string TargetName, bool IsMicrosoft) record, ITelemetryService telemetryService)
            {
                return record.IsMicrosoft
                    ? record.TargetName
                    : telemetryService.HashValue(record.TargetName);
            }
        }

        /// <summary>
        /// Models data about a target's execution.
        /// </summary>
        [DebuggerDisplay("{TargetName,nq}: {Elapsed}")]
        private sealed record class TargetRecord(string TargetName, bool IsMicrosoft, DateTime Started)
        {
            public DateTime Ended { get; set; }

            public TimeSpan Elapsed => Ended - Started;
        }

        /// <summary>
        /// Data about errors reported during design-time builds.
        /// </summary>
        /// <param name="TargetName">Names of the targets that reported the error. May be hashed. <see langword="null"/> if the target name could not be identified.</param>
        /// <param name="ErrorCode">An error code that identifies the type of error.</param>
        private sealed record class ErrorData(string? TargetName, string ErrorCode);
    }
}
