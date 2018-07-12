// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// For maintaining light state about dependency tree to generate telemetry
    /// </summary>
    internal interface IDependencyTreeTelemetryService
    {
        /// <summary>
        /// Indicate whether we have seen all rules we initialized with, in all target frameworks
        /// </summary>
        bool ObservedAllRules();

        /// <summary>
        /// Initialize telemetry state with the set of rules we expect to observe for target framework
        /// </summary>
        void InitializeTargetFrameworkRules(ITargetFramework targetFramework, IEnumerable<string> rules);

        /// <summary>
        /// Indicate that a set of rules has been observed in either an Evaluation or Design Time pass.
        /// This information is used when firing tree update telemetry events to indicate whether all rules
        /// have been observed.
        /// </summary>
        void ObserveTargetFrameworkRules(ITargetFramework targetFramework, IEnumerable<string> rules);

        /// <summary>
        /// Fire telemetry when dependency tree completes an update
        /// </summary>
        /// <param name="hasUnresolvedDependency">indicates if the snapshot used for the update had any unresolved dependencies</param>
        void ObserveTreeUpdateCompleted(bool hasUnresolvedDependency);
    }
}
