// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Telemetry
{
    internal class LanguageServiceProjectEvaluationTelemetryEvent
    {
        internal LanguageServiceProjectEvaluationTelemetryEvent(string projectId, int projectEvaluationHandlersProcessed)
        {
            ProjectId = projectId ?? string.Empty;
            ProjectEvaluationHandlersInvoked = projectEvaluationHandlersProcessed;
            LanguageServiceOperationName = LanguageServiceOperationNames.ProjectEvaluationHandlersProcessed;
        }

        internal string LanguageServiceOperationName { get; }

        internal string ProjectId { get; }

        internal int ProjectEvaluationHandlersInvoked { get; }
    }
}
