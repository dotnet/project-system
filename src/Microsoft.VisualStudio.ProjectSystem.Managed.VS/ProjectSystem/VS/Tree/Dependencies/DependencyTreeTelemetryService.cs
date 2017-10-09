// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
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

        private readonly IProjectAsynchronousTasksService _tasksService;
        private readonly ITelemetryService _telemetryService;
        private readonly Dictionary<string, bool> observedRuleChanges = new Dictionary<string, bool>();
        private bool observedDesignTime = false;
        private bool stopTelemetry = false;

        [ImportingConstructor]
        public DependencyTreeTelemetryService(
            IUnconfiguredProjectCommonServices commonServices,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService,
            ITelemetryService telemetryService)
        {
            _tasksService = tasksService;
            _telemetryService = telemetryService;
        }

        public void ObserveTreeUpdateCompleted()
        {
            if (stopTelemetry) return;

            var notReady = !observedRuleChanges.Any() || !observedRuleChanges.All(entry => entry.Value);

            _telemetryService.PostEvent($"{TelemetryEventName}/{(notReady ? UnresolvedLabel : ResolvedLabel)}");
        }

        public void ObserveUnresolvedRules(IEnumerable<string> rules)
        {
            if (stopTelemetry) return;

            foreach (var rule in rules)
            {
                // observe all unresolved rules by default ignoring whether they have actual changes
                UpdateObservedRuleChanges(rule);
            }
        }

        public void ObserveHandlerRulesChanges(
            IEnumerable<string> handlerRules, 
            IImmutableDictionary<string, IProjectChangeDescription> projectChanges)
        {
            if (stopTelemetry) return;

            foreach (var rule in handlerRules)
            {
                var hasChanges = projectChanges.ContainsKey(rule)
                    && projectChanges[rule].Difference.AnyChanges;

                UpdateObservedRuleChanges(rule, hasChanges);
            }
        }

        public void ObserveCompleteHandlers(RuleHandlerType handlerType)
        {
            if (stopTelemetry) return;

            observedDesignTime |= handlerType == RuleHandlerType.DesignTimeBuild;
            stopTelemetry |= observedDesignTime && handlerType == RuleHandlerType.Evaluation;
        }

        private void UpdateObservedRuleChanges(string rule, bool hasChanges = true)
        {
            if (observedRuleChanges.TryGetValue(rule, out var anyChanges))
            {
                observedRuleChanges[rule] = anyChanges || hasChanges;
            }
            else
            {
                observedRuleChanges.Add(rule, hasChanges);
            }
        }
    }
}
