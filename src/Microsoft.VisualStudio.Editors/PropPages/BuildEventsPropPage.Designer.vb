' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Class BuildEventsPropPage

        Friend WithEvents lblPreBuildEventCommandLine As System.Windows.Forms.Label
        Friend WithEvents lblPostBuildEventCommandLine As System.Windows.Forms.Label
        Friend WithEvents lblRunPostBuildEvent As System.Windows.Forms.Label
        Friend WithEvents txtPreBuildEventCommandLine As System.Windows.Forms.TextBox
        Friend WithEvents txtPostBuildEventCommandLine As System.Windows.Forms.TextBox
        Friend WithEvents cboRunPostBuildEvent As System.Windows.Forms.ComboBox
        Friend WithEvents btnPreBuildBuilder As System.Windows.Forms.Button
        Friend WithEvents btnPostBuildBuilder As System.Windows.Forms.Button
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Private _components As System.ComponentModel.IContainer

        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If Not (_components Is Nothing) Then
                    _components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(BuildEventsPropPage))
            Dim runPostBuildEventPanel As System.Windows.Forms.TableLayoutPanel
            Me.lblPreBuildEventCommandLine = New System.Windows.Forms.Label()
            Me.txtPreBuildEventCommandLine = New System.Windows.Forms.TextBox()
            Me.btnPreBuildBuilder = New System.Windows.Forms.Button()
            Me.lblPostBuildEventCommandLine = New System.Windows.Forms.Label()
            Me.txtPostBuildEventCommandLine = New System.Windows.Forms.TextBox()
            Me.btnPostBuildBuilder = New System.Windows.Forms.Button()
            Me.lblRunPostBuildEvent = New System.Windows.Forms.Label()
            Me.cboRunPostBuildEvent = New System.Windows.Forms.ComboBox()
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            runPostBuildEventPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            runPostBuildEventPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'lblPreBuildEventCommandLine
            '
            resources.ApplyResources(Me.lblPreBuildEventCommandLine, "lblPreBuildEventCommandLine")
            Me.lblPreBuildEventCommandLine.Name = "lblPreBuildEventCommandLine"
            '
            'txtPreBuildEventCommandLine
            '
            Me.txtPreBuildEventCommandLine.AcceptsReturn = True
            resources.ApplyResources(Me.txtPreBuildEventCommandLine, "txtPreBuildEventCommandLine")
            Me.txtPreBuildEventCommandLine.Name = "txtPreBuildEventCommandLine"
            '
            'btnPreBuildBuilder
            '
            resources.ApplyResources(Me.btnPreBuildBuilder, "btnPreBuildBuilder")
            Me.btnPreBuildBuilder.Name = "btnPreBuildBuilder"
            '
            'lblPostBuildEventCommandLine
            '
            resources.ApplyResources(Me.lblPostBuildEventCommandLine, "lblPostBuildEventCommandLine")
            Me.lblPostBuildEventCommandLine.Name = "lblPostBuildEventCommandLine"
            '
            'txtPostBuildEventCommandLine
            '
            Me.txtPostBuildEventCommandLine.AcceptsReturn = True
            resources.ApplyResources(Me.txtPostBuildEventCommandLine, "txtPostBuildEventCommandLine")
            Me.txtPostBuildEventCommandLine.Name = "txtPostBuildEventCommandLine"
            '
            'btnPostBuildBuilder
            '
            resources.ApplyResources(Me.btnPostBuildBuilder, "btnPostBuildBuilder")
            Me.btnPostBuildBuilder.Name = "btnPostBuildBuilder"
            '
            'lblRunPostBuildEvent
            '
            resources.ApplyResources(Me.lblRunPostBuildEvent, "lblRunPostBuildEvent")
            Me.lblRunPostBuildEvent.Name = "lblRunPostBuildEvent"
            '
            'cboRunPostBuildEvent
            '
            resources.ApplyResources(Me.cboRunPostBuildEvent, "cboRunPostBuildEvent")
            Me.cboRunPostBuildEvent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboRunPostBuildEvent.FormattingEnabled = True
            Me.cboRunPostBuildEvent.Items.AddRange(New Object() {resources.GetString("cboRunPostBuildEvent.Items"), resources.GetString("cboRunPostBuildEvent.Items1"), resources.GetString("cboRunPostBuildEvent.Items2")})
            Me.cboRunPostBuildEvent.Name = "cboRunPostBuildEvent"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblPreBuildEventCommandLine, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.txtPostBuildEventCommandLine, 0, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.txtPreBuildEventCommandLine, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.lblPostBuildEventCommandLine, 0, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.btnPostBuildBuilder, 0, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.btnPreBuildBuilder, 0, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(runPostBuildEventPanel, 0, 7)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            '
            'runPostBuildEventPanel
            '
            resources.ApplyResources(runPostBuildEventPanel, "runPostBuildEventPanel")
            runPostBuildEventPanel.Controls.Add(Me.lblRunPostBuildEvent, 0, 0)
            runPostBuildEventPanel.Controls.Add(Me.cboRunPostBuildEvent, 1, 0)
            runPostBuildEventPanel.Name = "runPostBuildEventPanel"
            '
            'BuildEventsPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "BuildEventsPropPage"
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            runPostBuildEventPanel.ResumeLayout(False)
            runPostBuildEventPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

    End Class

End Namespace
