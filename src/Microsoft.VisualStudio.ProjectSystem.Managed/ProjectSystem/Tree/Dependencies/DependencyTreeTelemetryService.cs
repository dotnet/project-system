// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies
{
    /// <summary>
    /// Model for creating telemetry events when dependency tree is updated.
    /// It maintains some light state for each Target Framework to keep track
    /// whether all expected rules have been observed; this information is passed
    /// as a property of the telemetry event and can be used to determine if the
    /// 'resolved' event is fired too early (so sessions can be appropriately filtered).
    /// </summary>
    /// <remarks>
    /// Instantiated per unconfigured project.
    /// </remarks>
    [Export(typeof(IDependencyTreeTelemetryService))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class DependencyTreeTelemetryService : IDependencyTreeTelemetryService, IDisposable
    {
        private const int MaxEventCount = 10;

        private readonly UnconfiguredProject _project;
        private readonly ITelemetryService? _telemetryService;
        private readonly ISafeProjectGuidService _safeProjectGuidService;
        private readonly Stopwatch _projectLoadTime = Stopwatch.StartNew();
        private readonly object _stateUpdateLock = new object();

        /// <summary>
        /// Holds data used for telemetry. If telemetry is disabled, or if required
        /// information has been gathered, this field will be null.
        /// </summary>
        private Dictionary<TargetFramework, TelemetryState>? _stateByFramework;

        private string? _projectId;
        private int _eventCount;
        private DependenciesSnapshot? _dependenciesSnapshot;

        [ImportingConstructor]
        public DependencyTreeTelemetryService(
            UnconfiguredProject project,
            [Import(AllowDefault = true)] ITelemetryService? telemetryService,
            ISafeProjectGuidService safeProjectGuidService)
        {
            _project = project;
            _safeProjectGuidService = safeProjectGuidService;

            if (telemetryService != null)
            {
                _telemetryService = telemetryService;
                _stateByFramework = new Dictionary<TargetFramework, TelemetryState>();
            }
        }

        public void InitializeTargetFrameworkRules(ImmutableArray<TargetFramework> targetFrameworks, IReadOnlyCollection<string> rules)
        {
            if (_stateByFramework == null)
                return;

            lock (_stateUpdateLock)
            {
                if (_stateByFramework == null)
                    return;

                foreach (TargetFramework targetFramework in targetFrameworks)
                {
                    if (!_stateByFramework.TryGetValue(targetFramework, out TelemetryState telemetryState))
                    {
                        telemetryState = _stateByFramework[targetFramework] = new TelemetryState();
                    }

                    foreach (string rule in rules)
                    {
                        telemetryState.InitializeRule(rule);
                    }
                }
            }
        }

        public void ObserveTargetFrameworkRules(TargetFramework targetFramework, IEnumerable<string> rules)
        {
            if (_stateByFramework == null)
                return;

            lock (_stateUpdateLock)
            {
                if (_stateByFramework == null)
                    return;

                if (_stateByFramework.TryGetValue(targetFramework, out TelemetryState telemetryState))
                {
                    foreach (string rule in rules)
                    {
                        telemetryState.ObserveRule(rule);
                    }
                }
            }
        }

        public async ValueTask ObserveTreeUpdateCompletedAsync(bool hasUnresolvedDependency)
        {
            if (_stateByFramework == null)
                return;

            Assumes.NotNull(_telemetryService);

            bool observedAllRules;
            lock (_stateUpdateLock)
            {
                if (_stateByFramework == null)
                    return;

                observedAllRules = _stateByFramework.All(state => state.Value.ObservedAllRules());

                if (!hasUnresolvedDependency || (_eventCount++ == MaxEventCount))
                {
                    _stateByFramework = null;
                }
            }

            _projectId ??= await GetProjectIdAsync();

            if (hasUnresolvedDependency)
            {
                _telemetryService.PostProperties(TelemetryEventName.TreeUpdatedUnresolved, new[]
                {
                    (TelemetryPropertyName.TreeUpdatedUnresolvedProject, (object)_projectId),
                    (TelemetryPropertyName.TreeUpdatedUnresolvedObservedAllRules, observedAllRules)
                });
            }
            else
            {
                _telemetryService.PostProperties(TelemetryEventName.TreeUpdatedResolved, new[]
                {
                    (TelemetryPropertyName.TreeUpdatedResolvedProject, (object)_projectId),
                    (TelemetryPropertyName.TreeUpdatedResolvedObservedAllRules, observedAllRules)
                });
            }

            return;

            async Task<string> GetProjectIdAsync()
            {
                Guid projectGuid = await _safeProjectGuidService.GetProjectGuidAsync();

                if (!projectGuid.Equals(Guid.Empty))
                {
                    return projectGuid.ToString();
                }
                else
                {
                    return _telemetryService.HashValue(_project.FullPath);
                }
            }
        }

        public void ObserveSnapshot(DependenciesSnapshot dependenciesSnapshot)
        {
            _dependenciesSnapshot = dependenciesSnapshot;
        }

        public void Dispose()
        {
            if (_telemetryService != null && _dependenciesSnapshot != null && _projectId != null)
            {
                var data = new Dictionary<string, Dictionary<string, DependencyCount>>();
                int totalDependencyCount = 0;
                int unresolvedDependencyCount = 0;

                // Scan the snapshot and tally dependencies
                foreach ((TargetFramework targetFramework, TargetedDependenciesSnapshot targetedSnapshot) in _dependenciesSnapshot.DependenciesByTargetFramework)
                {
                    var countsByType = new Dictionary<string, DependencyCount>();

                    data[targetFramework.ShortName] = countsByType;

                    foreach (IDependency dependency in targetedSnapshot.Dependencies)
                    {
                        // Only include visible dependencies in telemetry counts
                        if (!dependency.Visible)
                        {
                            continue;
                        }

                        if (!countsByType.TryGetValue(dependency.ProviderType, out DependencyCount counts))
                        {
                            counts = new DependencyCount();
                        }

                        countsByType[dependency.ProviderType] = counts.Add(dependency.Resolved);

                        totalDependencyCount++;

                        if (!dependency.Resolved)
                        {
                            unresolvedDependencyCount++;
                        }
                    }
                }

                _telemetryService.PostProperties(TelemetryEventName.ProjectUnloadDependencies, new (string, object)[]
                {
                    (TelemetryPropertyName.ProjectUnloadDependenciesProject, _projectId),
                    (TelemetryPropertyName.ProjectUnloadProjectAgeMillis, _projectLoadTime.ElapsedMilliseconds),
                    (TelemetryPropertyName.ProjectUnloadTotalDependencyCount, totalDependencyCount),
                    (TelemetryPropertyName.ProjectUnloadUnresolvedDependencyCount, unresolvedDependencyCount),
                    (TelemetryPropertyName.ProjectUnloadTargetFrameworkCount, _dependenciesSnapshot.DependenciesByTargetFramework.Count),
                    (TelemetryPropertyName.ProjectUnloadDependencyBreakdown, new ComplexPropertyValue(data))
                });
            }
        }

        private readonly struct DependencyCount
        {
            public int TotalCount { get; } 
            public int UnresolvedCount { get; }

            public DependencyCount(int totalCount, int unresolvedCount)
            {
                TotalCount = totalCount;
                UnresolvedCount = unresolvedCount;
            }

            public DependencyCount Add(bool isResolved) => new DependencyCount(
                TotalCount + 1,
                isResolved ? UnresolvedCount : UnresolvedCount + 1);
        }

        /// <summary>
        /// Maintain state for a single target framework.
        /// </summary>
        private sealed class TelemetryState
        {
            private readonly Dictionary<string, bool> _observedRules = new Dictionary<string, bool>(StringComparers.RuleNames);

            public void InitializeRule(string rule)
            {
                if (!_observedRules.ContainsKey(rule))
                {
                    _observedRules[rule] = false;
                }
            }

            public void ObserveRule(string rule)
            {
                _observedRules[rule] = true;
            }

            public bool ObservedAllRules()
            {
                return _observedRules.Count != 0 && _observedRules.All(entry => entry.Value);
            }
        }
    }
}
