' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms.Integration
Imports System.Windows.Media
Imports Microsoft.VisualStudio.PlatformUI
Imports Microsoft.VisualStudio.Shell
Imports WinForms = System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' WPF UserControl that provides a CPS-designer-styled UI for the Debug property page.
    ''' This control is hosted inside the existing <see cref="DebugPropPage"/> via ElementHost,
    ''' bridging the WPF visual layer to the existing PropertyControlData binding infrastructure.
    ''' </summary>
    ''' <remarks>
    ''' The hidden WinForms controls remain the data targets for PropertyControlData.
    ''' This class provides two-way sync between the WPF controls (visual) and the
    ''' WinForms controls (data). Changes flow: PropertyControlData → WinForms → WPF (display)
    ''' and WPF user input → WinForms → PropertyControlData (persist).
    ''' </remarks>
    Partial Friend Class DebugPropPageWpfControl

        ' Suppresses recursive sync loops
        Private _isSyncing As Boolean

        Public Sub New()
            InitializeComponent()
            AddHandler Loaded, Sub(s, e)
                                   ApplyVsThemeColors()
                                   ConstrainComboBoxWidths()
                                   ' Poll-based nav sync (ScrollChanged/PreviewMouseWheel
                                   ' don't fire reliably in ElementHost context)
                                   Dim navTimer As New System.Windows.Threading.DispatcherTimer()
                                   navTimer.Interval = TimeSpan.FromMilliseconds(300)
                                   AddHandler navTimer.Tick, Sub(s2, e2) UpdateActiveNavItem()
                                   navTimer.Start()
                               End Sub
            AddHandler VSColorTheme.ThemeChanged, Sub(e) Dispatcher.Invoke(Sub() ApplyVsThemeColors())
        End Sub

        ''' <summary>
        ''' Sets ComboBox parent Border width programmatically.
        ''' XAML Width attributes are ignored when hosted in ElementHost.
        ''' </summary>
        Private Sub ConstrainComboBoxWidths()
            For Each cbo In {cboAuthenticationMode}
                Dim parent = TryCast(cbo.Parent, System.Windows.Controls.Border)
                If parent IsNot Nothing Then
                    parent.Width = 300
                    parent.HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                End If
            Next
        End Sub

        ''' <summary>
        ''' Themes the ScrollViewer's scrollbar by walking the visual tree.
        ''' XAML styles don't apply to ScrollBar in ElementHost context.
        ''' </summary>
        Private Sub ThemeScrollBar()
            Try
                Dim bgColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey)
                Dim darkBg = ToWpfBrush(bgColor)
                Dim thumbBrush = New SolidColorBrush(System.Windows.Media.Color.FromRgb(104, 104, 104))
                ThemeScrollBarRecursive(contentScrollViewer, darkBg, thumbBrush)
            Catch
            End Try
        End Sub

        Private Sub ThemeScrollBarRecursive(parent As System.Windows.DependencyObject,
                                            bgBrush As SolidColorBrush, thumbBrush As SolidColorBrush)
            Dim count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent)
            For i = 0 To count - 1
                Dim child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i)
                If TypeOf child Is System.Windows.Controls.Primitives.ScrollBar Then
                    Dim sb = DirectCast(child, System.Windows.Controls.Primitives.ScrollBar)
                    sb.Background = bgBrush
                    sb.Foreground = thumbBrush
                    ThemeScrollBarParts(sb, bgBrush, thumbBrush)
                Else
                    ThemeScrollBarRecursive(child, bgBrush, thumbBrush)
                End If
            Next
        End Sub

        Private Sub ThemeScrollBarParts(parent As System.Windows.DependencyObject,
                                        bgBrush As SolidColorBrush, thumbBrush As SolidColorBrush)
            Dim count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent)
            For i = 0 To count - 1
                Dim child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i)
                If TypeOf child Is System.Windows.Controls.Primitives.Track Then
                    Dim track = DirectCast(child, System.Windows.Controls.Primitives.Track)
                    If track.Thumb IsNot Nothing Then
                        track.Thumb.Background = thumbBrush
                    End If
                ElseIf TypeOf child Is System.Windows.Controls.Border Then
                    DirectCast(child, System.Windows.Controls.Border).Background = bgBrush
                ElseIf TypeOf child Is System.Windows.Controls.Primitives.RepeatButton Then
                    DirectCast(child, System.Windows.Controls.Primitives.RepeatButton).Background = bgBrush
                End If
                ThemeScrollBarParts(child, bgBrush, thumbBrush)
            Next
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
            Dim buttonBg = VSColorTheme.GetThemedColor(EnvironmentColors.SystemButtonFaceColorKey)
            Dim buttonFg = VSColorTheme.GetThemedColor(EnvironmentColors.SystemButtonTextColorKey)

            Dim wpfBg = ToWpfBrush(bgColor)
            Dim wpfFg = ToWpfBrush(fgColor)
            Dim wpfGray = ToWpfBrush(grayColor)
            Dim wpfInputBg = ToWpfBrush(inputBg)
            Dim wpfInputFg = ToWpfBrush(inputFg)
            Dim wpfInputBorder = ToWpfBrush(inputBorder)

            Background = wpfBg
            Foreground = wpfFg

            ' Explicitly set ScrollViewer and content panel backgrounds —
            ' in ElementHost context, the ScrollViewer's default template has
            ' a white background that isn't overridden by SystemColors alone.
            contentScrollViewer.Background = wpfBg

            ' Override SystemColors resource keys so that default WPF control templates
            ' (ComboBox, ScrollBar, etc.) pick up VS theme colors instead of Windows system colors.
            Resources(System.Windows.SystemColors.WindowBrushKey) = wpfInputBg
            Resources(System.Windows.SystemColors.WindowTextBrushKey) = wpfInputFg
            Resources(System.Windows.SystemColors.ControlBrushKey) = wpfBg
            Resources(System.Windows.SystemColors.ControlTextBrushKey) = wpfFg
            Resources(System.Windows.SystemColors.HighlightBrushKey) = ToWpfBrush(
                VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxMouseOverBackgroundMiddle1ColorKey))
            Resources(System.Windows.SystemColors.HighlightTextBrushKey) = wpfInputFg
            Resources(System.Windows.SystemColors.GrayTextBrushKey) = wpfGray
            Resources(System.Windows.SystemColors.ActiveBorderBrushKey) = wpfInputBorder
            ' ScrollBar theming — prevent white scrollbar track/thumb
            Resources(System.Windows.SystemColors.ScrollBarBrushKey) = wpfBg
            Resources(System.Windows.SystemColors.ControlDarkBrushKey) = wpfInputBorder
            Resources(System.Windows.SystemColors.ControlLightBrushKey) = wpfBg
            Resources(System.Windows.SystemColors.ControlDarkDarkBrushKey) = wpfInputBorder

            ' Apply to all styled elements recursively
            ApplyColorsRecursive(Me, wpfFg, wpfGray, wpfBg,
                                 wpfInputBg, wpfInputFg, wpfInputBorder,
                                 ToWpfBrush(separatorColor),
                                 ToWpfBrush(buttonBg), ToWpfBrush(buttonFg))

            ' Store a brush for the active nav item highlight (used by UpdateActiveNavItem)
            Dim navHighlight = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTabSelectedTabColorKey)
            Resources("NavActiveBrush") = ToWpfBrush(navHighlight)

            ' Update nav item highlighting
            UpdateActiveNavItem()
        End Sub

        Private Shared Function ToWpfBrush(color As System.Drawing.Color) As SolidColorBrush
            Return New SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B))
        End Function

        Private Sub ApplyColorsRecursive(parent As System.Windows.DependencyObject,
                                         fgBrush As SolidColorBrush, grayBrush As SolidColorBrush,
                                         bgBrush As SolidColorBrush, inputBg As SolidColorBrush,
                                         inputFg As SolidColorBrush, inputBorder As SolidColorBrush,
                                         separatorBrush As SolidColorBrush,
                                         buttonBg As SolidColorBrush, buttonFg As SolidColorBrush)
            Dim count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent)
            For i = 0 To count - 1
                Dim child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i)

                If TypeOf child Is System.Windows.Controls.TextBlock Then
                    Dim tb = DirectCast(child, System.Windows.Controls.TextBlock)
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

                ElseIf TypeOf child Is System.Windows.Controls.ComboBox Then
                    Dim cbo = DirectCast(child, System.Windows.Controls.ComboBox)
                    cbo.Background = inputBg
                    cbo.Foreground = inputFg
                    cbo.BorderBrush = inputBorder

                ElseIf TypeOf child Is System.Windows.Controls.CheckBox Then
                    Dim chk = DirectCast(child, System.Windows.Controls.CheckBox)
                    chk.Foreground = If(chk.IsEnabled, fgBrush, grayBrush)

                ElseIf TypeOf child Is System.Windows.Controls.RadioButton Then
                    Dim rb = DirectCast(child, System.Windows.Controls.RadioButton)
                    rb.Foreground = If(rb.IsEnabled, fgBrush, grayBrush)

                ElseIf TypeOf child Is System.Windows.Controls.Border Then
                    Dim bdr = DirectCast(child, System.Windows.Controls.Border)
                    ' Only style separator borders (identified by resource lookup)
                    Dim sepStyle = TryCast(TryFindResource("TitleSeparatorStyle"), System.Windows.Style)
                    If bdr.Style IsNot Nothing AndAlso sepStyle IsNot Nothing AndAlso bdr.Style Is sepStyle Then
                        bdr.Background = separatorBrush
                    End If

                ElseIf TypeOf child Is System.Windows.Controls.Button Then
                    Dim btn = DirectCast(child, System.Windows.Controls.Button)
                    btn.Background = buttonBg
                    btn.Foreground = buttonFg
                    btn.BorderBrush = inputBorder
                End If

                ApplyColorsRecursive(child, fgBrush, grayBrush, bgBrush, inputBg, inputFg, inputBorder,
                                     separatorBrush, buttonBg, buttonFg)
            Next
        End Sub

        ''' <summary>
        ''' Binds this WPF control's visual elements to the hidden WinForms controls
        ''' that serve as PropertyControlData data targets. Sets up two-way sync.
        ''' </summary>
        Public Sub BindToWinFormsControls(
            wfRbStartProject As WinForms.RadioButton,
            wfRbStartProgram As WinForms.RadioButton,
            wfRbStartURL As WinForms.RadioButton,
            wfTxtStartProgram As WinForms.TextBox,
            wfTxtStartURL As WinForms.TextBox,
            wfTxtStartArguments As WinForms.TextBox,
            wfTxtStartWorkingDirectory As WinForms.TextBox,
            wfChkRemoteDebugEnabled As WinForms.CheckBox,
            wfTxtRemoteDebugMachine As WinForms.TextBox,
            wfCboAuthenticationMode As WinForms.ComboBox,
            wfChkEnableUnmanagedDebugging As WinForms.CheckBox,
            wfChkEnableSQLServerDebugging As WinForms.CheckBox)

            ' --- RadioButton sync ---
            BindRadioButton(rbStartProject, wfRbStartProject)
            BindRadioButton(rbStartProgram, wfRbStartProgram)
            BindRadioButton(rbStartURL, wfRbStartURL)

            ' --- TextBox sync ---
            BindTextBox(txtStartProgram, wfTxtStartProgram)
            BindTextBox(txtStartURL, wfTxtStartURL)
            BindTextBox(txtCommandLineArgs, wfTxtStartArguments)
            BindTextBox(txtWorkingDirectory, wfTxtStartWorkingDirectory)
            BindTextBox(txtRemoteDebugMachine, wfTxtRemoteDebugMachine)

            ' --- CheckBox sync ---
            BindCheckBox(chkRemoteDebugEnabled, wfChkRemoteDebugEnabled)
            BindCheckBox(chkEnableUnmanagedDebugging, wfChkEnableUnmanagedDebugging)
            BindCheckBox(chkEnableSQLServerDebugging, wfChkEnableSQLServerDebugging)

            ' --- ComboBox sync ---
            BindComboBox(cboAuthenticationMode, wfCboAuthenticationMode)

            ' --- Enabled state sync for controls dependent on radio/checkbox state ---
            ' Start program textbox + browse enabled only when "Start external program" is selected
            txtStartProgram.IsEnabled = wfTxtStartProgram.Enabled
            btnStartProgramBrowse.IsEnabled = wfTxtStartProgram.Enabled
            AddHandler wfTxtStartProgram.EnabledChanged, Sub(s, e)
                                                             txtStartProgram.IsEnabled = wfTxtStartProgram.Enabled
                                                             btnStartProgramBrowse.IsEnabled = wfTxtStartProgram.Enabled
                                                         End Sub

            ' Start URL textbox enabled only when "Start browser with URL" is selected
            txtStartURL.IsEnabled = wfTxtStartURL.Enabled
            AddHandler wfTxtStartURL.EnabledChanged, Sub(s, e)
                                                         txtStartURL.IsEnabled = wfTxtStartURL.Enabled
                                                     End Sub

            ' Remote debug machine textbox enabled only when "Use remote machine" is checked
            txtRemoteDebugMachine.IsEnabled = wfTxtRemoteDebugMachine.Enabled
            AddHandler wfTxtRemoteDebugMachine.EnabledChanged, Sub(s, e)
                                                                   txtRemoteDebugMachine.IsEnabled = wfTxtRemoteDebugMachine.Enabled
                                                               End Sub

            ' Authentication mode enabled only when remote debugging is enabled
            cboAuthenticationMode.IsEnabled = wfCboAuthenticationMode.Enabled
            lblAuthenticationMode.IsEnabled = wfCboAuthenticationMode.Enabled
            AddHandler wfCboAuthenticationMode.EnabledChanged, Sub(s, e)
                                                                   cboAuthenticationMode.IsEnabled = wfCboAuthenticationMode.Enabled
                                                                   lblAuthenticationMode.IsEnabled = wfCboAuthenticationMode.Enabled
                                                               End Sub

            ' Pull initial state from WinForms → WPF
            PullAllFromWinForms(wfRbStartProject, wfRbStartProgram, wfRbStartURL,
                wfTxtStartProgram, wfTxtStartURL, wfTxtStartArguments,
                wfTxtStartWorkingDirectory, wfChkRemoteDebugEnabled,
                wfTxtRemoteDebugMachine, wfCboAuthenticationMode,
                wfChkEnableUnmanagedDebugging, wfChkEnableSQLServerDebugging)

            ' Re-apply theme colors AFTER binding so disabled checkboxes get gray text
            ApplyVsThemeColors()
        End Sub

        ''' <summary>
        ''' Pulls current values from all WinForms controls into WPF controls.
        ''' Called after PropertyControlData populates the WinForms controls.
        ''' </summary>
        Public Sub PullAllFromWinForms(
            wfRbStartProject As WinForms.RadioButton,
            wfRbStartProgram As WinForms.RadioButton,
            wfRbStartURL As WinForms.RadioButton,
            wfTxtStartProgram As WinForms.TextBox,
            wfTxtStartURL As WinForms.TextBox,
            wfTxtStartArguments As WinForms.TextBox,
            wfTxtStartWorkingDirectory As WinForms.TextBox,
            wfChkRemoteDebugEnabled As WinForms.CheckBox,
            wfTxtRemoteDebugMachine As WinForms.TextBox,
            wfCboAuthenticationMode As WinForms.ComboBox,
            wfChkEnableUnmanagedDebugging As WinForms.CheckBox,
            wfChkEnableSQLServerDebugging As WinForms.CheckBox)

            _isSyncing = True
            Try
                rbStartProject.IsChecked = wfRbStartProject.Checked
                rbStartProgram.IsChecked = wfRbStartProgram.Checked
                rbStartURL.IsChecked = wfRbStartURL.Checked
                txtStartProgram.Text = wfTxtStartProgram.Text
                txtStartURL.Text = wfTxtStartURL.Text
                txtCommandLineArgs.Text = wfTxtStartArguments.Text
                txtWorkingDirectory.Text = wfTxtStartWorkingDirectory.Text
                chkRemoteDebugEnabled.IsChecked = wfChkRemoteDebugEnabled.Checked
                txtRemoteDebugMachine.Text = wfTxtRemoteDebugMachine.Text
                chkEnableUnmanagedDebugging.IsChecked = wfChkEnableUnmanagedDebugging.Checked
                chkEnableSQLServerDebugging.IsChecked = wfChkEnableSQLServerDebugging.Checked

                SyncComboItems(cboAuthenticationMode, wfCboAuthenticationMode)

                ' Note: Do NOT call SyncVisibility here — the WinForms parent
                ' (overarchingTableLayoutPanel) is hidden, which makes all WinForms
                ' controls report Visible=False. Enabled state is synced separately
                ' in BindToWinFormsControls via IsEnabled property.
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

#End Region

#Region "Section navigation"

        ''' <summary>
        ''' Handles click on a nav item to scroll to the corresponding section.
        ''' </summary>
        Private Sub NavItem_Click(sender As Object, e As System.Windows.Input.MouseButtonEventArgs)
            Dim tb = TryCast(sender, System.Windows.Controls.TextBlock)
            If tb Is Nothing Then Return

            Dim sectionName = TryCast(tb.Tag, String)
            If String.IsNullOrEmpty(sectionName) Then Return

            Dim section = TryCast(FindName(sectionName), System.Windows.FrameworkElement)
            If section IsNot Nothing Then
                ' Use TransformToVisual (TransformToAncestor doesn't work in ElementHost)
                Try
                    Dim transform = section.TransformToVisual(contentScrollViewer)
                    Dim point = transform.Transform(New System.Windows.Point(0, 0))
                    contentScrollViewer.ScrollToVerticalOffset(contentScrollViewer.VerticalOffset + point.Y - 10)
                Catch
                    ' Fallback: try BringIntoView
                    section.BringIntoView()
                End Try
            End If
        End Sub

        Private Sub UpdateActiveNavItem()
            ' Find which section is closest to the top of the viewport
            Dim sections() As System.Windows.FrameworkElement = {sectionStartAction, sectionStartOptions, sectionDebuggerEngines}
            Dim navItems() As System.Windows.Controls.TextBlock = {navStartAction, navStartOptions, navDebuggerEngines}

            Dim activeBrush = TryCast(TryFindResource("NavActiveBrush"), System.Windows.Media.SolidColorBrush)
            If activeBrush Is Nothing Then
                activeBrush = New System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(60, 100, 100, 255))
            End If
            Dim transparentBrush = System.Windows.Media.Brushes.Transparent

            Dim activeIndex = 0
            Try
                Dim scrollOffset = contentScrollViewer.VerticalOffset
                For i = 0 To sections.Length - 1
                    ' Use TransformToVisual instead of TransformToAncestor (more reliable in ElementHost)
                    Dim transform = sections(i).TransformToVisual(contentScrollViewer)
                    Dim point = transform.Transform(New System.Windows.Point(0, 0))
                    If point.Y <= 40 Then
                        activeIndex = i
                    End If
                Next
            Catch
                ' Non-critical — keep current activeIndex
            End Try

            For i = 0 To navItems.Length - 1
                If i = activeIndex Then
                    navItems(i).SetValue(System.Windows.Controls.TextBlock.BackgroundProperty, activeBrush)
                    navItems(i).FontWeight = System.Windows.FontWeights.SemiBold
                Else
                    navItems(i).SetValue(System.Windows.Controls.TextBlock.BackgroundProperty, transparentBrush)
                    navItems(i).FontWeight = System.Windows.FontWeights.Normal
                End If
            Next
        End Sub

#End Region

    End Class

End Namespace
