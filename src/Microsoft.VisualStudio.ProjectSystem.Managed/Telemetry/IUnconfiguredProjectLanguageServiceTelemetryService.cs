// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IUnconfiguredProjectLanguageServiceTelemetryService
    {
        /// <summary>
        /// Post a language service telemetry event from an unconfigured project.
        /// </summary>
        /// <param name="languageServiceOperationName">The name of the specific language service operation that was invoked.</param>
        void PostLanguageServiceEvent(string languageServiceOperationName);
    }
}
