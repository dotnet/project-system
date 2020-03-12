' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.MyExtensibility
    Friend Class TrackProjectDocumentsEventsHelper
        Implements IVsTrackProjectDocumentsEvents2

        Public Shared Function GetInstance(serviceProvider As IServiceProvider) As TrackProjectDocumentsEventsHelper
            Try
                Return New TrackProjectDocumentsEventsHelper(serviceProvider)
            Catch ex As Exception When Common.ReportWithoutCrash(ex, "Fail to listen to IVsTrackProjectDocumentsEvents2", NameOf(TrackProjectDocumentsEventsHelper))
                Return Nothing
            End Try
        End Function

        Public Event AfterAddFilesEx(cProjects As Integer, cFiles As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgpszMkDocuments() As String, rgFlags() As VSADDFILEFLAGS)
        Public Event AfterRemoveFiles(cProjects As Integer, cFiles As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgpszMkDocuments() As String, rgFlags() As VSREMOVEFILEFLAGS)
        Public Event AfterRenameFiles(cProjects As Integer, cFiles As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgszMkOldNames() As String, rgszMkNewNames() As String, rgFlags() As VSRENAMEFILEFLAGS)
        Public Event AfterRemoveDirectories(cProjects As Integer, cDirectories As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgpszMkDocuments() As String, rgFlags() As VSREMOVEDIRECTORYFLAGS)
        Public Event AfterRenameDirectories(cProjects As Integer, cDirs As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgszMkOldNames() As String, rgszMkNewNames() As String, rgFlags() As VSRENAMEDIRECTORYFLAGS)

        ''' ;UnAdviseTrackProjectDocumentsEvents
        ''' <summary>
        ''' Stop listening to IVsTrackProjectDocumentsEvents2
        ''' </summary>
        Public Sub UnAdviseTrackProjectDocumentsEvents()
            If _vsTrackProjectDocumentsEventsCookie <> 0 Then
                If _vsTrackProjectDocuments IsNot Nothing Then
                    _vsTrackProjectDocuments.UnadviseTrackProjectDocumentsEvents(_vsTrackProjectDocumentsEventsCookie)
                    _vsTrackProjectDocuments = Nothing
                End If
                _vsTrackProjectDocumentsEventsCookie = 0
            End If
        End Sub

        Private Sub New(serviceProvider As IServiceProvider)
            Requires.NotNull(serviceProvider, NameOf(serviceProvider))

            _serviceProvider = serviceProvider

            _vsTrackProjectDocuments = TryCast(_serviceProvider.GetService(GetType(SVsTrackProjectDocuments)),
                IVsTrackProjectDocuments2)
            If _vsTrackProjectDocuments Is Nothing Then
                Throw New Exception("Could not get IVsTrackProjectDocuments2!")
            End If

            ErrorHandler.ThrowOnFailure(
                _vsTrackProjectDocuments.AdviseTrackProjectDocumentsEvents(Me, _vsTrackProjectDocumentsEventsCookie))
            Debug.Assert(_vsTrackProjectDocumentsEventsCookie <> 0)
        End Sub

