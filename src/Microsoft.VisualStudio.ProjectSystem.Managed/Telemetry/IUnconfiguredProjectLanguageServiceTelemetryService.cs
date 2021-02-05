// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    /// <summary>
    /// The managed project system is responsible for initiating the creation of the Roslyn
    /// IWorkspaceProjectContext for each project in the solution. This interface enables
    /// the UnconfiguredProject scope components related to the IWorkspaceProjectContext
    /// to post events indicating successful initialization. The absence of these events
    /// would indicate an initialization failure.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IUnconfiguredProjectLanguageServiceTelemetryService
    {
        /// <summary>
        /// Posts a <see cref="TelemetryEventName.LanguageServiceOperation"/> event with the
        /// <see cref="TelemetryPropertyName.LanguageServiceOperationName"/> value
        /// of <see cref="LanguageServiceOperationNames.ActiveWorkspaceProjectContextHostPublishing"/>
        /// </summary>
        void PostActiveWorkspaceProjectContextHostPublishingEvent();

        /// <summary>
        /// Posts a <see cref="TelemetryEventName.LanguageServiceOperation"/> event with the
        /// <see cref="TelemetryPropertyName.LanguageServiceOperationName"/> value
        /// of <see cref="LanguageServiceOperationNames.WorkspaceProjectContextHostInitiatorInitialized"/>
        /// </summary>
        void PostWorkspaceProjectContextHostInitiatorInitializedEvent();
    }
}
