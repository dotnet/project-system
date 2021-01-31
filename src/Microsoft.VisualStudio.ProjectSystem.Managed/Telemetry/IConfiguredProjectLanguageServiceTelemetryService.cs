// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

namespace Microsoft.VisualStudio.Telemetry
{
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IConfiguredProjectLanguageServiceTelemetryService
    {
        /// <summary>
        /// Post a telemetry event when a design time build fails.
        /// </summary>
        void PostDesignTimeBuildFailureEvent();

        /// <summary>
        /// Post a telemetry event with information about changes being applied to a project.
        /// </summary>
        /// <param name="languageServiceOperationName">The name of the specific language service operation that was invoked.</param>
        /// <param name="state">The state of the language service operation.</param>
        void PostLanguageServiceEvent(string languageServiceOperationName, ContextState state);

        /// <summary>
        /// Post a language service telemetry event without any corresponding metadata.
        /// </summary>
        /// <param name="languageServiceOperationName">The name of the specific language service operation that was invoked.</param>
        void PostLanguageServiceEvent(string languageServiceOperationName);

        /// <summary>
        /// Post a telemetry event when a event with an operation counter has been processed.
        /// </summary>
        /// <param name="languageServiceOperationName">The name of the specific language service operation that was invoked.</param>
        /// <param name="operationCount">The number of times the operation has been performed.</param>
        void PostLanguageServiceEvent(string languageServiceOperationName, int operationCount);
    }
}
