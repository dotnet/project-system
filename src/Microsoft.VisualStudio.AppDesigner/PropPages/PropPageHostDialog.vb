' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.AppDesDesignerFramework

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Public NotInheritable Class PropPageHostDialog
        Inherits BaseDialog
        'Inherits Form

        Private _propPage As PropPageUserControlBase
        Public WithEvents Cancel As Button
        Public WithEvents OK As Button
#Disable Warning IDE1006 ' Naming Styles (Compat)
        Public WithEvents okCancelTableLayoutPanel As TableLayoutPanel
        Public WithEvents overArchingTableLayoutPanel As TableLayoutPanel
#Enable Warning IDE1006 ' Naming Styles
        Private _firstFocusHandled As Boolean

        ''' <summary>
        ''' Gets the F1 keyword to push into the user context for this property page
        ''' </summary>
        Protected Overrides Property F1Keyword As String
            Get
                Dim keyword As String = MyBase.F1Keyword
                If String.IsNullOrEmpty(keyword) AndAlso _propPage IsNot Nothing Then
                    Return DirectCast(_propPage, IPropertyPageInternal).GetHelpContextF1Keyword()
                End If
                Return keyword
            End Get
            Set
                MyBase.F1Keyword = Value
            End Set
        End Property

        Public Property PropPage As PropPageUserControlBase
            Get
                Return _propPage
            End Get
            Set
                SuspendLayout()
                If _propPage IsNot Nothing Then
                    'Remove previous page if any
                    overArchingTableLayoutPanel.Controls.Remove(_propPage)
                End If
                _propPage = Value
                If _propPage IsNot Nothing Then
                    'm_propPage.SuspendLayout()
                    BackColor = Value.BackColor
                    MinimumSize = Drawing.Size.Empty
                    AutoSize = True

                    If _propPage.PageResizable Then
                        FormBorderStyle = FormBorderStyle.Sizable
                    Else
                        FormBorderStyle = FormBorderStyle.FixedDialog
                    End If

                    _propPage.Margin = New Padding(0, 0, 0, 0)
                    _propPage.Anchor = CType(AnchorStyles.Top Or AnchorStyles.Bottom _
                        Or AnchorStyles.Left _
                        Or AnchorStyles.Right, AnchorStyles)
                    _propPage.TabIndex = 0
                    'overArchingTableLayoutPanel.SuspendLayout()
                    overArchingTableLayoutPanel.Controls.Add(_propPage, 0, 0)
                    'overArchingTableLayoutPanel.ResumeLayout(False)

                    'm_propPage.ResumeLayout(False)
                End If
                ResumeLayout(False)
                PerformLayout()
                SetFocusToPage()
            End Set
        End Property

#Region " Windows Form Designer generated code "

        'Form overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If _components IsNot Nothing Then
                    _components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        'Required by the Windows Form Designer
        Private ReadOnly _components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(PropPageHostDialog))
            OK = New Button
            Cancel = New Button
            okCancelTableLayoutPanel = New TableLayoutPanel
            overArchingTableLayoutPanel = New TableLayoutPanel
            okCancelTableLayoutPanel.SuspendLayout()
            overArchingTableLayoutPanel.SuspendLayout()
            SuspendLayout()
            '
            'OK
            '
            resources.ApplyResources(OK, "OK")
            OK.DialogResult = DialogResult.OK
            OK.Margin = New Padding(0, 0, 3, 0)
            OK.Name = "OK"
            '
            'Cancel
            '
            resources.ApplyResources(Cancel, "Cancel")
            Cancel.CausesValidation = False
            Cancel.DialogResult = DialogResult.Cancel
            Cancel.Margin = New Padding(3, 0, 0, 0)
            Cancel.Name = "Cancel"
            '
            'okCancelTableLayoutPanel
            '
            resources.ApplyResources(okCancelTableLayoutPanel, "okCancelTableLayoutPanel")
            okCancelTableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0!))
            okCancelTableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0!))
            okCancelTableLayoutPanel.Controls.Add(Cancel, 1, 0)
            okCancelTableLayoutPanel.Controls.Add(OK, 0, 0)
            okCancelTableLayoutPanel.Margin = New Padding(0, 6, 0, 0)
            okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel"
            okCancelTableLayoutPanel.RowStyles.Add(New RowStyle)
            '
            'overArchingTableLayoutPanel
            '
            resources.ApplyResources(overArchingTableLayoutPanel, "overArchingTableLayoutPanel")
            overArchingTableLayoutPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0!))
            overArchingTableLayoutPanel.Controls.Add(okCancelTableLayoutPanel, 0, 1)
            overArchingTableLayoutPanel.Name = "overArchingTableLayoutPanel"
            overArchingTableLayoutPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0!))
            overArchingTableLayoutPanel.RowStyles.Add(New RowStyle)
            '
            'PropPageHostDialog
            '
            resources.ApplyResources(Me, "$this")
            Controls.Add(overArchingTableLayoutPanel)
            Padding = New Padding(12, 12, 12, 12)
            FormBorderStyle = FormBorderStyle.FixedDialog
            HelpButton = True
            MaximizeBox = False
            MinimizeBox = False
            Name = "PropPageHostDialog"
            ' Do not scale, the proppage will handle it. If we set AutoScale here, the page will expand twice, and becomes way huge
            'Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            ShowIcon = False
            ShowInTaskbar = False
            okCancelTableLayoutPanel.ResumeLayout(False)
            okCancelTableLayoutPanel.PerformLayout()
            overArchingTableLayoutPanel.ResumeLayout(False)
            overArchingTableLayoutPanel.PerformLayout()
            ResumeLayout(False)
            PerformLayout()

        End Sub

#End Region

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="ServiceProvider"></param>
        Public Sub New(ServiceProvider As IServiceProvider, F1Keyword As String)
            MyBase.New(ServiceProvider)

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call
            Me.F1Keyword = F1Keyword

            AcceptButton = OK
            CancelButton = Cancel
        End Sub

        Protected Overrides Sub OnShown(e As EventArgs)
            MyBase.OnShown(e)

            If MinimumSize.IsEmpty Then
                MinimumSize = Size
                AutoSize = False
            End If
        End Sub

        Private Sub Cancel_Click(sender As Object, e As EventArgs) Handles Cancel.Click
            PropPage.RestoreInitialValues()
            Close()
        End Sub

        Private Sub OK_Click(sender As Object, e As EventArgs) Handles OK.Click
            'Save the changes if current values
            Try
                'No errors in the values, apply & close the dialog
                If PropPage.IsDirty Then
                    PropPage.Apply()
                End If
                Close()
            Catch ex As ValidationException
                _propPage.ShowErrorMessage(ex)
                ex.RestoreFocus()
                Return
            Catch ex As SystemException
                _propPage.ShowErrorMessage(ex)
                Return
            Catch ex As Exception When AppDesCommon.ReportWithoutCrash(ex, NameOf(OK_Click), NameOf(PropPageHostDialog))
                _propPage.ShowErrorMessage(ex)
                Return
            End Try
        End Sub

        Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
            If e.CloseReason = CloseReason.None Then
                ' That happens when the user clicks the OK button, but validation failed
                ' That is how we block the user leave when something wrong.
                e.Cancel = True
            ElseIf DialogResult <> DialogResult.OK Then
                ' If the user cancelled the edit, we should restore the initial values...
                PropPage.RestoreInitialValues()
            End If
        End Sub

        Public Sub SetFocusToPage()
            If Not _firstFocusHandled AndAlso _propPage IsNot Nothing Then
                _firstFocusHandled = True
                For i As Integer = 0 To _propPage.Controls.Count - 1
                    With _propPage.Controls.Item(i)
                        If .CanFocus() Then
                            .Focus()
                            Return
                        End If
                    End With
                Next i
            End If
        End Sub

        Private Sub PropPageHostDialog_HelpButtonClicked(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.HelpButtonClicked
            e.Cancel = True
            ShowHelp()
        End Sub
    End Class

End Namespace

