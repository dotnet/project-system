' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    ''' <summary>
    ''' This is a Windows control that is shown when there is an exception loading a designer or property page.
    ''' All it does is display an error message and an error icon.
    ''' </summary>
    Friend NotInheritable Class ErrorControl

        Private _firstGotFocus As Boolean = True
        Private ReadOnly _sizingLabel As Label

        Friend Sub New()
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
        Friend Sub New(Text As String)
            Me.New()
            Me.Text = Text
        End Sub

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="ex">The exception to display</param>
        Friend Sub New(ex As Exception)
            Me.New(Common.DebugMessageFromException(ex))
        End Sub

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="errors">A list of exceptions or error messages to display</param>
        Friend Sub New(errors As ICollection)
            Me.New()

            Dim TextBuilder As New System.Text.StringBuilder

            For Each er As Object In errors
                TextBuilder.Append(er.ToString())
                TextBuilder.Append(vbCrLf)
            Next

            Text = TextBuilder.ToString()
        End Sub

        ''' <summary>
        ''' Constructor
        ''' </summary>
        Public Overrides Property Text As String
            Get
                Return ErrorText.Text
            End Get
            Set
                MyBase.Text = value
                ErrorText.Text = value
            End Set
        End Property

        ''' <summary>
        ''' Fires when the ErrorText gets focus
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
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
        Public Overrides Function GetPreferredSize(proposedSize As Size) As Size
            If proposedSize.Width = 0 Then
                Return MyBase.GetPreferredSize(proposedSize)
            End If

            Dim sizeBeyondTheTextbox As Size = Size.Subtract(Size, ErrorText.Size)

            'Use a label of the same size to determine the preferred size.  We use the
            '  suggested width, and expand the height as needed.
            _sizingLabel.Font = ErrorText.Font
            _sizingLabel.Text = ErrorText.Text & vbCrLf & vbCrLf & " " 'Add an extra line of buffer
            _sizingLabel.Width = proposedSize.Width - sizeBeyondTheTextbox.Width
            _sizingLabel.AutoSize = False

            Dim textPreferredSize As Size = _sizingLabel.GetPreferredSize(New Size(_sizingLabel.Width, 0))
            Return Size.Add(textPreferredSize, sizeBeyondTheTextbox)
        End Function

    End Class

End Namespace
