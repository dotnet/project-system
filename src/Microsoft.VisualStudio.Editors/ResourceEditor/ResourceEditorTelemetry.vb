' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Explicit On
Option Strict On
Option Compare Binary

Imports Microsoft.VisualStudio.Telemetry

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    Friend NotInheritable Class ResourceEditorTelemetry

        Public Shared Sub OnResourcesLoaded(resourceCount As Integer, metadataCount As Integer)
            Dim telemetryEvent = New TelemetryEvent("vs/projectsystem/editors/resourceeditor/loaded")
            telemetryEvent.Properties("vs.projectsystem.editors.resourceeditor.resourcecount") = resourceCount
            telemetryEvent.Properties("vs.projectsystem.editors.resourceeditor.metadatacount") = metadataCount
            TelemetryService.DefaultSession.PostEvent(telemetryEvent)
        End Sub

        Public Shared Sub OnResourceAdded(valueTypeName As String)
            Dim telemetryEvent = New TelemetryEvent("vs/projectsystem/editors/resourceeditor/resourceadded")
            telemetryEvent.Properties("vs.projectsystem.editors.resourceeditor.valuetype") = valueTypeName
            TelemetryService.DefaultSession.PostEvent(telemetryEvent)
        End Sub

        Public Shared Sub OnResourceRemoved(valueTypeName As String)
            Dim telemetryEvent = New TelemetryEvent("vs/projectsystem/editors/resourceeditor/resourceremoved")
            telemetryEvent.Properties("vs.projectsystem.editors.resourceeditor.valuetype") = valueTypeName
            TelemetryService.DefaultSession.PostEvent(telemetryEvent)
        End Sub

        Public Shared Sub OnResourceRenamed(valueTypeName As String)
            Dim telemetryEvent = New TelemetryEvent("vs/projectsystem/editors/resourceeditor/resourcerenamed")
            telemetryEvent.Properties("vs.projectsystem.editors.resourceeditor.valuetype") = valueTypeName
            TelemetryService.DefaultSession.PostEvent(telemetryEvent)
        End Sub

        Public Shared Sub OnResourceChanged(valueTypeName As String)
            Dim telemetryEvent = New TelemetryEvent("vs/projectsystem/editors/resourceeditor/resourcechanged")
            telemetryEvent.Properties("vs.projectsystem.editors.resourceeditor.valuetype") = valueTypeName
            TelemetryService.DefaultSession.PostEvent(telemetryEvent)
        End Sub

    End Class

End Namespace
