' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.VisualStudio.LiveShare


Namespace Microsoft.VisualStudio.Editors.Common

    ''' <summary>
    ''' This is a temporary hack.
    ''' In cloud environments we need a way to map local file paths back to the orignal
    ''' "server" path and vice versa; e.g. in order to pass to some server API that
    ''' operates on the file. There is currently no <see cref="ServiceHub.Framework.IServiceBroker"/>-based
    ''' API to do this, so here we have a standard workaround that relies on Live Share.
    ''' This should be removed when an alternative is available.
    ''' </summary>
    ''' <remarks>
    ''' We've had to make Microsoft.VisualStudio.Editors.dll a MEF component so that Live
    ''' Shared will properly find and initialize the <see cref="RemotePathServiceFactory"/>
    ''' (see below). We can stop doing that (and remove the PackageReference to
    ''' Microsoft.VisualStudio.LiveShare) when this type is no longer needed.
    ''' </remarks>
    Friend Class RemotePathService
        Implements ICollaborationService, IDisposable

        Private Shared s_singleton As RemotePathService

        Private _collaborationSession As CollaborationSession

        Public Sub New(collaborationSession As CollaborationSession)
            s_singleton = Me

            _collaborationSession = collaborationSession
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            s_singleton = Nothing
        End Sub

        Public Shared Function TranslateToSharedPathIfNecessary(path As String, isDirectory As Boolean) As String
            If s_singleton Is Nothing OrElse s_singleton._collaborationSession.SessionId Is Nothing Then
                Return path
            End If

            Dim unadjust = isDirectory AndAlso AdjustDirectory(path)

            Dim result As String = s_singleton._collaborationSession.ConvertLocalPathToSharedUri(path)?.ToString()

            ' If the path is outside the Live Share directory cone then conversion will fail. Return the original path.
            If result Is Nothing Then
                result = path
            End If

            If unadjust Then
                UnadjustDirectory(result)
            End If

            Return result
        End Function

        Public Shared Function TranslateFromSharedPathIfNecessary(sharedPath As String, isDirectory As Boolean) As String
            If s_singleton Is Nothing OrElse s_singleton._collaborationSession.SessionId Is Nothing Then
                Return sharedPath
            End If

            Dim unadjust = isDirectory AndAlso AdjustDirectory(sharedPath)

            Dim result As String = s_singleton._collaborationSession.ConvertSharedUriToLocalPath(New Uri(sharedPath, UriKind.RelativeOrAbsolute))

            ' If the path is outside the Live Share directory cone then conversion will fail. Return the original path.
            If result Is Nothing Then
                result = sharedPath
            Else
                ' Paths come out with Unix-style forward slashes. Fix them.
                result = result.Replace("/", "\\")
            End If

            If unadjust Then
                UnadjustDirectory(result)
            End If

            Return result
        End Function

        Private Shared Function AdjustDirectory(ByRef path As String) As Boolean
            If path.EndsWith("\\") Then
                Return False
            End If

            path = path + "\\"
            Return True
        End Function

        Private Shared Sub UnadjustDirectory(ByRef path As String)
            If path.EndsWith("\\") Then
                path = path.Substring(0, path.Length - 1)
            End If
        End Sub
    End Class

    <ExportCollaborationService(
        GetType(RemotePathService),
        Scope:=SessionScope.All,
        Role:=ServiceRole.LocalService)>
    Friend Class RemotePathServiceFactory
        Implements ICollaborationServiceFactory

        Public Function CreateServiceAsync(sessionContext As CollaborationSession, cancellationToken As CancellationToken) As Task(Of ICollaborationService) Implements ICollaborationServiceFactory.CreateServiceAsync
            Return Task.FromResult(Of ICollaborationService)(New RemotePathService(sessionContext))
        End Function
    End Class
End Namespace

