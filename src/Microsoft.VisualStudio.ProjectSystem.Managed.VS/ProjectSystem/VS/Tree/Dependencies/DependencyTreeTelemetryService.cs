// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    /// Maintain light state to fire telemetry on dependency tree
    /// </summary>
    [Export(typeof(IDependencyTreeTelemetryService))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependencyTreeTelemetryService : IDependencyTreeTelemetryService
    {
        private const string TelemetryEventName = "TreeUpdated";
        private const string UnresolvedLabel = "Unresolved";
        private const string ResolvedLabel = "Resolved";
        private const string ProjectProperty = "Project";

        public class TelemetryState
        {
            public Dictionary<string, bool> ObservedRuleChanges { get; } = new Dictionary<string, bool>();
            public bool ObservedDesignTime { get; set; } = false;
            public bool StopTelemetry { get; set; } = false;

            public bool NotReady()
            {
                return !ObservedRuleChanges.Any() || !ObservedRuleChanges.All(entry => entry.Value);
            }
        }

        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly ITelemetryService _telemetryService;
        private readonly Dictionary<ITargetFramework, TelemetryState> telemetryStates = 
            new Dictionary<ITargetFramework, TelemetryState>();
        private readonly string projectHash;
        private bool stopTelemetry = false;

        [ImportingConstructor]
        public DependencyTreeTelemetryService(
            IUnconfiguredProjectCommonServices commonServices,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService,
            ITelemetryService telemetryService)
        {
            _tasksService = tasksService;
            _telemetryService = telemetryService;

            projectHash = GetHash(commonServices.Project.FullPath);
        }

        private string GetHash(string fullPath)
        {
            return fullPath;
        }

        /// <summary>
        /// This is fired when Tree is complete based on three heuristics:
        /// - Marked 'Unresolved' until all TFMs have fired at least 1 Design Time build
        /// - Marked 'Unresolved' if any relevant Resolved Rules are yet to report AnyChanges.
        ///   Relevant Resolved rules are determined based on the handlers that have fired.
        /// - Stops firing completely after an Evaluation is received following the 
        ///   first DT build for that TFM
        /// </summary>
        public void ObserveTreeUpdateCompleted()
        {
            if (stopTelemetry) return;

            var notReady = !telemetryStates.Any() || telemetryStates.Values.Any(s => s.NotReady());

            _telemetryService.PostProperty($"{TelemetryEventName}/{(notReady ? UnresolvedLabel : ResolvedLabel)}",
                ProjectProperty, projectHash);
        }

        public void ObserveUnresolvedRules(ITargetFramework targetFramework, IEnumerable<string> rules)
        {
            if (stopTelemetry) return;

            if (!telemetryStates.TryGetValue(targetFramework, out var telemetryState))
            {
                telemetryState = new TelemetryState();
                telemetryStates[targetFramework] = telemetryState;
            }

            foreach (var rule in rules)
            {
                // observe all unresolved rules by default ignoring whether they have actual changes
                UpdateObservedRuleChanges(telemetryState, rule);
            }
        }

        public void ObserveHandlerRulesChanges(
            ITargetFramework targetFramework,
            IEnumerable<string> handlerRules, 
            IImmutableDictionary<string, IProjectChangeDescription> projectChanges)
        {
            if (stopTelemetry) return;

            if (telemetryStates.TryGetValue(targetFramework, out var telemetryState))
            {
                foreach (var rule in handlerRules)
                {
                    var hasChanges = projectChanges.ContainsKey(rule)
                        && projectChanges[rule].Difference.AnyChanges;

                    UpdateObservedRuleChanges(telemetryState, rule, hasChanges);
                }
            }
        }

        public void ObserveCompleteHandlers(ITargetFramework targetFramework, RuleHandlerType handlerType)
        {
            if (stopTelemetry) return;

            if (telemetryStates.TryGetValue(targetFramework, out var telemetryState))
            {
                telemetryState.ObservedDesignTime |= handlerType == RuleHandlerType.DesignTimeBuild;
                telemetryState.StopTelemetry |= telemetryState.ObservedDesignTime && handlerType == RuleHandlerType.Evaluation;
            }

            stopTelemetry = telemetryStates.Any() && telemetryStates.Values.All(t => t.StopTelemetry);
        }

        private void UpdateObservedRuleChanges(TelemetryState telemetryState, string rule, bool hasChanges = true)
        {
            if (telemetryState.ObservedRuleChanges.TryGetValue(rule, out var anyChanges))
            {
                telemetryState.ObservedRuleChanges[rule] = anyChanges || hasChanges;
            }
            else
            {
                telemetryState.ObservedRuleChanges.Add(rule, hasChanges);
            }
        }
    }
}
