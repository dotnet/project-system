' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.AppDesDesignerFramework


    ''' <summary>
    ''' This is a Windows control that is shown when there is an exception loading a designer or property page.
    ''' All it does is display an error message and an error icon.
    ''' </summary>
    ''' <remarks></remarks>
    Public NotInheritable Class ErrorControl

        Private _firstGotFocus As Boolean = True
        Private _sizingLabel As Label

        Public Sub New()
            ' This call is required by the Windows Form Designer.
            InitializeComponent()

            ' Add any initialization after the InitializeComponent() call.
            IconGlyph.Image = SystemIcons.Error.ToBitmap()

            ' A label used for determining the preferred size of the text in the textbox
            _sizingLabel = New Label()
        End Sub


        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="Text">The error text to display</param>
        ''' <remarks></remarks>
        Public Sub New(Text As String)
            Me.New()
            Me.Text = Text
        End Sub


        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="ex">The exception to display</param>
        Public Sub New(ex As Exception)
            Me.New(AppDesCommon.DebugMessageFromException(ex))
        End Sub


        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="errors">A list of exceptions or error messages to display</param>
        Public Sub New(errors As ICollection)
            Me.New()

            Dim TextBuilder As New System.Text.StringBuilder

            For Each er As Object In errors
                TextBuilder.Append(er.ToString())
                TextBuilder.AppendLine()
            Next

            Text = TextBuilder.ToString()
        End Sub


        ''' <summary>
        ''' Text
        ''' </summary>
        Public Overrides Property Text() As String
            Get
                Return ErrorText.Text
            End Get
            Set(ByVal value As String)
                MyBase.Text = value
                ErrorText.Text = value
            End Set
        End Property


        ''' <summary>
        ''' Fires when the ErrorText gets focus
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ErrorText_GotFocus(sender As Object, e As EventArgs) Handles ErrorText.GotFocus
            If _firstGotFocus Then
                'The first time a textbox gets focus, WinForms selects all text in it.  That
                '  doesn't really make sense in this case, so set it back to no selection.
                ErrorText.SelectionLength = 0
                ErrorText.SelectionStart = ErrorText.Text.Length
                _firstGotFocus = False
            End If
        End Sub


        ''' <summary>
        ''' Get the preferred size of the control, expanding 
        ''' </summary>
        ''' <param name="proposedSize"></param>
        Public Overrides Function GetPreferredSize(
                                                  proposedSize As Size
                                                  ) As Size
            If proposedSize.Width = 0 Then Return MyBase.GetPreferredSize(proposedSize)

            Dim sizeBeyondTheTextbox = Size - ErrorText.Size

            'Use a label of the same size to determine the preferred size.  We use the
            '  suggested width, and expand the height as needed.
            With _sizingLabel
                .Font = ErrorText.Font
                .Text = ErrorText.Text & vbCrLf & vbCrLf & " " 'Add an extra line of buffer
                .Width = proposedSize.Width - sizeBeyondTheTextbox.Width
                .AutoSize = False

                Dim textPreferredSize = .GetPreferredSize(New Size(.Width, 0))
                Return textPreferredSize + sizeBeyondTheTextbox
            End With
        End Function

    End Class

End Namespace
