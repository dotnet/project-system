// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

namespace Microsoft.VisualStudio.Telemetry
{
    /// <summary>
    /// The managed project system is responsible for initiating the creation of the Roslyn
    /// IWorkspaceProjectContext for each project. This interface enables
    /// the ConfiguredProject scope components related to the IWorkspaceProjectContext
    /// to post events indicating successful initialization. More crucially, the workspace
    /// project context management system needs to report that changes have been successfully
    /// been applied to the IWorkspaceProjectContext for each project by posting these events:
    /// 
    /// Table 1:
    ///  ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    /// | Event Name                                                | Property Name                                                        | Property Value                                                              |
    /// |----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
    /// | <see cref="TelemetryEventName.LanguageServiceOperation"/> | <see cref="TelemetryPropertyName.LanguageServiceOperationName"/>     | <see cref="LanguageServiceOperationNames.ApplyingProjectChangesStarted"/>   |
    /// | <see cref="TelemetryEventName.LanguageServiceOperation"/> | <see cref="TelemetryPropertyName.LanguageServiceOperationName"/>     | <see cref="LanguageServiceOperationNames.ApplyingProjectChangesCompleted"/> |
    ///  ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    /// 
    /// In case of a design time build failure, a <see cref="TelemetryEventName.DesignTimeBuildComplete"/> event
    /// with the <see cref="TelemetryPropertyName.DesignTimeBuildCompleteSucceeded"/>
    /// property set to false will be posted. Should this happen, these sequence of
    /// events will be posted:
    /// 
    /// Table 2:
    ///  ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    /// | Event Name                                                | Property Name                                                        | Property Value                                                              |
    /// |----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
    /// | <see cref="TelemetryEventName.LanguageServiceOperation"/> | <see cref="TelemetryPropertyName.LanguageServiceOperationName"/>     | <see cref="LanguageServiceOperationNames.ApplyingProjectChangesStarted"/>   |
    /// | <see cref="TelemetryEventName.DesignTimeBuildComplete"/>  | <see cref="TelemetryPropertyName.DesignTimeBuildCompleteSucceeded"/> | <see langword="false"/>                                                     |
    /// | <see cref="TelemetryEventName.LanguageServiceOperation"/> | <see cref="TelemetryPropertyName.LanguageServiceOperationName"/>     | <see cref="LanguageServiceOperationNames.ApplyingProjectChangesCompleted"/> |
    ///  ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    /// 
    /// To determine whether changes have been successfully applied to the IWorkspaceProjectContext,
    /// it is therefore sufficient to ensure that the sequence of events in Table 1 is present for
    /// each project while that of Table 2 is not present for each project.
    /// 
    /// This interface also provides methods for the package restore components to post telemetry events
    /// indicating whether the initialization (for the corresponding ConfiguredProject) suceeded.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IConfiguredProjectLanguageServiceTelemetryService
    {
        /// <summary>
        /// Post a <see cref="TelemetryEventName.DesignTimeBuildComplete"/> event
        /// with the <see cref="TelemetryPropertyName.DesignTimeBuildCompleteSucceeded"/> property set to false
        /// when a design time build fails.
        /// </summary>
        void PostDesignTimeBuildFailureEvent();

        /// <summary>
        /// Post a <see cref="TelemetryEventName.LanguageServiceOperation"/> event with information about
        /// changes being applied to a workspace project context. The 
        /// <see cref="TelemetryPropertyName.LanguageServiceOperationName"/> property is set to
        /// <see cref="LanguageServiceOperationNames.ApplyingProjectChangesStarted"/> if <paramref name="starting"/>
        /// is set to <see langword="true"/>. Otherwise, the it is set to
        /// <see cref="LanguageServiceOperationNames.ApplyingProjectChangesCompleted"/>.
        /// </summary>
        /// <param name="state">The state of the language service operation.</param>
        /// <param name="workspaceContextId">An identifier of the workspace in which a language service operation has occurred.</param>
        /// <param name="eventId">An identifier to enable correlation of the specific language service operation events.</param>
        /// <param name="starting">Flag indicating whether the project changes are starting to be applied or completing.</param>
        void PostApplyProjectChangesEvent(ContextState state, long workspaceContextId, long eventId, bool starting);

        /// <summary>
        /// Post a language service telemetry event with a correlation identifier.
        /// </summary>
        /// <param name="languageServiceOperationName">The name of the specific language service operation that was invoked.</param>
        /// <param name="workspaceContextId">An identifier to enable correlation of events.</param>
        void PostLanguageServiceEvent(string languageServiceOperationName, long workspaceContextId);

        /// <summary>
        /// Post a telemetry event when a event with an operation counter has been processed.
        /// </summary>
        /// <param name="languageServiceOperationName">The name of the specific language service operation that was invoked.</param>
        /// <param name="operationCount">The number of times the operation has been performed.</param>
        void PostLanguageServiceEvent(string languageServiceOperationName, int operationCount);
    }
}
