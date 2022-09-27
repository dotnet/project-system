' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Explicit On
Option Strict On
Option Compare Binary

Imports Microsoft.VisualStudio.Telemetry

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    Friend NotInheritable Class ResourceEditorTelemetry

        Public Shared Sub OnResourcesLoaded(resources As Dictionary(Of String, Resource), metadataCount As Integer)

            Dim resourceCount = resources.Count
            Dim stringCount = 0
            Dim bitmapCount = 0
            Dim iconCount = 0
            Dim audioCount = 0
            Dim otherCount = 0
            Dim commentCount = 0
            Dim linkCount = 0

            For Each resource As Resource In resources.Values
                If resource.FriendlyValueTypeName = "System.String" Then
                    stringCount += 1
                ElseIf resource.FriendlyValueTypeName = "System.Drawing.Bitmap" Then
                    bitmapCount += 1
                ElseIf resource.FriendlyValueTypeName = "System.Drawing.Icon" Then
                    iconCount += 1
                ElseIf resource.FriendlyTypeDescription = "Wave Sound" Then
                    audioCount += 1
                Else
                    otherCount += 1
                End If

                If Not String.IsNullOrWhiteSpace(resource.Comment) Then
                    commentCount += 1
                End If

                If resource.IsLink Then
                    linkCount += 1
                End If
            Next

            Dim telemetryEvent = New TelemetryEvent("vs/projectsystem/editors/resourceeditor/loaded")
            telemetryEvent.Properties("vs.projectsystem.editors.resourceeditor.resourcecount") = resourceCount
            telemetryEvent.Properties("vs.projectsystem.editors.resourceeditor.metadatacount") = metadataCount
            telemetryEvent.Properties("vs.projectsystem.editors.resourceeditor.stringcount") = stringCount
            telemetryEvent.Properties("vs.projectsystem.editors.resourceeditor.bitmapcount") = bitmapCount
            telemetryEvent.Properties("vs.projectsystem.editors.resourceeditor.iconcount") = iconCount
            telemetryEvent.Properties("vs.projectsystem.editors.resourceeditor.audioCount") = audioCount
            telemetryEvent.Properties("vs.projectsystem.editors.resourceeditor.othercount") = otherCount
            telemetryEvent.Properties("vs.projectsystem.editors.resourceeditor.commentCount") = commentCount
            telemetryEvent.Properties("vs.projectsystem.editors.resourceeditor.linkCount") = linkCount
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
