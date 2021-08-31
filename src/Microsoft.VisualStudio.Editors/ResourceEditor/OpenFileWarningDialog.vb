' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Drawing

Imports Microsoft.VisualStudio.Editors.DesignerFramework

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    Friend Class OpenFileWarningDialog
        Inherits BaseDialog
        'Inherits System.Windows.Forms.Form

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="ServiceProvider"></param>
        Public Sub New(ServiceProvider As IServiceProvider, fileName As String)
            MyBase.New(ServiceProvider)

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call
            alwaysCheckCheckBox.Checked = True
            messageLabel.Text = String.Format(messageLabel.Text, fileName)
            messageLabel.PerformLayout()
            dialogLayoutPanel.PerformLayout()

            ClientSize = New Size(ClientSize.Width, dialogLayoutPanel.Size.Height + Padding.Top * 2)
            AddHandler dialogLayoutPanel.SizeChanged, AddressOf TableLayoutPanelSizeChanged
            F1Keyword = HelpIDs.Dlg_OpenFileWarning
        End Sub

        Private Sub TableLayoutPanelSizeChanged(sender As Object, e As EventArgs)
            ClientSize = New Size(ClientSize.Width, dialogLayoutPanel.Size.Height + Padding.Top * 2)
        End Sub

        'Form overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing AndAlso _components IsNot Nothing Then
                RemoveHandler dialogLayoutPanel.SizeChanged, AddressOf TableLayoutPanelSizeChanged
                _components.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub

        ''' <summary>
        ''' returns whether we need pop up a warning dialog for this extension again
        ''' </summary>
        Public ReadOnly Property AlwaysCheckForThisExtension As Boolean
            Get
                Return alwaysCheckCheckBox.Checked
            End Get
        End Property

        ''' <summary>
        ''' Click handler for the OK button
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ButtonOk_Click(sender As Object, e As EventArgs) Handles buttonOK.Click
            Close()
        End Sub
        Friend WithEvents messageLabel2 As System.Windows.Forms.Label

        ''' <summary>
        ''' Click handler for the Help button
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub DialogQueryName_HelpButtonClicked(sender As Object, e As ComponentModel.CancelEventArgs) Handles MyBase.HelpButtonClicked
            e.Cancel = True
            ShowHelp()
        End Sub

        'Required by the Windows Form Designer
        Private ReadOnly _components As ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <DebuggerStepThrough>
        Private Sub InitializeComponent()
            Dim resources As ComponentModel.ComponentResourceManager = New ComponentModel.ComponentResourceManager(GetType(OpenFileWarningDialog))
            dialogLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            alwaysCheckCheckBox = New System.Windows.Forms.CheckBox
            messageLabel = New System.Windows.Forms.Label
            buttonOK = New System.Windows.Forms.Button
            buttonCancel = New System.Windows.Forms.Button
            messageLabel2 = New System.Windows.Forms.Label
            dialogLayoutPanel.SuspendLayout()
            SuspendLayout()
            '
            'dialogLayoutPanel
            '
            resources.ApplyResources(dialogLayoutPanel, "dialogLayoutPanel")
            dialogLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            dialogLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            dialogLayoutPanel.Controls.Add(alwaysCheckCheckBox, 0, 2)
            dialogLayoutPanel.Controls.Add(messageLabel, 0, 0)
            dialogLayoutPanel.Controls.Add(buttonOK, 0, 3)
            dialogLayoutPanel.Controls.Add(buttonCancel, 1, 3)
            dialogLayoutPanel.Controls.Add(messageLabel2, 0, 1)
            dialogLayoutPanel.Name = "dialogLayoutPanel"
            dialogLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            dialogLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            dialogLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            dialogLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'alwaysCheckCheckBox
            '
            resources.ApplyResources(alwaysCheckCheckBox, "alwaysCheckCheckBox")
            dialogLayoutPanel.SetColumnSpan(alwaysCheckCheckBox, 2)
            alwaysCheckCheckBox.Name = "alwaysCheckCheckBox"
            '
            'messageLabel
            '
            resources.ApplyResources(messageLabel, "messageLabel")
            dialogLayoutPanel.SetColumnSpan(messageLabel, 2)
            messageLabel.Name = "messageLabel"
            '
            'buttonOK
            '
            resources.ApplyResources(buttonOK, "buttonOK")
            buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK
            buttonOK.Name = "buttonOK"
            '
            'buttonCancel
            '
            resources.ApplyResources(buttonCancel, "buttonCancel")
            buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
            buttonCancel.Name = "buttonCancel"
            '
            'messageLabel2
            '
            resources.ApplyResources(messageLabel2, "messageLabel2")
            dialogLayoutPanel.SetColumnSpan(messageLabel2, 2)
            messageLabel2.Name = "messageLabel2"
            '
            'OpenFileWarningDialog
            '
            AcceptButton = buttonOK
            resources.ApplyResources(Me, "$this")
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            CancelButton = buttonCancel
            Controls.Add(dialogLayoutPanel)
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
            HelpButton = True
            MaximizeBox = False
            MinimizeBox = False
            Name = "OpenFileWarningDialog"
            ShowIcon = False
            SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide
            dialogLayoutPanel.ResumeLayout(False)
            dialogLayoutPanel.PerformLayout()
            ResumeLayout(False)
            PerformLayout()

        End Sub
        Friend WithEvents dialogLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents messageLabel As System.Windows.Forms.Label
        Friend WithEvents alwaysCheckCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents buttonOK As System.Windows.Forms.Button
        Friend WithEvents buttonCancel As System.Windows.Forms.Button

    End Class
End Namespace
