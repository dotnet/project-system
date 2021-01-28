// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Telemetry
{
    internal class LanguageServiceCountableOperationsTelemetryEvent
    {
        internal LanguageServiceCountableOperationsTelemetryEvent(string languageServiceOperationName, string projectId, int operationCount)
        {
            ProjectId = projectId ?? string.Empty;
            OperationCount = operationCount;
            LanguageServiceOperationName = languageServiceOperationName;
        }

        internal string LanguageServiceOperationName { get; }

        internal string ProjectId { get; }

        internal int OperationCount { get; }
    }
}
