// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Model for creating telemetry events when dependency tree is updated.
    /// It maintains some light state for each Target Framework to keep track
    /// whether all expected rules have been observed; this information is passed
    /// as a property of the telemetry event and can be used to determine if the
    /// 'resolved' event is fired too early (so sessions can be appropriately filtered).
    /// </summary>
    [Export(typeof(IDependencyTreeTelemetryService))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependencyTreeTelemetryService : IDependencyTreeTelemetryService
    {
        private const int MaxEventCount = 10;

        private readonly UnconfiguredProject _project;
        private readonly ITelemetryService? _telemetryService;
        private readonly ISafeProjectGuidService _safeProjectGuidService;
        private readonly ConcurrentDictionary<ITargetFramework, TelemetryState> _telemetryStates =
            new ConcurrentDictionary<ITargetFramework, TelemetryState>();
        private readonly object _stateUpdateLock = new object();
        private string? _projectId;
        private bool _stopTelemetry = false;
        private int _eventCount = 0;

        [ImportingConstructor]
        public DependencyTreeTelemetryService(
            UnconfiguredProject project,
            [Import(AllowDefault = true)] ITelemetryService? telemetryService,
            ISafeProjectGuidService safeProjectGuidService)
        {
            _project = project;
            _telemetryService = telemetryService;
            _safeProjectGuidService = safeProjectGuidService;

            if (telemetryService == null)
            {
                _stopTelemetry = true;
            }
        }

        /// <inheritdoc />
        public void InitializeTargetFrameworkRules(ImmutableArray<ITargetFramework> targetFrameworks, IReadOnlyCollection<string> rules)
        {
            if (_stopTelemetry)
                return;

            lock (_stateUpdateLock)
            {
                if (_stopTelemetry)
                    return;

                foreach (ITargetFramework targetFramework in targetFrameworks)
                {
                    TelemetryState telemetryState = _telemetryStates.GetOrAdd(targetFramework, _ => new TelemetryState());

                    foreach (string rule in rules)
                    {
                        telemetryState.InitializeRule(rule);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void ObserveTargetFrameworkRules(ITargetFramework targetFramework, IEnumerable<string> rules)
        {
            if (_stopTelemetry)
                return;

            lock (_stateUpdateLock)
            {
                if (_stopTelemetry)
                    return;

                if (_telemetryStates.TryGetValue(targetFramework, out TelemetryState telemetryState))
                {
                    foreach (string rule in rules)
                    {
                        telemetryState.ObserveRule(rule);
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task ObserveTreeUpdateCompletedAsync(bool hasUnresolvedDependency)
        {
            if (_stopTelemetry)
                return;

            bool observedAllRules;
            lock (_stateUpdateLock)
            {
                if (_stopTelemetry)
                    return;
                _stopTelemetry = !hasUnresolvedDependency || (++_eventCount >= MaxEventCount);
                observedAllRules = _telemetryStates.All(state => state.Value.ObservedAllRules());
            }

            _projectId ??= await GetProjectIdAsync();

            if (hasUnresolvedDependency)
            {
                _telemetryService!.PostProperties(TelemetryEventName.TreeUpdatedUnresolved, new[]
                {
                    (TelemetryPropertyName.TreeUpdatedUnresolvedProject, (object)_projectId),
                    (TelemetryPropertyName.TreeUpdatedUnresolvedObservedAllRules, observedAllRules)
                });
            }
            else
            {
                _telemetryService!.PostProperties(TelemetryEventName.TreeUpdatedResolved, new[]
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
                    return _telemetryService!.HashValue(_project.FullPath);
                }
            }
        }

        /// <summary>
        /// Maintain state for a single target framework.
        /// </summary>
        private class TelemetryState
        {
            private readonly ConcurrentDictionary<string, bool> _observedRules = new ConcurrentDictionary<string, bool>(StringComparers.RuleNames);

            public void InitializeRule(string rule) => _observedRules.TryAdd(rule, false);

            public void ObserveRule(string rule) => _observedRules.TryUpdate(rule, true, false);

            public bool ObservedAllRules() => !_observedRules.IsEmpty && _observedRules.All(entry => entry.Value);
        }
    }
}
