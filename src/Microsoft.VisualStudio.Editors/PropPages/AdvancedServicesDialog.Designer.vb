' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Class AdvancedServicesDialog
        Friend WithEvents RoleServiceCacheTimeoutLabel As System.Windows.Forms.Label
        Friend WithEvents TimeQuantity As System.Windows.Forms.NumericUpDown
        Friend WithEvents TimeUnitComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents CustomConnectionString As System.Windows.Forms.TextBox
        Friend WithEvents UseCustomConnectionStringCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents HonorServerCookieExpirationCheckbox As System.Windows.Forms.CheckBox
        Friend WithEvents SavePasswordHashLocallyCheckbox As System.Windows.Forms.CheckBox
        Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
        Private _components As System.ComponentModel.IContainer

        Protected Overrides Sub Dispose(disposing As Boolean)
            Try
                If disposing AndAlso _components IsNot Nothing Then
                    _components.Dispose()
                End If
            Finally
                MyBase.Dispose(disposing)
            End Try
        End Sub

        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(AdvancedServicesDialog))
            Me.RoleServiceCacheTimeoutLabel = New System.Windows.Forms.Label
            Me.TimeQuantity = New System.Windows.Forms.NumericUpDown
            Me.TimeUnitComboBox = New System.Windows.Forms.ComboBox
            Me.CustomConnectionString = New System.Windows.Forms.TextBox
            Me.UseCustomConnectionStringCheckBox = New System.Windows.Forms.CheckBox
            Me.HonorServerCookieExpirationCheckbox = New System.Windows.Forms.CheckBox
            Me.SavePasswordHashLocallyCheckbox = New System.Windows.Forms.CheckBox
            Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
            CType(Me.TimeQuantity, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.TableLayoutPanel1.SuspendLayout()
            Me.SuspendLayout()
            '
            'RoleServiceCacheTimeoutLabel
            '
            resources.ApplyResources(Me.RoleServiceCacheTimeoutLabel, "RoleServiceCacheTimeoutLabel")
            Me.RoleServiceCacheTimeoutLabel.Name = "RoleServiceCacheTimeoutLabel"
            '
            'TimeQuantity
            '
            resources.ApplyResources(Me.TimeQuantity, "TimeQuantity")
            Me.TimeQuantity.Maximum = New Decimal(New Integer() {10000, 0, 0, 0})
            Me.TimeQuantity.Name = "TimeQuantity"
            Me.TimeQuantity.Value = New Decimal(New Integer() {60, 0, 0, 0})
            '
            'TimeUnitComboBox
            '
            Me.TimeUnitComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.TimeUnitComboBox.FormattingEnabled = True
            resources.ApplyResources(Me.TimeUnitComboBox, "TimeUnitComboBox")
            Me.TimeUnitComboBox.Name = "TimeUnitComboBox"
            '
            'CustomConnectionString
            '
            Me.TableLayoutPanel1.SetColumnSpan(Me.CustomConnectionString, 3)
            resources.ApplyResources(Me.CustomConnectionString, "CustomConnectionString")
            Me.CustomConnectionString.Name = "CustomConnectionString"
            '
            'UseCustomConnectionStringCheckBox
            '
            resources.ApplyResources(Me.UseCustomConnectionStringCheckBox, "UseCustomConnectionStringCheckBox")
            Me.TableLayoutPanel1.SetColumnSpan(Me.UseCustomConnectionStringCheckBox, 3)
            Me.UseCustomConnectionStringCheckBox.Name = "UseCustomConnectionStringCheckBox"
            Me.UseCustomConnectionStringCheckBox.UseVisualStyleBackColor = True
            '
            'HonorServerCookieExpirationCheckbox
            '
            resources.ApplyResources(Me.HonorServerCookieExpirationCheckbox, "HonorServerCookieExpirationCheckbox")
            Me.TableLayoutPanel1.SetColumnSpan(Me.HonorServerCookieExpirationCheckbox, 3)
            Me.HonorServerCookieExpirationCheckbox.Name = "HonorServerCookieExpirationCheckbox"
            Me.HonorServerCookieExpirationCheckbox.UseVisualStyleBackColor = True
            '
            'SavePasswordHashLocallyCheckbox
            '
            resources.ApplyResources(Me.SavePasswordHashLocallyCheckbox, "SavePasswordHashLocallyCheckbox")
            Me.TableLayoutPanel1.SetColumnSpan(Me.SavePasswordHashLocallyCheckbox, 3)
            Me.SavePasswordHashLocallyCheckbox.Name = "SavePasswordHashLocallyCheckbox"
            Me.SavePasswordHashLocallyCheckbox.UseVisualStyleBackColor = True
            '
            'TableLayoutPanel1
            '
            resources.ApplyResources(Me.TableLayoutPanel1, "TableLayoutPanel1")
            Me.TableLayoutPanel1.Controls.Add(Me.SavePasswordHashLocallyCheckbox, 0, 0)
            Me.TableLayoutPanel1.Controls.Add(Me.HonorServerCookieExpirationCheckbox, 0, 1)
            Me.TableLayoutPanel1.Controls.Add(Me.RoleServiceCacheTimeoutLabel, 0, 2)
            Me.TableLayoutPanel1.Controls.Add(Me.TimeQuantity, 1, 2)
            Me.TableLayoutPanel1.Controls.Add(Me.TimeUnitComboBox, 2, 2)
            Me.TableLayoutPanel1.Controls.Add(Me.UseCustomConnectionStringCheckBox, 0, 3)
            Me.TableLayoutPanel1.Controls.Add(Me.CustomConnectionString, 0, 4)
            Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
            '
            'AdvancedServicesDialog
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.TableLayoutPanel1)
            Me.Name = "AdvancedServicesDialog"
            CType(Me.TimeQuantity, System.ComponentModel.ISupportInitialize).EndInit()
            Me.TableLayoutPanel1.ResumeLayout(False)
            Me.TableLayoutPanel1.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
    End Class
End Namespace
