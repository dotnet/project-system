// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.Tracing;

namespace Microsoft.VisualStudio.ProjectSystem.Telemetry;

[EventSource(Name = "Microsoft-VisualStudio-DotNetProjectSystem")]
internal sealed class DotNetProjectSystemEventSource : EventSource
{
    public static readonly DotNetProjectSystemEventSource Instance = new();

    private DotNetProjectSystemEventSource() { }

    [Event(eventId: 1, Level = EventLevel.Informational)]
    public void NominateForRestore(string projectFilePath, string changes)
    {
        WriteEvent(eventId: 1, projectFilePath, changes);
    }
}
