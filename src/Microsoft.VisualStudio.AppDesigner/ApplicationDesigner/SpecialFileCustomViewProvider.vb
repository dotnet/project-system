' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    ''' <summary>
    ''' A provider which can create views of the type SpecialFileCustomView.  See that
    '''   class' description for more information.
    ''' </summary>
    Public Class SpecialFileCustomViewProvider
        Inherits CustomViewProvider

        Private _view As Control
        Private ReadOnly _linkText As String
        Private WithEvents _designerView As ApplicationDesignerView
        Private ReadOnly _designerPanel As ApplicationDesignerPanel
        Private ReadOnly _specialFileId As Integer

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="DesignerView">The ApplicationDesignerView which owns this view.</param>
        ''' <param name="DesignerPanel">The ApplicationDesignerPanel in which this view will be displayed.</param>
        ''' <param name="SpecialFileId">The special file ID to create when the user clicks the link.</param>
        ''' <param name="LinkText">The text of the link message to display.</param>
        Public Sub New(DesignerView As ApplicationDesignerView, DesignerPanel As ApplicationDesignerPanel, SpecialFileId As Integer, LinkText As String)
            Debug.Assert(DesignerView IsNot Nothing)
            _designerView = DesignerView
            Debug.Assert(DesignerPanel IsNot Nothing)
            _designerPanel = DesignerPanel
            _linkText = LinkText
            _specialFileId = SpecialFileId
        End Sub

        ''' <summary>
        ''' The text of the link message to display.
        ''' </summary>
        Public ReadOnly Property LinkText As String
            Get
                Return _linkText
            End Get
        End Property

        ''' <summary>
        ''' The ApplicationDesignerView which owns this view.
        ''' </summary>
        Public ReadOnly Property DesignerView As ApplicationDesignerView
            Get
                Return _designerView
            End Get
        End Property

        ''' <summary>
        ''' The special file ID to create when the user clicks the link.  Used by IVsProjectSpecialFiles.
        ''' </summary>
        Public ReadOnly Property SpecialFileId As Integer
            Get
                Return _specialFileId
            End Get
        End Property

        ''' <summary>
        ''' The ApplicationDesignerPanel in which this view will be displayed.
        ''' </summary>
        Public ReadOnly Property DesignerPanel As ApplicationDesignerPanel
            Get
                Return _designerPanel
            End Get
        End Property

        ''' <summary>
        ''' Returns the view control (if already created)
        ''' </summary>
        Public Overrides ReadOnly Property View As Control
            Get
                Return _view
            End Get
        End Property

        ''' <summary>
        ''' Creates the view control, if it doesn't already exist
        ''' </summary>
        Public Overrides Sub CreateView()
            If _view Is Nothing Then
                Dim NewView As New SpecialFileCustomView
                NewView.LinkLabel.SetThemedColor(_designerPanel.VsUIShell5)
                NewView.SetSite(Me)

                _view = NewView
            End If
        End Sub

        ''' <summary>
        ''' Close the view control, if not already closed
        ''' </summary>
        Public Overrides Sub CloseView()
            If _view IsNot Nothing Then
                _view.Dispose()
                _view = Nothing
            End If
        End Sub

        Private Sub DesignerView_ThemeChanged(sender As Object, args As EventArgs) Handles _designerView.ThemeChanged
            If _view IsNot Nothing Then
                Dim View As SpecialFileCustomView = CType(_view, SpecialFileCustomView)
                View.LinkLabel.SetThemedColor(_designerPanel.VsUIShell5)
            End If
        End Sub

#Region "Dispose/IDisposable"

        ''' <summary>
        ''' Disposes of contained objects
        ''' </summary>
        ''' <param name="disposing"></param>
        Protected Overloads Overrides Sub Dispose(Disposing As Boolean)
            If Disposing Then
                ' Dispose managed resources.
                CloseView()
            End If
            MyBase.Dispose(Disposing)
        End Sub

#End Region

    End Class

    ''' <summary>
    ''' Returns the document of a special file by calling through the IVsProjectSpecialFiles interface
    ''' </summary>
    Public NotInheritable Class SpecialFileCustomDocumentMonikerProvider
        Inherits CustomDocumentMonikerProvider

        Private ReadOnly _specialFileId As Integer
        Private ReadOnly _designerView As ApplicationDesignerView

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="DesignerView">The associated ApplicationDesignerView</param>
        ''' <param name="SpecialFileId">The special file ID for IVsProjectSpecialFiles that will be used to
        '''   obtain the document filename</param>
        Public Sub New(DesignerView As ApplicationDesignerView, SpecialFileId As Integer)
            Requires.NotNull(DesignerView, NameOf(DesignerView))

            _specialFileId = SpecialFileId
            _designerView = DesignerView

#If DEBUG Then
            Try
                Call GetDocumentMoniker()
            Catch ex As Exception When AppDesCommon.ReportWithoutCrash(ex, "Shouldn't be creating a SpecialFileCustomDocumentMonikerProvider instance if the requested special file ID is not supported by the project", NameOf(SpecialFileCustomView))
            End Try
#End If
        End Sub

        Public Overrides Function GetDocumentMoniker() As String
            'Ask the project for the filename (do not create if it doesn't exist)
            Dim ItemId As UInteger
            Dim SpecialFilePath As String = Nothing
            Dim hr As Integer = _designerView.SpecialFiles.GetFile(_specialFileId, CUInt(__PSFFLAGS.PSFF_FullPath), ItemId, SpecialFilePath)
            If VSErrorHandler.Succeeded(hr) AndAlso SpecialFilePath <> "" Then
                'The file is supported (it doesn't necessarily mean that it exists yet)
                Return SpecialFilePath
            Else
                Debug.Fail("Why did the call to IVsProjectSpecialFiles fail?  We shouldn't have created a SpecialFileCustomDocumentMonikerProvider instance in the first place if the project didn't support this special file id" _
                    & vbCrLf & "Hr = 0x" & Hex(hr))
                Throw New InvalidOperationException(My.Resources.Designer.APPDES_SpecialFileNotSupported)
            End If
        End Function

    End Class

End Namespace