#Region " IVsTrackProjectDocumentsEvents2 methods that are handled "
        Private Function OnAfterAddFilesEx(cProjects As Integer, cFiles As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgpszMkDocuments() As String, rgFlags() As VSADDFILEFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterAddFilesEx
            RaiseEvent AfterAddFilesEx(cProjects, cFiles, rgpProjects, rgFirstIndices, rgpszMkDocuments, rgFlags)
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterRemoveFiles(cProjects As Integer, cFiles As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgpszMkDocuments() As String, rgFlags() As VSREMOVEFILEFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterRemoveFiles
            RaiseEvent AfterRemoveFiles(cProjects, cFiles, rgpProjects, rgFirstIndices, rgpszMkDocuments, rgFlags)
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterRenameFiles(cProjects As Integer, cFiles As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgszMkOldNames() As String, rgszMkNewNames() As String, rgFlags() As VSRENAMEFILEFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterRenameFiles
            RaiseEvent AfterRenameFiles(cProjects, cFiles, rgpProjects, rgFirstIndices, rgszMkOldNames, rgszMkNewNames, rgFlags)
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterRemoveDirectories(cProjects As Integer, cDirectories As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgpszMkDocuments() As String, rgFlags() As VSREMOVEDIRECTORYFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterRemoveDirectories
            RaiseEvent AfterRemoveDirectories(cProjects, cDirectories, rgpProjects, rgFirstIndices, rgpszMkDocuments, rgFlags)
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterRenameDirectories(cProjects As Integer, cDirs As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgszMkOldNames() As String, rgszMkNewNames() As String, rgFlags() As VSRENAMEDIRECTORYFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterRenameDirectories
            RaiseEvent AfterRenameDirectories(cProjects, cDirs, rgpProjects, rgFirstIndices, rgszMkOldNames, rgszMkNewNames, rgFlags)
            Return NativeMethods.S_OK
        End Function
#End Region

#Region " IVsTrackProjectDocumentsEvents2 methods that are ignored "

        Private Function OnAfterAddDirectoriesEx(cProjects As Integer, cDirectories As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgpszMkDocuments() As String, rgFlags() As VSADDDIRECTORYFLAGS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterAddDirectoriesEx
            Return NativeMethods.S_OK
        End Function

        Private Function OnAfterSccStatusChanged(cProjects As Integer, cFiles As Integer, rgpProjects() As IVsProject, rgFirstIndices() As Integer, rgpszMkDocuments() As String, rgdwSccStatus() As UInteger) As Integer Implements IVsTrackProjectDocumentsEvents2.OnAfterSccStatusChanged
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryAddDirectories(pProject As IVsProject, cDirectories As Integer, rgpszMkDocuments() As String, rgFlags() As VSQUERYADDDIRECTORYFLAGS, pSummaryResult() As VSQUERYADDDIRECTORYRESULTS, rgResults() As VSQUERYADDDIRECTORYRESULTS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnQueryAddDirectories
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryAddFiles(pProject As IVsProject, cFiles As Integer, rgpszMkDocuments() As String, rgFlags() As VSQUERYADDFILEFLAGS, pSummaryResult() As VSQUERYADDFILERESULTS, rgResults() As VSQUERYADDFILERESULTS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnQueryAddFiles
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryRemoveDirectories(pProject As IVsProject, cDirectories As Integer, rgpszMkDocuments() As String, rgFlags() As VSQUERYREMOVEDIRECTORYFLAGS, pSummaryResult() As VSQUERYREMOVEDIRECTORYRESULTS, rgResults() As VSQUERYREMOVEDIRECTORYRESULTS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnQueryRemoveDirectories
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryRemoveFiles(pProject As IVsProject, cFiles As Integer, rgpszMkDocuments() As String, rgFlags() As VSQUERYREMOVEFILEFLAGS, pSummaryResult() As VSQUERYREMOVEFILERESULTS, rgResults() As VSQUERYREMOVEFILERESULTS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnQueryRemoveFiles
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryRenameDirectories(pProject As IVsProject, cDirs As Integer, rgszMkOldNames() As String, rgszMkNewNames() As String, rgFlags() As VSQUERYRENAMEDIRECTORYFLAGS, pSummaryResult() As VSQUERYRENAMEDIRECTORYRESULTS, rgResults() As VSQUERYRENAMEDIRECTORYRESULTS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnQueryRenameDirectories
            Return NativeMethods.S_OK
        End Function

        Private Function OnQueryRenameFiles(pProject As IVsProject, cFiles As Integer, rgszMkOldNames() As String, rgszMkNewNames() As String, rgFlags() As VSQUERYRENAMEFILEFLAGS, pSummaryResult() As VSQUERYRENAMEFILERESULTS, rgResults() As VSQUERYRENAMEFILERESULTS) As Integer Implements IVsTrackProjectDocumentsEvents2.OnQueryRenameFiles
            Return NativeMethods.S_OK
        End Function

#End Region

        Private ReadOnly _serviceProvider As IServiceProvider
        Private _vsTrackProjectDocuments As IVsTrackProjectDocuments2
        Private _vsTrackProjectDocumentsEventsCookie As UInteger
    End Class
End Namespace
