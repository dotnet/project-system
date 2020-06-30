// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
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
    internal sealed class DependencyTreeTelemetryService : IDependencyTreeTelemetryService
    {
        private const int MaxEventCount = 10;

        private readonly UnconfiguredProject _project;
        private readonly ITelemetryService? _telemetryService;
        private readonly ISafeProjectGuidService _safeProjectGuidService;
        private readonly object _stateUpdateLock = new object();

        /// <summary>
        /// Holds data used for telemetry. If telemetry is disabled, or if required
        /// information has been gathered, this field will be null.
        /// </summary>
        private Dictionary<TargetFramework, TelemetryState>? _stateByFramework;

        private string? _projectId;
        private int _eventCount = 0;

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

        public bool IsActive => _stateByFramework != null;

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

        public async Task ObserveTreeUpdateCompletedAsync(bool hasUnresolvedDependency)
        {
            if (_stateByFramework == null)
                return;

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
