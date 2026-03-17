' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms.Integration
Imports System.Windows.Media
Imports Microsoft.VisualStudio.PlatformUI
Imports Microsoft.VisualStudio.Shell
Imports WinForms = System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' WPF UserControl that provides a CPS-designer-styled UI for the Build property page.
    ''' This control is hosted inside the existing <see cref="BuildPropPage"/> via ElementHost,
    ''' bridging the WPF visual layer to the existing PropertyControlData binding infrastructure.
    ''' </summary>
    ''' <remarks>
    ''' The hidden WinForms controls remain the data targets for PropertyControlData.
    ''' This class provides two-way sync between the WPF controls (visual) and the
    ''' WinForms controls (data). Changes flow: PropertyControlData → WinForms → WPF (display)
    ''' and WPF user input → WinForms → PropertyControlData (persist).
    ''' </remarks>
    Partial Friend Class BuildPropPageWpfControl

        ' Suppresses recursive sync loops
        Private _isSyncing As Boolean

        Public Sub New()
            InitializeComponent()
            ' Defer theme color application until the visual tree is built
            AddHandler Loaded, Sub(s, e) ApplyVsThemeColors()
            AddHandler VSColorTheme.ThemeChanged, Sub(e) Dispatcher.Invoke(Sub() ApplyVsThemeColors())
        End Sub

        ''' <summary>
        ''' Applies VS shell theme colors to the WPF control tree.
        ''' Called on creation and whenever the VS theme changes (dark ↔ light ↔ high contrast).
        ''' Uses VSColorTheme.GetThemedColor() which works correctly in ElementHost context
        ''' (unlike EnvironmentColors DynamicResource keys which don't resolve).
        ''' </summary>
        Public Sub ApplyVsThemeColors()
            Dim bgColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey)
            Dim fgColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey)
            Dim grayColor = VSColorTheme.GetThemedColor(EnvironmentColors.SystemGrayTextColorKey)
            Dim separatorColor = VSColorTheme.GetThemedColor(EnvironmentColors.PanelSeparatorColorKey)
            Dim inputBg = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxBackgroundColorKey)
            Dim inputFg = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxTextColorKey)
            Dim inputBorder = VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxBorderColorKey)

            Dim wpfBg = ToWpfBrush(bgColor)
            Dim wpfFg = ToWpfBrush(fgColor)
            Dim wpfGray = ToWpfBrush(grayColor)

            Background = wpfBg
            Foreground = wpfFg

            ' Apply to all styled elements recursively
            ApplyColorsRecursive(Me, wpfFg, wpfGray, wpfBg,
                                 ToWpfBrush(inputBg), ToWpfBrush(inputFg), ToWpfBrush(inputBorder),
                                 ToWpfBrush(separatorColor))
        End Sub

        Private Shared Function ToWpfBrush(color As System.Drawing.Color) As SolidColorBrush
            Return New SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B))
        End Function

        Private Sub ApplyColorsRecursive(parent As System.Windows.DependencyObject,
                                         fgBrush As SolidColorBrush, grayBrush As SolidColorBrush,
                                         bgBrush As SolidColorBrush, inputBg As SolidColorBrush,
                                         inputFg As SolidColorBrush, inputBorder As SolidColorBrush,
                                         separatorBrush As SolidColorBrush)
            Dim count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent)
            For i = 0 To count - 1
                Dim child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i)

                If TypeOf child Is System.Windows.Controls.TextBlock Then
                    Dim tb = DirectCast(child, System.Windows.Controls.TextBlock)
                    ' Description labels use the PropertyDescriptionStyle (identified by resource lookup)
                    Dim descStyle = TryCast(TryFindResource("PropertyDescriptionStyle"), System.Windows.Style)
                    If tb.Style IsNot Nothing AndAlso descStyle IsNot Nothing AndAlso tb.Style Is descStyle Then
                        tb.Foreground = grayBrush
                    Else
                        tb.Foreground = fgBrush
                    End If

                ElseIf TypeOf child Is System.Windows.Controls.TextBox Then
                    Dim tb = DirectCast(child, System.Windows.Controls.TextBox)
                    tb.Background = inputBg
                    tb.Foreground = inputFg
                    tb.BorderBrush = inputBorder

                ElseIf TypeOf child Is System.Windows.Controls.CheckBox Then
                    DirectCast(child, System.Windows.Controls.CheckBox).Foreground = fgBrush

                ElseIf TypeOf child Is System.Windows.Controls.RadioButton Then
                    DirectCast(child, System.Windows.Controls.RadioButton).Foreground = fgBrush

                ElseIf TypeOf child Is System.Windows.Controls.Separator Then
                    DirectCast(child, System.Windows.Controls.Separator).Background = separatorBrush

                ElseIf TypeOf child Is System.Windows.Controls.Button Then
                    Dim btn = DirectCast(child, System.Windows.Controls.Button)
                    btn.Foreground = fgBrush
                    btn.BorderBrush = inputBorder
                End If

                ApplyColorsRecursive(child, fgBrush, grayBrush, bgBrush, inputBg, inputFg, inputBorder, separatorBrush)
            Next
        End Sub

        ''' <summary>
        ''' Binds this WPF control's visual elements to the hidden WinForms controls
        ''' that serve as PropertyControlData data targets. Sets up two-way sync.
        ''' </summary>
        Public Sub BindToWinFormsControls(
            wfTxtCondComp As WinForms.TextBox,
            wfChkDebug As WinForms.CheckBox,
            wfChkTrace As WinForms.CheckBox,
            wfCboPlatform As WinForms.ComboBox,
            wfCboNullable As WinForms.ComboBox,
            wfChkPrefer32 As WinForms.CheckBox,
            wfChkPreferArm64 As WinForms.CheckBox,
            wfChkUnsafe As WinForms.CheckBox,
            wfChkOptimize As WinForms.CheckBox,
            wfCboWarningLevel As WinForms.ComboBox,
            wfTxtSuppressWarnings As WinForms.TextBox,
            wfRbNone As WinForms.RadioButton,
            wfRbAll As WinForms.RadioButton,
            wfRbSpecific As WinForms.RadioButton,
            wfTxtSpecificWarnings As WinForms.TextBox,
            wfTxtOutputPath As WinForms.TextBox,
            wfChkXmlDoc As WinForms.CheckBox,
            wfTxtXmlDoc As WinForms.TextBox,
            wfChkRegCom As WinForms.CheckBox,
            wfCboSGen As WinForms.ComboBox,
            wfLblNullable As WinForms.Label,
            wfLblSGen As WinForms.Label)

            ' --- TextBox sync ---
            BindTextBox(txtConditionalCompilationSymbols, wfTxtCondComp)
            BindTextBox(txtSupressWarnings, wfTxtSuppressWarnings)
            BindTextBox(txtSpecificWarnings, wfTxtSpecificWarnings)
            BindTextBox(txtOutputPath, wfTxtOutputPath)
            BindTextBox(txtXMLDocumentationFile, wfTxtXmlDoc)

            ' --- CheckBox sync ---
            BindCheckBox(chkDefineDebug, wfChkDebug)
            BindCheckBox(chkDefineTrace, wfChkTrace)
            BindCheckBox(chkPrefer32Bit, wfChkPrefer32)
            BindCheckBox(chkPreferNativeArm64, wfChkPreferArm64)
            BindCheckBox(chkAllowUnsafeCode, wfChkUnsafe)
            BindCheckBox(chkOptimizeCode, wfChkOptimize)
            BindCheckBox(chkXMLDocumentationFile, wfChkXmlDoc)
            BindCheckBox(chkRegisterForCOM, wfChkRegCom)

            ' --- ComboBox sync ---
            BindComboBox(cboPlatformTarget, wfCboPlatform)
            BindComboBox(cboNullable, wfCboNullable)
            BindComboBox(cboWarningLevel, wfCboWarningLevel)
            BindComboBox(cboSGenOption, wfCboSGen)

            ' --- RadioButton sync ---
            BindRadioButton(rbWarningNone, wfRbNone)
            BindRadioButton(rbWarningAll, wfRbAll)
            BindRadioButton(rbWarningSpecific, wfRbSpecific)

            ' --- Visibility sync for conditional controls ---
            ' Use Enabled state (not Visible) because the WinForms controls
            ' are behind the ElementHost overlay and always report Visible=True,
            ' but their Enabled state reflects whether the feature is supported.
            SyncEnabledToVisibility(chkPrefer32Bit, wfChkPrefer32)
            SyncEnabledToVisibility(chkPreferNativeArm64, wfChkPreferArm64)

            ' Hide Nullable section if WinForms label has no items
            ' (HiddenIfMissingPropertyControlData hides via Visible=False on the
            ' label, but since the label's parent panel is behind our overlay,
            ' we check the combo's item count instead)
            If wfCboNullable.Items.Count = 0 Then
                lblNullable.Visibility = System.Windows.Visibility.Collapsed
                cboNullable.Visibility = System.Windows.Visibility.Collapsed
            Else
                lblNullable.Visibility = System.Windows.Visibility.Visible
                cboNullable.Visibility = System.Windows.Visibility.Visible
            End If

            ' Pull initial state from WinForms → WPF
            PullAllFromWinForms(wfTxtCondComp, wfChkDebug, wfChkTrace, wfCboPlatform,
                wfCboNullable, wfChkPrefer32, wfChkPreferArm64, wfChkUnsafe, wfChkOptimize,
                wfCboWarningLevel, wfTxtSuppressWarnings, wfRbNone, wfRbAll, wfRbSpecific,
                wfTxtSpecificWarnings, wfTxtOutputPath, wfChkXmlDoc, wfTxtXmlDoc,
                wfChkRegCom, wfCboSGen)
        End Sub

        ''' <summary>
        ''' Pulls current values from all WinForms controls into WPF controls.
        ''' Called after PropertyControlData populates the WinForms controls.
        ''' </summary>
        Public Sub PullAllFromWinForms(
            wfTxtCondComp As WinForms.TextBox,
            wfChkDebug As WinForms.CheckBox,
            wfChkTrace As WinForms.CheckBox,
            wfCboPlatform As WinForms.ComboBox,
            wfCboNullable As WinForms.ComboBox,
            wfChkPrefer32 As WinForms.CheckBox,
            wfChkPreferArm64 As WinForms.CheckBox,
            wfChkUnsafe As WinForms.CheckBox,
            wfChkOptimize As WinForms.CheckBox,
            wfCboWarningLevel As WinForms.ComboBox,
            wfTxtSuppressWarnings As WinForms.TextBox,
            wfRbNone As WinForms.RadioButton,
            wfRbAll As WinForms.RadioButton,
            wfRbSpecific As WinForms.RadioButton,
            wfTxtSpecificWarnings As WinForms.TextBox,
            wfTxtOutputPath As WinForms.TextBox,
            wfChkXmlDoc As WinForms.CheckBox,
            wfTxtXmlDoc As WinForms.TextBox,
            wfChkRegCom As WinForms.CheckBox,
            wfCboSGen As WinForms.ComboBox)

            _isSyncing = True
            Try
                txtConditionalCompilationSymbols.Text = wfTxtCondComp.Text
                chkDefineDebug.IsChecked = wfChkDebug.Checked
                chkDefineTrace.IsChecked = wfChkTrace.Checked
                chkPrefer32Bit.IsChecked = wfChkPrefer32.Checked
                chkPreferNativeArm64.IsChecked = wfChkPreferArm64.Checked
                chkAllowUnsafeCode.IsChecked = wfChkUnsafe.Checked
                chkOptimizeCode.IsChecked = wfChkOptimize.Checked
                chkXMLDocumentationFile.IsChecked = wfChkXmlDoc.Checked
                chkRegisterForCOM.IsChecked = wfChkRegCom.Checked
                txtSupressWarnings.Text = wfTxtSuppressWarnings.Text
                txtSpecificWarnings.Text = wfTxtSpecificWarnings.Text
                txtOutputPath.Text = wfTxtOutputPath.Text
                txtXMLDocumentationFile.Text = wfTxtXmlDoc.Text
                rbWarningNone.IsChecked = wfRbNone.Checked
                rbWarningAll.IsChecked = wfRbAll.Checked
                rbWarningSpecific.IsChecked = wfRbSpecific.Checked

                SyncComboItems(cboPlatformTarget, wfCboPlatform)
                SyncComboItems(cboWarningLevel, wfCboWarningLevel)
                SyncComboItems(cboNullable, wfCboNullable)
                SyncComboItems(cboSGenOption, wfCboSGen)

                ' Sync visibility
                SyncVisibility(chkPrefer32Bit, wfChkPrefer32)
                SyncVisibility(chkPreferNativeArm64, wfChkPreferArm64)
                SyncVisibility(chkRegisterForCOM, wfChkRegCom)
            Finally
                _isSyncing = False
            End Try
        End Sub

#Region "Binding helpers"

        Private Sub BindTextBox(wpfTb As System.Windows.Controls.TextBox, wfTb As WinForms.TextBox)
            ' WPF → WinForms
            AddHandler wpfTb.TextChanged, Sub(s, e)
                                              If Not _isSyncing Then
                                                  _isSyncing = True
                                                  wfTb.Text = wpfTb.Text
                                                  _isSyncing = False
                                              End If
                                          End Sub
            ' WinForms → WPF
            AddHandler wfTb.TextChanged, Sub(s, e)
                                             If Not _isSyncing Then
                                                 _isSyncing = True
                                                 wpfTb.Text = wfTb.Text
                                                 _isSyncing = False
                                             End If
                                         End Sub
        End Sub

        Private Sub BindCheckBox(wpfCb As System.Windows.Controls.CheckBox, wfCb As WinForms.CheckBox)
            AddHandler wpfCb.Checked, Sub(s, e)
                                          If Not _isSyncing Then
                                              _isSyncing = True
                                              wfCb.Checked = True
                                              _isSyncing = False
                                          End If
                                      End Sub
            AddHandler wpfCb.Unchecked, Sub(s, e)
                                            If Not _isSyncing Then
                                                _isSyncing = True
                                                wfCb.Checked = False
                                                _isSyncing = False
                                            End If
                                        End Sub
            AddHandler wfCb.CheckedChanged, Sub(s, e)
                                                If Not _isSyncing Then
                                                    _isSyncing = True
                                                    wpfCb.IsChecked = wfCb.Checked
                                                    _isSyncing = False
                                                End If
                                            End Sub
        End Sub

        Private Sub BindComboBox(wpfCbo As System.Windows.Controls.ComboBox, wfCbo As WinForms.ComboBox)
            AddHandler wpfCbo.SelectionChanged, Sub(s, e)
                                                    If Not _isSyncing AndAlso wpfCbo.SelectedIndex >= 0 Then
                                                        _isSyncing = True
                                                        wfCbo.SelectedIndex = wpfCbo.SelectedIndex
                                                        _isSyncing = False
                                                    End If
                                                End Sub
            AddHandler wfCbo.SelectedIndexChanged, Sub(s, e)
                                                       If Not _isSyncing Then
                                                           _isSyncing = True
                                                           SyncComboItems(wpfCbo, wfCbo)
                                                           _isSyncing = False
                                                       End If
                                                   End Sub
        End Sub

        Private Sub BindRadioButton(wpfRb As System.Windows.Controls.RadioButton, wfRb As WinForms.RadioButton)
            AddHandler wpfRb.Checked, Sub(s, e)
                                          If Not _isSyncing Then
                                              _isSyncing = True
                                              wfRb.Checked = True
                                              _isSyncing = False
                                          End If
                                      End Sub
            AddHandler wfRb.CheckedChanged, Sub(s, e)
                                                If Not _isSyncing Then
                                                    _isSyncing = True
                                                    wpfRb.IsChecked = wfRb.Checked
                                                    _isSyncing = False
                                                End If
                                            End Sub
        End Sub

        Private Shared Sub SyncComboItems(wpfCbo As System.Windows.Controls.ComboBox, wfCbo As WinForms.ComboBox)
            wpfCbo.Items.Clear()
            For Each item In wfCbo.Items
                wpfCbo.Items.Add(item.ToString())
            Next
            If wfCbo.SelectedIndex >= 0 AndAlso wfCbo.SelectedIndex < wpfCbo.Items.Count Then
                wpfCbo.SelectedIndex = wfCbo.SelectedIndex
            End If
        End Sub

        Private Shared Sub SyncVisibility(wpfElement As System.Windows.UIElement, wfControl As WinForms.Control)
            wpfElement.Visibility = If(wfControl.Visible, System.Windows.Visibility.Visible, System.Windows.Visibility.Collapsed)
        End Sub

        Private Shared Sub SyncEnabledToVisibility(wpfElement As System.Windows.UIElement, wfControl As WinForms.Control)
            ' Show checkbox if control is enabled (feature is supported for this project type)
            wpfElement.Visibility = If(wfControl.Enabled, System.Windows.Visibility.Visible, System.Windows.Visibility.Collapsed)
        End Sub

#End Region

    End Class

End Namespace
