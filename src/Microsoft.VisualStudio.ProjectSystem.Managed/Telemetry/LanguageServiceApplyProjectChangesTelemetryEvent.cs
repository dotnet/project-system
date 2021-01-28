// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

namespace Microsoft.VisualStudio.Telemetry
{
    internal class LanguageServiceApplyProjectChangesTelemetryEvent
    {
        internal LanguageServiceApplyProjectChangesTelemetryEvent(ContextState state, string languageServiceOperationName)
        {
            LanguageServiceOperationName = languageServiceOperationName;
            State = state;
        }

        internal string LanguageServiceOperationName { get; }

        internal ContextState State { get; }
    }
}
