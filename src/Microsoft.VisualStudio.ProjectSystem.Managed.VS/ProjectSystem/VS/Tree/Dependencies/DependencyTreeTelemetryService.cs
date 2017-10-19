// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// Model for creating telemetry events when dependency tree is updated.
    /// It maintains some light state for each Target Framework to keep track
    /// of the status of Rule processing so we can heuristically determine
    /// when the dependency tree is "ready"
    /// </summary>
    [Export(typeof(IDependencyTreeTelemetryService))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependencyTreeTelemetryService : IDependencyTreeTelemetryService
    {
        private const string TelemetryEventName = "TreeUpdated";
        private const string UnresolvedLabel = "Unresolved";
        private const string ResolvedLabel = "Resolved";
        private const string ProjectProperty = "Project";

        /// <summary>
        /// Maintain state for each target framework
        /// </summary>
        internal class TelemetryState
        {
            public ConcurrentDictionary<string, bool> ObservedRuleChanges { get; } = new ConcurrentDictionary<string, bool>(StringComparers.RuleNames);
            public bool ObservedDesignTime { get; set; }
            public bool StopTelemetry { get; set; }

            public bool IsReadyToResolve() => ObservedDesignTime
                && ObservedRuleChanges.Any()
                && ObservedRuleChanges.All(entry => entry.Value);            
        }

        private readonly UnconfiguredProject _project;
        private readonly ITelemetryService _telemetryService;
        private readonly ConcurrentDictionary<ITargetFramework, TelemetryState> _telemetryStates = 
            new ConcurrentDictionary<ITargetFramework, TelemetryState>();
        private readonly object _stateUpdateLock = new object();
        private string _projectId;
        private bool _stopTelemetry = false;
        private bool _isReadyToResolve = false;

        [ImportingConstructor]
        public DependencyTreeTelemetryService(
            UnconfiguredProject project,
            ITelemetryService telemetryService)
        {
            _project = project;
            _telemetryService = telemetryService;
        }

        /// <summary>
        /// Fire a telemetry event when the dependency tree update is complete, 
        /// conditioned on three heuristics:
        ///  1. Telemetry is fired with 'Unresolved' label if any target frameworks are
        ///     yet to process Rules from a Design Time build. This is tracked by 
        ///     TelemetryState.ObservedDesignTime
        ///  2. Telemetry is fired with 'Unresolved' label if any relevant Rules from design
        ///     time builds are yet to report any changes. Relevant Rules are determined
        ///     based on the 'ICrossTargetRuleHandler's that have processed. This is tracked
        ///     by TelemetryState.ObservedRuleChanges
        ///  3. Telemetry fires with 'Resolved' label when 1. and 2. are no longer true
        ///  4. Telemetry stops firing once all target frameworks observe an Evaluation following
        ///     the first design time build, or once it has fired once with 'Resolved' label. 
        ///     This prevents telemetry events caused by project changes and tree updates after initial load has completed.
        /// </summary>
        public void ObserveTreeUpdateCompleted()
        {
            if (_stopTelemetry) return;

            if (_projectId == null)
            {
                InitializeProjectId();
            }

            lock (_stateUpdateLock)
            {
                _stopTelemetry = _isReadyToResolve || IsStopTelemetryInAllTargetFrameworks();
            }

            _telemetryService.PostProperty($"{TelemetryEventName}/{(_isReadyToResolve ? ResolvedLabel : UnresolvedLabel)}",
                ProjectProperty, _projectId);
        }

        public void ObserveUnresolvedRules(ITargetFramework targetFramework, IEnumerable<string> rules)
        {
            if (_stopTelemetry) return;

            lock (_stateUpdateLock)
            {
                var telemetryState = _telemetryStates.GetOrAdd(targetFramework, (key) => new TelemetryState());

                foreach (var rule in rules)
                {
                    // observe all unresolved rules by default ignoring whether they have actual changes
                    UpdateObservedRuleChanges(telemetryState, rule);
                }
            }
        }

        public void ObserveHandlerRulesChanges(
            ITargetFramework targetFramework,
            IEnumerable<string> handlerRules, 
            IImmutableDictionary<string, IProjectChangeDescription> projectChanges)
        {
            if (_stopTelemetry) return;

            lock (_stateUpdateLock)
            {
                if (_telemetryStates.TryGetValue(targetFramework, out var telemetryState))
                {
                    foreach (var rule in handlerRules)
                    {
                        var hasChanges = projectChanges.TryGetValue(rule, out var description)
                            && description.Difference.AnyChanges;
                        UpdateObservedRuleChanges(telemetryState, rule, hasChanges);
                    }
                }
            }
        }

        public void ObserveCompleteHandlers(ITargetFramework targetFramework, RuleHandlerType handlerType)
        {
            if (_stopTelemetry) return;

            lock (_stateUpdateLock)
            {
                if (_telemetryStates.TryGetValue(targetFramework, out var telemetryState))
                {
                    telemetryState.ObservedDesignTime |= handlerType == RuleHandlerType.DesignTimeBuild;
                    telemetryState.StopTelemetry |= telemetryState.ObservedDesignTime && handlerType == RuleHandlerType.Evaluation;
                }

                _isReadyToResolve = !_telemetryStates.IsEmpty && _telemetryStates.Values.All(s => s.IsReadyToResolve());

                // if isReadyToResolve, wait until atleast one Resolved telemetry event has fired before stopping telemetry
                _stopTelemetry = !_isReadyToResolve && IsStopTelemetryInAllTargetFrameworks();
            }
        }

        private void InitializeProjectId()
        {
            var projectGuidService = _project.Services.ExportProvider.GetExportedValueOrDefault<IProjectGuidService>();
            if (projectGuidService != null)
            {
                SetProjectId(projectGuidService.ProjectGuid.ToString());
            }
            else
            {
                SetProjectId(_telemetryService.HashValue(_project.FullPath));
            }
        }

        private bool IsStopTelemetryInAllTargetFrameworks() => 
            !_telemetryStates.IsEmpty && _telemetryStates.Values.All(t => t.StopTelemetry);

        private void UpdateObservedRuleChanges(TelemetryState telemetryState, string rule, bool hasChanges = true)
        {
            telemetryState.ObservedRuleChanges.AddOrUpdate(
                rule, 
                hasChanges, 
                (key, anyChanges) => anyChanges || hasChanges);
        }

        // helper to support testing
        internal void SetProjectId(string projectId)
        {
            _projectId = projectId;
        }
    }
}
