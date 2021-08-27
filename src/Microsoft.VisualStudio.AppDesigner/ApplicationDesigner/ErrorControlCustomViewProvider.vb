' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.AppDesDesignerFramework

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    ''' <summary>
    ''' Provides a custom view for DesignerPanel that creates an Error control for its view.
    ''' </summary>
    Public Class ErrorControlCustomViewProvider
        Inherits CustomViewProvider

        Private _view As ErrorControl 'The Error control as view
        Private ReadOnly _errorText As String    'Error text, if given
        Private ReadOnly _exception As Exception 'Error exception, if given

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="ErrorText">The error text to display in the error control.</param>
        Public Sub New(ErrorText As String)
            _errorText = ErrorText
        End Sub

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="Exception">The exception to display in the error control.</param>
        Public Sub New(Exception As Exception)
            _exception = Exception
        End Sub

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
                If _exception IsNot Nothing Then
                    _view = New ErrorControl(_exception)
                Else
                    _view = New ErrorControl(_errorText)
                End If
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

End Namespace
