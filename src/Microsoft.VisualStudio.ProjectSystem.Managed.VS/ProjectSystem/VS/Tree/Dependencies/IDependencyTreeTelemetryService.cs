// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// For maintaining light state about dependency tree to generate telemetry
    /// </summary>
    internal interface IDependencyTreeTelemetryService
    {
        /// <summary>
        /// Fire telemetry when dependency tree completes an update
        /// </summary>
        void ObserveTreeUpdateCompleted();

        /// <summary>
        /// Add rules to the list that is always unresolved and hence should be  
        /// ignored when determining whether all rules have resolved. Typically 
        /// these are "Evaluation-only" rules.
        /// </summary>
        void ObserveUnresolvedRules(ITargetFramework targetFramework, IEnumerable<string> rules);

        /// <summary>
        /// Observe the full set of rules processed by a handler, and determine 
        /// if they have any changes.
        /// </summary>
        void ObserveHandlerRulesChanges(
            ITargetFramework targetFramework,
            IEnumerable<string> handlerRules,
            IImmutableDictionary<string, IProjectChangeDescription> projectChanges);

        /// <summary>
        /// Observe when a pass handling all changed rules has completed, indicating
        /// whether the pass covered Evaluation or DesignTimeBuild rules
        /// </summary>
        void ObserveCompleteHandlers(ITargetFramework targetFramework, RuleHandlerType handlerType);
    }
}
